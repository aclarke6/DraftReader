using DraftReader.Domain.Enumerations;
using DraftReader.Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DraftReader.Domain.Entities;
using DraftReader.Domain.Interfaces.Repositories;
using DraftReader.Domain.Interfaces.Services;
using DraftReader.Web.Models;

namespace DraftReader.Web.Controllers;

[Authorize]
public class AuthorController(
    IScrivenerProjectRepository projectRepo,
    ISectionRepository sectionRepo,
    IPublicationService publicationService,
    IUserService userService,
    IDashboardService dashboardService,
    ISyncService syncService,
    IUserRepository userRepo,
    IScrivenerProjectDiscoveryService discoveryService,
    IServiceScopeFactory scopeFactory,
    ISyncProgressTracker progressTracker,
    ILogger<AuthorController> logger) : Controller
{
    // ---------------------------------------------------------------------------
    // Dashboard
    // ---------------------------------------------------------------------------
    public async Task<IActionResult> Dashboard()
    {
        var author = await GetAuthorAsync();
        if (author is null)
            return RedirectToAction("Index", "Reader");

        var projects         = await projectRepo.GetAllAsync();
        var active           = await projectRepo.GetReaderActiveProjectAsync();
        var publishedChapters = active is not null
            ? await publicationService.GetPublishedChaptersAsync(active.Id)
            : new List<Section>();
        var failures  = await dashboardService.GetEmailHealthSummaryAsync();
        var readers   = await userRepo.GetAllBetaReadersAsync();

        return View(new DashboardViewModel
        {
            ActiveProject     = active,
            AllProjects       = projects,
            PublishedSections = publishedChapters,
            EmailFailures     = failures,
            ActiveReaderCount = readers.Count(r => r.IsActive && !r.IsSoftDeleted)
        });
    }

    // ---------------------------------------------------------------------------
    // Sync
    // ---------------------------------------------------------------------------
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Sync(Guid projectId)
    {
        // Mark as syncing immediately so the dashboard shows the progress bar
        var project = await projectRepo.GetByIdAsync(projectId);
        if (project is not null)
        {
            project.MarkSyncing();
            await GetUnitOfWork().SaveChangesAsync();
        }

        // Fire sync in background using scopeFactory (safe after request ends)
        _ = Task.Run(async () =>
        {
            using var scope   = scopeFactory.CreateScope();
            var bgSyncService = scope.ServiceProvider.GetRequiredService<ISyncService>();
            var bgProjectRepo = scope.ServiceProvider.GetRequiredService<IScrivenerProjectRepository>();
            var bgUnitOfWork  = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            try
            {
                await bgSyncService.ParseProjectAsync(projectId);
                logger.LogInformation("Background sync completed for project {ProjectId}", projectId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Background sync failed for project {ProjectId}: {Message}",
                    projectId, ex.Message);

                // Ensure the project status reflects the failure
                try
                {
                    var failedProject = await bgProjectRepo.GetByIdAsync(projectId);
                    if (failedProject is not null && failedProject.SyncStatus == SyncStatus.Syncing)
                    {
                        failedProject.UpdateSyncStatus(SyncStatus.Error, DateTime.UtcNow,
                            ex.Message.Length > 200 ? ex.Message[..200] : ex.Message);
                        await bgUnitOfWork.SaveChangesAsync();
                    }
                }
                catch (Exception innerEx)
                {
                    logger.LogError(innerEx, "Failed to update sync error status for {ProjectId}", projectId);
                }
            }
        });

        return RedirectToAction("Dashboard");
    }

    [HttpGet]
    public async Task<IActionResult> GetSyncStatus(Guid projectId)
    {
        var project = await projectRepo.GetByIdAsync(projectId);
        if (project is null) return NotFound();

        var progress = progressTracker.Get(projectId);

        return Json(new
        {
            status            = project.SyncStatus.ToString(),
            errorMessage      = project.SyncErrorMessage,
            sectionsProcessed = progress?.SectionsProcessed ?? 0,
            currentSection    = progress?.CurrentSection ?? string.Empty
        });
    }

    // ---------------------------------------------------------------------------
    // Project activation
    // ---------------------------------------------------------------------------
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ActivateProject(Guid projectId)
    {
        var project = await projectRepo.GetByIdAsync(projectId);
        if (project is null) return NotFound();

        var current = await projectRepo.GetReaderActiveProjectAsync();
        if (current is not null && current.Id != projectId)
            current.DeactivateForReaders();

        project.ActivateForReaders();
        await GetUnitOfWork().SaveChangesAsync();

        TempData["Success"] = $"{project.Name} is now active for readers.";
        return RedirectToAction("Dashboard");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeactivateProject(Guid projectId)
    {
        var project = await projectRepo.GetByIdAsync(projectId);
        if (project is null) return NotFound();

        project.DeactivateForReaders();
        await GetUnitOfWork().SaveChangesAsync();

        TempData["Success"] = $"{project.Name} is now inactive for readers.";
        return RedirectToAction("Dashboard");
    }

    // ---------------------------------------------------------------------------
    // Sections list with chapter publish buttons
    // ---------------------------------------------------------------------------
    public async Task<IActionResult> Sections(Guid projectId)
    {
        var project = await projectRepo.GetByIdAsync(projectId);
        if (project is null) return NotFound();

        var sections = await sectionRepo.GetByProjectIdAsync(projectId);
        var sorted   = SortDepthFirst(sections);

        // Pre-compute which folders can be published
        var publishable = new HashSet<Guid>();
        foreach (var (s, _) in sorted.Where(x => x.Section.NodeType == NodeType.Folder))
        {
            if (await publicationService.CanPublishAsync(s.Id))
                publishable.Add(s.Id);
        }

        ViewBag.Project    = project;
        ViewBag.Publishable = publishable;
        return View(sorted);
    }

    // ---------------------------------------------------------------------------
    // Chapter publish / unpublish
    // ---------------------------------------------------------------------------
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PublishChapter(Guid chapterId, Guid projectId)
    {
        var author = await GetAuthorAsync();
        if (author is null) return Forbid();

        try
        {
            await publicationService.PublishChapterAsync(chapterId, author.Id);
            TempData["Success"] = "Chapter published.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction("Sections", new { projectId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UnpublishChapter(Guid chapterId, Guid projectId)
    {
        var author = await GetAuthorAsync();
        if (author is null) return Forbid();

        await publicationService.UnpublishChapterAsync(chapterId, author.Id);
        TempData["Success"] = "Chapter unpublished.";
        return RedirectToAction("Sections", new { projectId });
    }

    // ---------------------------------------------------------------------------
    // Readers
    // ---------------------------------------------------------------------------
    public async Task<IActionResult> Readers()
    {
        var readers = await userRepo.GetAllBetaReadersAsync();
        return View(readers);
    }

    [HttpGet]
    public IActionResult InviteReader() => View(new InviteReaderViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> InviteReader(InviteReaderViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var author = await GetAuthorAsync();
        if (author is null) return Forbid();

        try
        {
            var policy = model.NeverExpires ? ExpiryPolicy.AlwaysOpen : ExpiryPolicy.ExpiresAt;
            await userService.IssueInvitationAsync(model.Email, policy, model.ExpiresAt, author.Id);
            TempData["Success"] = $"Invitation sent to {model.Email}.";
            return RedirectToAction("Readers");
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReactivateReader(Guid userId)
    {
        var author = await GetAuthorAsync();
        if (author is null) return Forbid();

        await userService.ReactivateUserAsync(userId, author.Id);
        TempData["Success"] = "Reader reactivated.";
        return RedirectToAction("Readers");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeactivateReader(Guid userId)
    {
        var author = await GetAuthorAsync();
        if (author is null) return Forbid();

        await userService.DeactivateUserAsync(userId, author.Id);
        TempData["Success"] = "Reader deactivated.";
        return RedirectToAction("Readers");
    }

    // ---------------------------------------------------------------------------
    // Section detail with comments (author view)
    // ---------------------------------------------------------------------------
    public async Task<IActionResult> Section(Guid id)
    {
        var author = await GetAuthorAsync();
        if (author is null) return Forbid();

        var s = await sectionRepo.GetByIdAsync(id);
        if (s is null) return NotFound();

        var comments = await GetCommentService().GetThreadsForSectionAsync(id, author.Id);
        var events   = await GetReadEventRepo().GetBySectionIdAsync(id);

        return View(new SectionViewModel
        {
            Section   = s,
            Comments  = comments,
            ReadCount = events.Count
        });
    }

    // ---------------------------------------------------------------------------
    // Project removal (soft delete)
    // ---------------------------------------------------------------------------

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveProject(Guid projectId)
    {
        var author = await GetAuthorAsync();
        if (author is null) return Forbid();

        var project = await projectRepo.GetByIdAsync(projectId);
        if (project is null) return NotFound();

        project.SoftDelete();
        await GetUnitOfWork().SaveChangesAsync();

        TempData["Success"] = $"{project.Name} removed. You can re-add it from Add Project.";
        return RedirectToAction("Dashboard");
    }

    // ---------------------------------------------------------------------------
    // Projects discovery
    // ---------------------------------------------------------------------------

    public async Task<IActionResult> Projects()
    {
        var author = await GetAuthorAsync();
        if (author is null) return Forbid();

        var discovered = await discoveryService.DiscoverAsync();
        return View(discovered);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddProjects(List<string> selectedUuids)
    {
        var author = await GetAuthorAsync();
        if (author is null) return Forbid();

        if (selectedUuids is null || selectedUuids.Count == 0)
        {
            TempData["Error"] = "No projects selected.";
            return RedirectToAction("Projects");
        }

        var discovered = await discoveryService.DiscoverAsync();
        var toAdd      = discovered
            .Where(d => selectedUuids.Contains(d.ScrivenerRootUuid) && !d.AlreadyAdded)
            .ToList();

        var addedCount = 0;
        foreach (var d in toAdd)
        {
            try
            {
                // Check for soft-deleted project with same UUID - restore instead of create
                var softDeleted = await projectRepo.GetSoftDeletedByScrivenerRootUuidAsync(d.ScrivenerRootUuid);
                if (softDeleted is not null)
                {
                    softDeleted.Restore(d.Name);
                    addedCount++;
                }
                else
                {
                    var project = ScrivenerProject.Create(d.Name, d.DropboxPath, d.ScrivenerRootUuid);
                    await projectRepo.AddAsync(project);
                    addedCount++;
                }
            }
            catch (DuplicateProjectException)
            {
                // Already exists as active - skip silently
            }
        }

        await GetUnitOfWork().SaveChangesAsync();

        // Trigger initial sync for each newly added project
        foreach (var d in toAdd)
        {
            var projects = await projectRepo.GetAllAsync();
            var project  = projects.FirstOrDefault(p => p.ScrivenerRootUuid == d.ScrivenerRootUuid);
            if (project is null) continue;

            try { await syncService.ParseProjectAsync(project.Id); }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Initial sync failed for {Name}", d.Name);
            }
        }

        TempData["Success"] = addedCount == 1
            ? $"{toAdd.First(d => true).Name} added successfully."
            : $"{addedCount} projects added successfully.";

        return RedirectToAction("Dashboard");
    }

    // ---------------------------------------------------------------------------
    // Private helpers
    // ---------------------------------------------------------------------------

    private async Task<User?> GetAuthorAsync()
    {
        var email = User.Identity?.Name;
        if (email is null)
            return null;
        var user = await userRepo.GetByEmailAsync(email);
        return user?.Role == Domain.Enumerations.Role.Author ? user : null;
    }

    private IUnitOfWork GetUnitOfWork() =>
        HttpContext.RequestServices.GetRequiredService<IUnitOfWork>();

    private ICommentService GetCommentService() =>
        HttpContext.RequestServices.GetRequiredService<ICommentService>();

    private IReadEventRepository GetReadEventRepo() =>
        HttpContext.RequestServices.GetRequiredService<IReadEventRepository>();

    private static IReadOnlyList<(Section Section, int Depth)> SortDepthFirst(
        IReadOnlyList<Section> sections)
    {
        var root   = Guid.Empty;
        var lookup = new Dictionary<Guid, List<Section>>();

        foreach (var s in sections)
        {
            var key = s.ParentId ?? root;
            if (!lookup.ContainsKey(key))
                lookup[key] = new List<Section>();
            lookup[key].Add(s);
        }

        foreach (var key in lookup.Keys.ToList())
            lookup[key] = lookup[key].OrderBy(s => s.SortOrder).ToList();

        var result = new List<(Section, int)>();

        void Walk(Guid parentId, int depth)
        {
            if (!lookup.TryGetValue(parentId, out var children)) return;
            foreach (var child in children)
            {
                result.Add((child, depth));
                Walk(child.Id, depth + 1);
            }
        }

        Walk(root, 0);
        return result;
    }
}















