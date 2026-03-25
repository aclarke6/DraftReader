# DraftReader - Build ISyncProgressTracker for live sync progress
$ErrorActionPreference = "Stop"

Write-Host "DraftReader - building sync progress tracker" -ForegroundColor Cyan
Write-Host ""

# ---------------------------------------------------------------------------
# 1. ISyncProgressTracker interface (Domain)
# ---------------------------------------------------------------------------
New-Item -ItemType Directory -Force -Path "DraftReader.Domain\Interfaces\Services" | Out-Null

Set-Content "DraftReader.Domain\Interfaces\Services\ISyncProgressTracker.cs" @'
namespace DraftReader.Domain.Interfaces.Services;

public sealed class SyncProgress
{
    public int SectionsProcessed { get; set; }
    public string? CurrentSection { get; set; }
    public DateTime StartedAt { get; set; }
}

public interface ISyncProgressTracker
{
    void Start(Guid projectId);
    void Increment(Guid projectId, string sectionTitle);
    SyncProgress? Get(Guid projectId);
    void Clear(Guid projectId);
}
'@
Write-Host "  ISyncProgressTracker.cs written." -ForegroundColor White

# ---------------------------------------------------------------------------
# 2. SyncProgressTracker implementation (Infrastructure or Web - singleton)
# ---------------------------------------------------------------------------
New-Item -ItemType Directory -Force -Path "DraftReader.Application\Services" | Out-Null

Set-Content "DraftReader.Application\Services\SyncProgressTracker.cs" @'
using System.Collections.Concurrent;
using DraftReader.Domain.Interfaces.Services;

namespace DraftReader.Application.Services;

public class SyncProgressTracker : ISyncProgressTracker
{
    private readonly ConcurrentDictionary<Guid, SyncProgress> _progress = new();

    public void Start(Guid projectId)
    {
        _progress[projectId] = new SyncProgress
        {
            SectionsProcessed = 0,
            CurrentSection    = null,
            StartedAt         = DateTime.UtcNow
        };
    }

    public void Increment(Guid projectId, string sectionTitle)
    {
        _progress.AddOrUpdate(projectId,
            new SyncProgress { SectionsProcessed = 1, CurrentSection = sectionTitle, StartedAt = DateTime.UtcNow },
            (_, existing) =>
            {
                existing.SectionsProcessed++;
                existing.CurrentSection = sectionTitle;
                return existing;
            });
    }

    public SyncProgress? Get(Guid projectId) =>
        _progress.TryGetValue(projectId, out var p) ? p : null;

    public void Clear(Guid projectId) =>
        _progress.TryRemove(projectId, out _);
}
'@
Write-Host "  SyncProgressTracker.cs written." -ForegroundColor White

# ---------------------------------------------------------------------------
# 3. Inject ISyncProgressTracker into SyncService
# ---------------------------------------------------------------------------
$syncPath = "DraftReader.Application\Services\SyncService.cs"
$sync = Get-Content $syncPath -Raw

# Add to constructor
$old1 = 'public class SyncService(
    IScrivenerProjectRepository projectRepo,
    ISectionRepository sectionRepo,
    IUnitOfWork unitOfWork,
    IScrivenerProjectParser parser,
    IRtfConverter converter,
    ILocalPathResolver pathResolver) : ISyncService'

$new1 = 'public class SyncService(
    IScrivenerProjectRepository projectRepo,
    ISectionRepository sectionRepo,
    IUnitOfWork unitOfWork,
    IScrivenerProjectParser parser,
    IRtfConverter converter,
    ILocalPathResolver pathResolver,
    ISyncProgressTracker progressTracker) : ISyncService'

$sync = $sync.Replace($old1, $new1)

# Start tracking at beginning of ParseProjectAsync
$old2 = '        // Mark as syncing immediately so the dashboard can show progress
        project.MarkSyncing();
        await unitOfWork.SaveChangesAsync(ct);'

$new2 = '        // Mark as syncing immediately so the dashboard can show progress
        project.MarkSyncing();
        progressTracker.Start(projectId);
        await unitOfWork.SaveChangesAsync(ct);'

$sync = $sync.Replace($old2, $new2)

# Clear tracker when sync completes (success)
$old3 = '            project.UpdateSyncStatus(SyncStatus.Healthy, DateTime.UtcNow, null);'
$new3 = '            project.UpdateSyncStatus(SyncStatus.Healthy, DateTime.UtcNow, null);
            progressTracker.Clear(projectId);'
$sync = $sync.Replace($old3, $new3)

# Clear tracker when sync fails
$old4 = '            project.UpdateSyncStatus(SyncStatus.Error, DateTime.UtcNow, ex.Message);'
$new4 = '            project.UpdateSyncStatus(SyncStatus.Error, DateTime.UtcNow, ex.Message);
            progressTracker.Clear(projectId);'
$sync = $sync.Replace($old4, $new4)

# Increment tracker in ReconcileNodeAsync
$old5 = '        seenUuids.Add(node.Uuid);

        var existing = await sectionRepo.GetByScrivenerUuidAsync(projectId, node.Uuid, ct);'

$new5 = '        seenUuids.Add(node.Uuid);
        progressTracker.Increment(projectId, node.Title);

        var existing = await sectionRepo.GetByScrivenerUuidAsync(projectId, node.Uuid, ct);'

$sync = $sync.Replace($old5, $new5)

Set-Content $syncPath $sync
Write-Host "  SyncService.cs updated with progress tracking." -ForegroundColor White

