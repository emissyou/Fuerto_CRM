using CRM.infrastructure.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace CRM.infrastructure.Services;

public class TenantDatabaseResolver : ITenantDatabaseResolver
{
    private readonly MasterErpDbContext _masterDb;

    public TenantDatabaseResolver(MasterErpDbContext masterDb)
    {
        _masterDb = masterDb;
    }

    public async Task<TenantDatabaseInfo> GetDatabaseInfoAsync(int companyId)
    {
        // 1. FAST OFFLINE CHECK: If network is offline or cloud is unreachable, use cached mappings immediately
        if (!System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable() || !TenantDbContextFactory.IsCloudReachable)
        {
            return GetFallbackDatabaseInfo(companyId);
        }

        const int MaxAttempts = 2;

        for (int attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            try
            {
                var tenantDatabase = await _masterDb.CompanyDatabases
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x =>
                        x.CompanyId == companyId &&
                        x.IsActive);

                if (tenantDatabase != null)
                {
                    return new TenantDatabaseInfo
                    {
                        ServerName = tenantDatabase.ServerName,
                        DatabaseName = tenantDatabase.DatabaseName,
                        CredentialKey = tenantDatabase.CredentialKey
                    };
                }
            }
            catch (SqlException ex) when (attempt < MaxAttempts && IsTransientConnectionError(ex))
            {
                try
                {
                    var conn = _masterDb.Database.GetDbConnection();
                    if (conn is SqlConnection sqlConn)
                        SqlConnection.ClearPool(sqlConn);
                }
                catch { /* ignore pool-clear failures */ }

                await Task.Delay(TimeSpan.FromMilliseconds(300));
            }
            catch (Exception)
            {
                // Network dropped, server unreachable, or DNS failure:
                // Instantly flag cloud as unreachable and return local/fallback mapping
                TenantDbContextFactory.IsCloudReachable = false;
                return GetFallbackDatabaseInfo(companyId);
            }
        }

        return GetFallbackDatabaseInfo(companyId);
    }

    public static TenantDatabaseInfo GetFallbackDatabaseInfo(int companyId)
    {
        return companyId switch
        {
            1 => new TenantDatabaseInfo { ServerName = "db67080.public.databaseasp.net", DatabaseName = "db67080", CredentialKey = "Fuerto" },
            2 => new TenantDatabaseInfo { ServerName = "db70838.public.databaseasp.net", DatabaseName = "db70838", CredentialKey = "GLIBahayBuilds" },
            3 => new TenantDatabaseInfo { ServerName = "db70839.public.databaseasp.net", DatabaseName = "db70839", CredentialKey = "CustomCraftersDavao" },
            _ => new TenantDatabaseInfo { ServerName = "(localdb)\\MSSQLLocalDB", DatabaseName = $"CRM_Tenant_{companyId}", CredentialKey = "Local" }
        };
    }

    /// <summary>
    /// Returns true for transient SQL errors that are safe to retry by clearing the
    /// connection pool and opening a new physical connection.
    /// </summary>
    private static bool IsTransientConnectionError(SqlException ex)
    {
        // Error 19  = Physical connection is not usable (stale pool connection)
        // Error -2  = Timeout
        // Error 20  = The instance of SQL Server you attempted to connect to does not support encryption
        // Error 233 = Connection was successfully established but during the pre-login handshake an error occurred
        // Error 10054 = Connection forcibly closed by remote host
        // Error 10060 = Connection attempt timed out
        int[] transientNumbers = { 19, -2, 20, 233, 10054, 10060 };
        return ex.Errors.Cast<SqlError>().Any(e => transientNumbers.Contains(e.Number));
    }
}