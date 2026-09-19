using CRM.infrastructure.Services;

namespace CRM.infrastructure.Services;

public interface ITenantDatabaseResolver
{
    Task<TenantDatabaseInfo> GetDatabaseInfoAsync(int companyId);
}