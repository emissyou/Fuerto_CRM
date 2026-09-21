using CRM.api.Security;
using CRM.domain.DTOs;
using CRM.domain.Entities;
using CRM.domain.Enums;
using CRM.infrastructure.Data;
using CRM.infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CRM.api.Endpoints;

public static class UserManagementEndpoints
{
    public static void MapUserManagementEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/tenant/{companyId:int}/users")
                       .RequireAuthorization();

        // ============================================================
        // LIST USERS
        // ============================================================
        group.MapGet("", async (
            int companyId,
            HttpContext http,
            UserManager<ApplicationUser> userManager) =>
        {
            if (!TenantAuthorization.IsAuthorized(http, companyId))
                return Results.Forbid();

            if (!IsAdminOrManager(http))
                return Results.Forbid();

            var users = await userManager.Users
                .Where(u => u.CompanyId == companyId)
                .ToListAsync();

            var result = new List<object>();

            foreach (var u in users)
            {
                var roles = await userManager.GetRolesAsync(u);
                if (roles.Contains(ApplicationRoles.SuperAdmin)) continue;

                var role = roles.FirstOrDefault() ?? ApplicationRoles.Staff;

                result.Add(new
                {
                    userId = u.Id,
                    email = u.Email,
                    fullName = string.IsNullOrWhiteSpace(u.FullName) ? u.Email : u.FullName,
                    companyId = u.CompanyId,
                    role,
                    isActive = !u.LockoutEnd.HasValue || u.LockoutEnd.Value <= DateTimeOffset.UtcNow
                });
            }

            var sorted = result
                .OrderBy(r => RoleRank(((dynamic)r).role))
                .ThenBy(r => ((dynamic)r).fullName)
                .ToList();

            return Results.Ok(new
            {
                count = sorted.Count,
                admins = sorted.Count(r => ((dynamic)r).role == ApplicationRoles.Admin),
                managers = sorted.Count(r => ((dynamic)r).role == ApplicationRoles.Manager),
                staff = sorted.Count(r => ((dynamic)r).role == ApplicationRoles.Staff),
                designers = sorted.Count(r => ((dynamic)r).role == ApplicationRoles.Designer),
                users = sorted
            });
        });

        // ============================================================
        // GET ONE USER
        // ============================================================
        group.MapGet("/{userId}", async (
            int companyId,
            string userId,
            HttpContext http,
            UserManager<ApplicationUser> userManager) =>
        {
            if (!TenantAuthorization.IsAuthorized(http, companyId))
                return Results.Forbid();

            if (!IsAdminOrManager(http))
                return Results.Forbid();

            var user = await userManager.FindByIdAsync(userId);
            if (user is null || user.CompanyId != companyId)
                return Results.NotFound(new { message = "User not found." });

            var roles = await userManager.GetRolesAsync(user);

            return Results.Ok(new
            {
                userId = user.Id,
                email = user.Email,
                fullName = user.FullName,
                companyId = user.CompanyId,
                role = roles.FirstOrDefault() ?? ApplicationRoles.Staff,
                isActive = !user.LockoutEnd.HasValue || user.LockoutEnd.Value <= DateTimeOffset.UtcNow
            });
        });