# ---------------------------------------------------------------------------
# 4. Register as singleton in Program.cs
# ---------------------------------------------------------------------------
$programPath = "DraftReader.Web\Program.cs"
$program = Get-Content $programPath -Raw

if ($program -notmatch "ISyncProgressTracker") {
    $old6 = 'builder.Services.AddScoped<IPublicationService, PublicationService>();'
    $new6 = 'builder.Services.AddScoped<IPublicationService, PublicationService>();
builder.Services.AddSingleton<ISyncProgressTracker, SyncProgressTracker>();'
    $program = $program.Replace($old6, $new6)
    Set-Content $programPath $program
    Write-Host "  Program.cs - ISyncProgressTracker registered as singleton." -ForegroundColor White
}

# ---------------------------------------------------------------------------
# 5. Update GetSyncStatus endpoint to return progress
# ---------------------------------------------------------------------------
$controllerPath = "DraftReader.Web\Controllers\AuthorController.cs"
$controller = Get-Content $controllerPath -Raw

# Add ISyncProgressTracker to constructor
$old7 = 'public class AuthorController(
    IScrivenerProjectRepository projectRepo,
    ISectionRepository sectionRepo,
    IPublicationService publicationService,
    IUserService userService,
    IDashboardService dashboardService,
    ISyncService syncService,
    IUserRepository userRepo,
    IScrivenerProjectDiscoveryService discoveryService,
    IServiceScopeFactory scopeFactory,
    ILogger<AuthorController> logger) : Controller'

$new7 = 'public class AuthorController(
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
    ILogger<AuthorController> logger) : Controller'

$controller = $controller.Replace($old7, $new7)

# Update GetSyncStatus to include progress
$old8 = '    [HttpGet]
    public async Task<IActionResult> GetSyncStatus(Guid projectId)
    {
        var project = await projectRepo.GetByIdAsync(projectId);
        if (project is null) return NotFound();

        return Json(new
        {
            status       = project.SyncStatus.ToString(),
            errorMessage = project.SyncErrorMessage
        });
    }'

$new8 = '    [HttpGet]
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
    }'

$controller = $controller.Replace($old8, $new8)
Set-Content $controllerPath $controller
Write-Host "  AuthorController.cs - GetSyncStatus returns progress data." -ForegroundColor White

# ---------------------------------------------------------------------------
# 6. Update Dashboard JS to show sections processed
# ---------------------------------------------------------------------------
$dashPath = "DraftReader.Web\Views\Author\Dashboard.cshtml"
$dash = Get-Content $dashPath -Raw

# Update the Syncing badge to include a sections counter
$old9 = '                            else if (p.SyncStatus == SyncStatus.Syncing)
                            {
                                <span class="badge badge--accent" id="sync-badge-@p.Id">Syncing...</span>
                                <div style="margin-top:4px; width:120px; height:4px; background:var(--color-border); border-radius:2px; overflow:hidden; position:relative;">
                                    <div id="sync-bar-@p.Id" style="height:100%; width:30%; background:var(--color-accent); border-radius:2px; position:absolute; animation: syncsweep 1.6s ease-in-out infinite;"></div>
                                </div>
                            }'

$new9 = '                            else if (p.SyncStatus == SyncStatus.Syncing)
                            {
                                <span class="badge badge--accent" id="sync-badge-@p.Id">Syncing...</span>
                                <div id="sync-count-@p.Id" style="font-size:0.75rem; color:var(--color-ink-muted); margin-top:2px;">Starting...</div>
                                <div style="margin-top:4px; width:120px; height:4px; background:var(--color-border); border-radius:2px; overflow:hidden; position:relative;">
                                    <div id="sync-bar-@p.Id" style="height:100%; width:30%; background:var(--color-accent); border-radius:2px; position:absolute; animation: syncsweep 1.6s ease-in-out infinite;"></div>
                                </div>
                            }'

$dash = $dash.Replace($old9, $new9)

# Update polling JS to show sections processed
$old10 = '                    .then(data => {
                        if (data.status !== ''Syncing'') {
                            // Sync complete - reload the dashboard
                            window.location.reload();
                        }
                    })'

$new10 = '                    .then(data => {
                        if (data.status !== ''Syncing'') {
                            window.location.reload();
                        } else {
                            var countEl = document.getElementById(''sync-count-'' + projectId);
                            if (countEl && data.sectionsProcessed > 0) {
                                countEl.textContent = data.sectionsProcessed + '' sections processed'';
                            }
                        }
                    })'

$dash = $dash.Replace($old10, $new10)
Set-Content $dashPath $dash
Write-Host "  Dashboard.cshtml - sections processed counter added." -ForegroundColor White

# ---------------------------------------------------------------------------
# 7. Build
# ---------------------------------------------------------------------------
Write-Host ""
Write-Host "Building solution..." -ForegroundColor Cyan
dotnet build DraftReader.slnx --nologo -v q
if ($LASTEXITCODE -ne 0) { Write-Host "Build failed." -ForegroundColor Red; exit 1 }
Write-Host "  Builds cleanly." -ForegroundColor Green
Write-Host ""
Write-Host "Done. During sync the dashboard now shows:" -ForegroundColor Green
Write-Host "  - 'Syncing...' badge" -ForegroundColor White
Write-Host "  - 'X sections processed' counter updating every 2 seconds" -ForegroundColor White
Write-Host "  - Animated sweep progress bar" -ForegroundColor White