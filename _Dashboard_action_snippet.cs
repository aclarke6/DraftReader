    // ---------------------------------------------------------------------------
    // Dashboard
    // ---------------------------------------------------------------------------
    public async Task<IActionResult> Dashboard()
    {
        var author = await GetAuthorAsync();
        if (author is null)
            return RedirectToAction("Index", "Reader");

        var projects          = await projectRepo.GetAllAsync();
        var active            = await projectRepo.GetReaderActiveProjectAsync();
        var publishedChapters = active is not null
            ? await publicationService.GetPublishedChaptersAsync(active.Id)
            : new List<Section>();
        var failures      = await dashboardService.GetEmailHealthSummaryAsync();
        var readers       = await userRepo.GetAllBetaReadersAsync();
        var notifications = await dashboardService.GetRecentNotificationsAsync(author.Id, maxItems: 20);

        return View(new DashboardViewModel
        {
            ActiveProject     = active,
            AllProjects       = projects,
            PublishedSections = publishedChapters,
            EmailFailures     = failures,
            ActiveReaderCount = readers.Count(r => r.IsActive && !r.IsSoftDeleted),
            Notifications     = notifications
        });
    }