        // ============================================================
        // CREATE USER
        // ============================================================
        group.MapPost("", async (
            int companyId,
            AdminCreateUserRequest request,
            HttpContext http,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager) =>
        {
            if (!TenantAuthorization.IsAuthorized(http, companyId))
                return Results.Forbid();

            if (!IsAdminOrManager(http))
                return Results.Forbid();

            if (string.IsNullOrWhiteSpace(request.Email))
                return Results.BadRequest(new { message = "Email is required." });

            if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 6)
                return Results.BadRequest(new { message = "Password must be at least 6 characters." });

            if (!ApplicationRoles.CanManageUsers.Contains(request.Role) &&
                !ApplicationRoles.ManagerCanTouch.Contains(request.Role))
            {
                return Results.BadRequest(new { message = "Invalid role." });
            }

            // If acting user is a Manager (not Admin/Super), restrict target role
            if (!IsAdmin(http) && IsManager(http))
            {
                if (!ApplicationRoles.ManagerCanTouch.Contains(request.Role))
                    return Results.BadRequest(new { message = "Managers can only create Staff or Designer accounts." });
            }

            var existing = await userManager.FindByEmailAsync(request.Email);
            if (existing != null)
                return Results.BadRequest(new { message = "Email is already registered." });

            if (!await roleManager.RoleExistsAsync(request.Role))
                return Results.BadRequest(new { message = $"Role '{request.Role}' does not exist." });

            var user = new ApplicationUser
            {
                UserName = request.Email,
                Email = request.Email,
                EmailConfirmed = true,
                FullName = string.IsNullOrWhiteSpace(request.FullName) ? request.Email : request.FullName.Trim(),
                CompanyId = companyId
            };

            var result = await userManager.CreateAsync(user, request.Password);
            if (!result.Succeeded)
                return Results.BadRequest(new { message = "Failed to create user.", errors = result.Errors.Select(e => e.Description) });

            var roleResult = await userManager.AddToRoleAsync(user, request.Role);
            if (!roleResult.Succeeded)
            {
                await userManager.DeleteAsync(user);
                return Results.BadRequest(new { message = "Failed to assign role.", errors = roleResult.Errors.Select(e => e.Description) });
            }

            return Results.Created(
                $"/tenant/{companyId}/users/{user.Id}",
                new
                {
                    message = "User created successfully.",
                    userId = user.Id,
                    email = user.Email,
                    fullName = user.FullName,
                    role = request.Role
                });
        });

        // ============================================================
        // UPDATE USER
        // ============================================================
        group.MapPut("/{userId}", async (
            int companyId,
            string userId,
            AdminUpdateUserRequest request,
            HttpContext http,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager) =>
        {
            if (!TenantAuthorization.IsAuthorized(http, companyId))
                return Results.Forbid();

            if (!IsAdminOrManager(http))
                return Results.Forbid();

            var user = await userManager.FindByIdAsync(userId);
            if (user is null || user.CompanyId != companyId)
                return Results.NotFound(new { message = "User not found." });

            var targetRoles = await userManager.GetRolesAsync(user);
            var targetCurrentRole = targetRoles.FirstOrDefault() ?? ApplicationRoles.Staff;

            if (targetRoles.Contains(ApplicationRoles.SuperAdmin))
                return Results.BadRequest(new { message = "Cannot edit a Super Admin." });

            // Manager restrictions
            if (!IsAdmin(http) && IsManager(http))
            {
                // Manager cannot touch Admins or other Managers
                if (targetCurrentRole == ApplicationRoles.Admin || targetCurrentRole == ApplicationRoles.Manager)
                    return Results.BadRequest(new { message = "Managers cannot edit Admin or Manager accounts." });

                // Manager cannot promote anyone TO Admin or Manager
                if (!ApplicationRoles.ManagerCanTouch.Contains(request.Role))
                    return Results.BadRequest(new { message = "Managers can only assign Staff or Designer roles." });
            }

            if (string.IsNullOrWhiteSpace(request.Role))
                return Results.BadRequest(new { message = "Role is required." });

            // Email change
            if (!string.Equals(user.Email, request.Email, StringComparison.OrdinalIgnoreCase))
            {
                var byEmail = await userManager.FindByEmailAsync(request.Email);
                if (byEmail != null && byEmail.Id != user.Id)
                    return Results.BadRequest(new { message = "Email is already used by another user." });

                user.Email = request.Email.Trim();
                user.UserName = request.Email.Trim();
                user.NormalizedEmail = request.Email.Trim().ToUpperInvariant();
                user.NormalizedUserName = request.Email.Trim().ToUpperInvariant();
            }

            user.FullName = string.IsNullOrWhiteSpace(request.FullName) ? user.Email ?? "" : request.FullName.Trim();

            if (request.IsActive)
                user.LockoutEnd = null;
            else
                user.LockoutEnd = DateTimeOffset.UtcNow.AddYears(100);

            var updateResult = await userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
                return Results.BadRequest(new { message = "Update failed.", errors = updateResult.Errors.Select(e => e.Description) });

            // Role change
            if (!string.Equals(targetCurrentRole, request.Role, StringComparison.OrdinalIgnoreCase))
            {
                if (targetCurrentRole == ApplicationRoles.Admin)
                {
                    var adminCount = await CountAdminsAsync(companyId, userManager);
                    if (adminCount <= 1)
                        return Results.BadRequest(new { message = "Cannot change role: this is the last Admin in the company." });
                }

                if (!string.IsNullOrEmpty(targetCurrentRole))
                    await userManager.RemoveFromRoleAsync(user, targetCurrentRole);

                if (!await roleManager.RoleExistsAsync(request.Role))
                    return Results.BadRequest(new { message = $"Role '{request.Role}' does not exist." });

                var roleResult = await userManager.AddToRoleAsync(user, request.Role);
                if (!roleResult.Succeeded)
                    return Results.BadRequest(new { message = "Role change failed.", errors = roleResult.Errors.Select(e => e.Description) });
            }

            return Results.Ok(new
            {
                message = "User updated successfully.",
                userId = user.Id,
                email = user.Email,
                fullName = user.FullName,
                role = request.Role,
                isActive = request.IsActive
            });
        });

        // ============================================================
        // RESET PASSWORD
        // ============================================================
        group.MapPost("/{userId}/reset-password", async (
            int companyId,
            string userId,
            AdminResetPasswordRequest request,
            HttpContext http,
            UserManager<ApplicationUser> userManager) =>
        {
            if (!TenantAuthorization.IsAuthorized(http, companyId))
                return Results.Forbid();

            if (!IsAdminOrManager(http))
                return Results.Forbid();

            if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 6)
                return Results.BadRequest(new { message = "Password must be at least 6 characters." });

            var user = await userManager.FindByIdAsync(userId);
            if (user is null || user.CompanyId != companyId)
                return Results.NotFound(new { message = "User not found." });

            var roles = await userManager.GetRolesAsync(user);
            if (roles.Contains(ApplicationRoles.SuperAdmin))
                return Results.BadRequest(new { message = "Cannot reset a Super Admin password." });

            // Manager restrictions
            if (!IsAdmin(http) && IsManager(http))
            {
                var targetRole = roles.FirstOrDefault() ?? "";
                if (targetRole == ApplicationRoles.Admin || targetRole == ApplicationRoles.Manager)
                    return Results.BadRequest(new { message = "Managers cannot reset Admin or Manager passwords." });
            }

            var token = await userManager.GeneratePasswordResetTokenAsync(user);
            var result = await userManager.ResetPasswordAsync(user, token, request.NewPassword);

            if (!result.Succeeded)
                return Results.BadRequest(new { message = "Password reset failed.", errors = result.Errors.Select(e => e.Description) });

            return Results.Ok(new { message = "Password reset successfully.", userId = user.Id, email = user.Email });
        });

        // ============================================================
        // DELETE USER
        // ============================================================
        group.MapDelete("/{userId}", async (
            int companyId,
            string userId,
            HttpContext http,
            UserManager<ApplicationUser> userManager,
            ITenantDbContextFactory tenantFactory) =>
        {
            if (!TenantAuthorization.IsAuthorized(http, companyId))
                return Results.Forbid();

            if (!IsAdminOrManager(http))
                return Results.Forbid();

            var user = await userManager.FindByIdAsync(userId);
            if (user is null || user.CompanyId != companyId)
                return Results.NotFound(new { message = "User not found." });

            var currentUserId = http.User.FindFirst(
                System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.Equals(currentUserId, userId, StringComparison.Ordinal))
                return Results.BadRequest(new { message = "You cannot delete your own account." });

            var roles = await userManager.GetRolesAsync(user);
            var targetRole = roles.FirstOrDefault() ?? ApplicationRoles.Staff;

            if (roles.Contains(ApplicationRoles.SuperAdmin))
                return Results.BadRequest(new { message = "Cannot delete a Super Admin." });

            // Manager restrictions
            if (!IsAdmin(http) && IsManager(http))
            {
                if (targetRole == ApplicationRoles.Admin || targetRole == ApplicationRoles.Manager)
                    return Results.BadRequest(new { message = "Managers cannot delete Admin or Manager accounts." });
            }

            // Last-admin check
            if (targetRole == ApplicationRoles.Admin)
            {
                var adminCount = await CountAdminsAsync(companyId, userManager);
                if (adminCount <= 1)
                    return Results.BadRequest(new { message = "Cannot delete the last Admin in the company." });
            }

            // Designer safety check
            if (targetRole == ApplicationRoles.Designer)
            {
                await using var db = await tenantFactory.CreateAsync(companyId);
                var activeProjects = await db.Projects
                    .Where(p => p.CompanyId == companyId
                             && p.DesignerId == userId
                             && p.DesignStage != "Completed"
                             && p.DesignStage != "Cancelled")
                    .CountAsync();

                if (activeProjects > 0)
                    return Results.BadRequest(new
                    {
                        message = $"Cannot delete: designer has {activeProjects} active project(s). Reassign them first."
                    });
            }

            var result = await userManager.DeleteAsync(user);
            if (!result.Succeeded)
                return Results.BadRequest(new { message = "Delete failed.", errors = result.Errors.Select(e => e.Description) });

            return Results.Ok(new { message = "User deleted successfully.", userId = userId, email = user.Email });
        });
    }

    // ============================================================
    // HELPERS
    // ============================================================
    private static bool IsAdmin(HttpContext http) =>
        http.User.IsInRole(ApplicationRoles.Admin) || http.User.IsInRole(ApplicationRoles.SuperAdmin);

    private static bool IsManager(HttpContext http) =>
        http.User.IsInRole(ApplicationRoles.Manager);

    private static bool IsAdminOrManager(HttpContext http) =>
        IsAdmin(http) || IsManager(http);

    private static int RoleRank(string role) => role switch
    {
        ApplicationRoles.Admin => 1,
        ApplicationRoles.Manager => 2,
        ApplicationRoles.Staff => 3,
        ApplicationRoles.Designer => 4,
        _ => 99
    };

    private static async Task<int> CountAdminsAsync(int companyId, UserManager<ApplicationUser> userManager)
    {
        var admins = await userManager.GetUsersInRoleAsync(ApplicationRoles.Admin);
        return admins.Count(u => u.CompanyId == companyId);
    }
}