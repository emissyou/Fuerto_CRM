using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CRM.infrastructure.Data;

public class TenantErpDbContextFactory
    : IDesignTimeDbContextFactory<TenantErpDbContext>
{
    public TenantErpDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("TENANT_DB_CONNECTION");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "TENANT_DB_CONNECTION environment variable is not configured.");
        }

        var options =
            new DbContextOptionsBuilder<TenantErpDbContext>()
                .UseSqlServer(connectionString)
                .Options;

        return new TenantErpDbContext(options);
    }
}