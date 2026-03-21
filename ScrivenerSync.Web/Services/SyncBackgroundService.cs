using ScrivenerSync.Domain.Interfaces.Repositories;
using ScrivenerSync.Domain.Interfaces.Services;

namespace ScrivenerSync.Web.Services;

public class SyncBackgroundService(
    IServiceProvider serviceProvider,
    ScrivenerSyncSettings settings,
    ILogger<SyncBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation(
            "Sync background service started. Interval: {Interval} minutes.",
            settings.SyncIntervalMinutes);

        while (!stoppingToken.IsCancellationRequested)
        {
            await RunSyncAsync(stoppingToken);
            await Task.Delay(
                TimeSpan.FromMinutes(settings.SyncIntervalMinutes),
                stoppingToken);
        }
    }

    private async Task RunSyncAsync(CancellationToken ct)
    {
        try
        {
            using var scope      = serviceProvider.CreateScope();
            var projectRepo      = scope.ServiceProvider.GetRequiredService<IScrivenerProjectRepository>();
            var syncService      = scope.ServiceProvider.GetRequiredService<ISyncService>();

            var projects = await projectRepo.GetAllAsync(ct);

            foreach (var project in projects)
            {
                logger.LogDebug("Syncing project {ProjectName}...", project.Name);
                await syncService.ParseProjectAsync(project.Id, ct);
                await syncService.DetectContentChangesAsync(project.Id, ct);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Sync background service encountered an error.");
        }
    }
}
