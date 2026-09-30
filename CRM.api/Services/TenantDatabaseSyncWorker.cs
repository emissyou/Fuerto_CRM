using CRM.infrastructure.Services;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CRM.api.Services;

/// <summary>
/// Background worker that continuously monitors internet/cloud connectivity
/// and automatically synchronizes LocalDB and Cloud databases whenever online.
/// </summary>
public class TenantDatabaseSyncWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private readonly ILogger<TenantDatabaseSyncWorker> _logger;
    private bool _wasCloudReachable = true;

    public TenantDatabaseSyncWorker(
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        ILogger<TenantDatabaseSyncWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("TenantDatabaseSyncWorker active.");

        // Initial delay before first probe
        await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                bool isReachable = await ProbeCloudAsync(stoppingToken);
                TenantDbContextFactory.IsCloudReachable = isReachable;

                if (isReachable && !_wasCloudReachable)
                {
                    _logger.LogInformation("🟢 Internet and Cloud database connectivity restored! Reconciling offline records to Cloud...");
                    using var scope = _serviceProvider.CreateScope();
                    var syncService = scope.ServiceProvider.GetRequiredService<ITenantDatabaseSyncService>();
                    await syncService.SyncAllTenantsAsync(stoppingToken);
                }
                else if (!isReachable && _wasCloudReachable)
                {
                    _logger.LogWarning("🟡 Cloud database unreachable (Offline). Operating in LocalDB fallback mode.");
                }
                else if (isReachable)
                {
                    // Periodic reconciliation every sync cycle while online
                    using var scope = _serviceProvider.CreateScope();
                    var syncService = scope.ServiceProvider.GetRequiredService<ITenantDatabaseSyncService>();
                    await syncService.SyncAllTenantsAsync(stoppingToken);
                }

                _wasCloudReachable = isReachable;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Background tenant database sync check skipped: {Message}", ex.Message);
            }

            // Re-check connectivity frequently when offline (3s) or periodic sync when online (8s)
            await Task.Delay(TimeSpan.FromSeconds(TenantDbContextFactory.IsCloudReachable ? 8 : 3), stoppingToken);
        }
    }

    private async Task<bool> ProbeCloudAsync(CancellationToken ct)
    {
        // Instant hardware check: if no network interface is active, we are immediately offline
        if (!System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
        {
            return false;
        }

        string? cloudConn = _configuration.GetConnectionString("Tenant_1") 
            ?? _configuration.GetConnectionString("MasterErp");

        if (string.IsNullOrWhiteSpace(cloudConn)) return false;

        try
        {
            var csb = new SqlConnectionStringBuilder(cloudConn) { ConnectTimeout = 8 };
            await using var conn = new SqlConnection(csb.ConnectionString);
            await conn.OpenAsync(ct);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
