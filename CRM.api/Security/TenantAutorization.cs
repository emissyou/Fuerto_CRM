namespace CRM.api.Security;

public static class TenantAuthorization
{
    public static bool IsAuthorized(
        HttpContext httpContext,
        int companyId)
    {
        // Super Admin can access any company
        if (httpContext.User.IsInRole("Super Admin"))
        {
            return true;
        }

        // Admin and Staff can only access their own company
        var companyClaim = httpContext.User.FindFirst("CompanyId")?.Value;

        return int.TryParse(companyClaim, out var userCompanyId)
            && userCompanyId == companyId;
    }
}