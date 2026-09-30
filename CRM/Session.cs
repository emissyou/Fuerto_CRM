using System;
using System.Collections.Generic;
using System.Linq;

namespace CRM_DesignServices.winforms;

public static class Session
{
    public static string? Token { get; set; }
    public static int? CompanyId { get; set; }
    public static string? Email { get; set; }
    public static string? CompanyName { get; set; }
    public static string? CompanyCode { get; set; }
    public static string? AvailedModules { get; set; } = "All";
    public static string? SubscriptionStatus { get; set; } = "Active";
    public static List<string> Roles { get; set; } = new();

    public static int? CurrentBranchId { get; set; }
    public static string? CurrentBranchName { get; set; } = "All Branches";
    public static bool IsOffline { get; set; } = false;
    public static bool HasAcceptedTerms { get; set; } = false;

    public static bool IsSuperAdmin => Roles != null && (Roles.Contains("Super Admin") || Roles.Contains("SuperAdmin"));
    public static bool IsAdmin => Roles != null && Roles.Contains("Admin");

    public static bool HasModule(string moduleName)
    {
        if (IsSuperAdmin) return true;
        if (string.IsNullOrWhiteSpace(AvailedModules)) return true;
        if (AvailedModules.Equals("All", StringComparison.OrdinalIgnoreCase)) return true;

        var modules = AvailedModules.Split(new[] { ',', ';', '|' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return modules.Any(m =>
            m.Equals(moduleName, StringComparison.OrdinalIgnoreCase) ||
            m.StartsWith(moduleName, StringComparison.OrdinalIgnoreCase) ||
            moduleName.StartsWith(m, StringComparison.OrdinalIgnoreCase) ||
            (moduleName.Contains("Branch", StringComparison.OrdinalIgnoreCase) && m.Contains("Branch", StringComparison.OrdinalIgnoreCase)) ||
            (moduleName.Contains("Action", StringComparison.OrdinalIgnoreCase) && m.Contains("Action", StringComparison.OrdinalIgnoreCase)) ||
            (moduleName.Contains("Transaction", StringComparison.OrdinalIgnoreCase) && m.Contains("Transaction", StringComparison.OrdinalIgnoreCase)) ||
            (moduleName.Contains("Collection", StringComparison.OrdinalIgnoreCase) && m.Contains("Collection", StringComparison.OrdinalIgnoreCase)) ||
            (moduleName.Contains("Intelligence", StringComparison.OrdinalIgnoreCase) && m.Contains("Intelligence", StringComparison.OrdinalIgnoreCase)) ||
            (moduleName.Contains("Support", StringComparison.OrdinalIgnoreCase) && m.Contains("Support", StringComparison.OrdinalIgnoreCase)) ||
            (moduleName.Contains("Inventory", StringComparison.OrdinalIgnoreCase) && m.Contains("Inventory", StringComparison.OrdinalIgnoreCase)) ||
            (moduleName.Contains("Supplies", StringComparison.OrdinalIgnoreCase) && (m.Contains("Supplies", StringComparison.OrdinalIgnoreCase) || m.Contains("Inventory", StringComparison.OrdinalIgnoreCase))) ||
            (moduleName.Contains("Audit", StringComparison.OrdinalIgnoreCase) && m.Contains("Audit", StringComparison.OrdinalIgnoreCase)) ||
            (moduleName.Contains("Compliance", StringComparison.OrdinalIgnoreCase) && (m.Contains("Compliance", StringComparison.OrdinalIgnoreCase) || m.Contains("Audit", StringComparison.OrdinalIgnoreCase)))
        );
    }
}
