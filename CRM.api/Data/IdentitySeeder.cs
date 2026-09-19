using CRM.domain.Entities;
using CRM.domain.Enums;
using Microsoft.AspNetCore.Identity;

namespace CRM.api.Data;   // ← adjust to match your actual folder

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
}