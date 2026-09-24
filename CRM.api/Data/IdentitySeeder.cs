using CRM.domain.Entities;
using CRM.domain.Enums;
using Microsoft.AspNetCore.Identity;

namespace CRM.api.Data;

public static class IdentitySeeder
{
    public static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
    {
        foreach (var role in ApplicationRoles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }
    }

    public static async Task SeedSuperAdminAsync(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager)
    {
        const string email = "admin@fuerto.local";
        const string password = "Admin@12345";

        var existing = await userManager.FindByEmailAsync(email);
        if (existing != null) return;

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FullName = "Super Admin"
        };

        var result = await userManager.CreateAsync(user, password);
        if (result.Succeeded)
            await userManager.AddToRoleAsync(user, ApplicationRoles.SuperAdmin);
    }

    /// <summary>
    /// Seeds the 5 default staff members (formerly called "designers").
    /// Staff does design work, so they all get the Staff role.
    /// </summary>
    public static async Task SeedDesignersAsync(
        UserManager<ApplicationUser> userManager,
        int companyId)
    {
        // (FullName, Email, Password)
        var staff = new[]
        {
            ("Marco Reyes",  "marco.reyes@fuerto.local",  "Design@12345"),
            ("Sofia Lim",    "sofia.lim@fuerto.local",    "Design@12345"),
            ("Diego Santos", "diego.santos@fuerto.local", "Design@12345"),
            ("Elena Cruz",   "elena.cruz@fuerto.local",   "Design@12345"),
            ("Rafael Tan",   "rafael.tan@fuerto.local",   "Design@12345")
        };

        foreach (var (fullName, email, password) in staff)
        {
            var existing = await userManager.FindByEmailAsync(email);
            if (existing != null) continue;

            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                CompanyId = companyId,
                FullName = fullName
            };

            var result = await userManager.CreateAsync(user, password);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(user, ApplicationRoles.Staff);
            }
        }
    }
}