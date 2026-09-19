using CRM.infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace CRM.infrastructure.Services;

public class TenantDbContextFactory : ITenantDbContextFactory
{
    private readonly ITenantDatabaseResolver _resolver;
    private readonly IConfiguration _configuration;

    public TenantDbContextFactory(
        ITenantDatabaseResolver resolver,
        IConfiguration configuration)
    {
        _resolver = resolver;
        _configuration = configuration;
    }

    public async Task<TenantErpDbContext> CreateAsync(int companyId)
    {
        var databaseInfo =
            await _resolver.GetDatabaseInfoAsync(companyId);

        string connectionString;

        // LocalDB uses Windows Authentication.
        if (databaseInfo.ServerName.Contains("(localdb)",
            StringComparison.OrdinalIgnoreCase))
        {
            connectionString =
                $"Server={databaseInfo.ServerName};" +
                $"Database={databaseInfo.DatabaseName};" +
                $"Trusted_Connection=True;" +
                $"TrustServerCertificate=True;" +
                $"MultipleActiveResultSets=True;";
        }
        else
        {
            // Cloud SQL Server uses SQL Server Authentication.
            var userId = _configuration[
                $"TenantCredentials:{databaseInfo.CredentialKey}:UserId"];

            var password = _configuration[
                $"TenantCredentials:{databaseInfo.CredentialKey}:Password"];

            if (string.IsNullOrWhiteSpace(userId) ||
                string.IsNullOrWhiteSpace(password))
            {
                throw new InvalidOperationException(
                    $"Credentials not configured for '{databaseInfo.CredentialKey}'.");
            }

            connectionString =
                $"Server={databaseInfo.ServerName},1433;" +
                $"Database={databaseInfo.DatabaseName};" +
                $"User Id={userId};" +
                $"Password={password};" +
                $"Encrypt=True;" +
                $"TrustServerCertificate=True;" +
                $"MultipleActiveResultSets=True;";
        }

        var options =
            new DbContextOptionsBuilder<TenantErpDbContext>()
                .UseSqlServer(connectionString)
                .Options;

        return new TenantErpDbContext(options);
    }
}