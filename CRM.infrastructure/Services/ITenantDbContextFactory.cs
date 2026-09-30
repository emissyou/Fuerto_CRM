using CRM.infrastructure.Data;

namespace CRM.infrastructure.Services;

public interface ITenantDbContextFactory
{
    Task<TenantErpDbContext> CreateAsync(int companyId);
    Task<TenantErpDbContext?> CreateLocalAsync(int companyId);
    Task<TenantErpDbContext?> CreateCloudAsync(int companyId);
}