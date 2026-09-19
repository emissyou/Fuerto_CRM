using CRM.api.Security;
using CRM.infrastructure.Data;
using CRM.infrastructure.Services;

namespace CRM.api.Endpoints;

public static class TenantEndpointHelper
{
    public static async Task<IResult> WithTenantDb(
        int companyId,
        HttpContext httpContext,
        ITenantDbContextFactory tenantFactory,
        Func<TenantErpDbContext, Task<IResult>> handler)
    {
        if (!TenantAuthorization.IsAuthorized(httpContext, companyId))
        {
            return Results.Forbid();
        }

        await using var db =
            await tenantFactory.CreateAsync(companyId);

        return await handler(db);
    }

    public static async Task<IResult> WithTenantDb(
        int companyId,
        HttpContext httpContext,
        ITenantDbContextFactory tenantFactory,
        Func<TenantErpDbContext, IResult> handler)
    {
        if (!TenantAuthorization.IsAuthorized(httpContext, companyId))
        {
            return Results.Forbid();
        }

        await using var db =
            await tenantFactory.CreateAsync(companyId);

        return handler(db);
    }
}
