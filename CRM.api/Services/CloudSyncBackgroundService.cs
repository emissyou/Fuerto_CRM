using CRM.infrastructure.Services;

namespace CRM.api.Services;

/// <summary>
/// Background worker that periodically synchronizes tenant data from Local databases
/// to the Cloud storage vault, maintaining the "Local then Cloud" dual-tier architecture.
/// </summary>
public class CloudSyncBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private readonly ILogger<CloudSyncBackgroundService> _logger;

    public CloudSyncBackgroundService(
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        ILogger<CloudSyncBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        bool autoSync = _configuration.GetValue<bool>("CloudStorage:AutoSyncToCloud", true);
        if (!autoSync)
        {
            _logger.LogInformation("CloudSyncBackgroundService is disabled by configuration.");
            return;
        }

        int intervalMinutes = _configuration.GetValue<int>("CloudStorage:SyncIntervalMinutes", 15);
        if (intervalMinutes <= 0) intervalMinutes = 15;

        _logger.LogInformation("CloudSyncBackgroundService started. Interval: {Interval} minutes.", intervalMinutes);

        // Initial grace period to allow app startup and database migrations to complete
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var storage = scope.ServiceProvider.GetRequiredService<IHybridStorageService>();

                _logger.LogInformation("CloudSyncBackgroundService: Starting automatic Local ➔ Cloud replication pass...");
                var results = await storage.SyncAllTenantsToCloudAsync(stoppingToken);

                int success = results.Count(r => r.Success);
                int totalRecs = results.Where(r => r.Success).Sum(r => r.RecordsProcessed);

                _logger.LogInformation("CloudSyncBackgroundService: Completed replication. {Success}/{Total} tenants synced ({Records} total entities).",
                    success, results.Count, totalRecs);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "Error occurred during periodic Local ➔ Cloud synchronization.");
            }

            try
            {
                await Task.Delay(TimeSpan.FromMinutes(intervalMinutes), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        _logger.LogInformation("CloudSyncBackgroundService stopped.");
    }
}
