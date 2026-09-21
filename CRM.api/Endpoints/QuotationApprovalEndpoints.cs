using CRM.api.Security;
using CRM.domain.DTOs;
using CRM.domain.Entities;
using CRM.domain.Enums;
using CRM.infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CRM.api.Endpoints;

public static class QuotationApprovalEndpoints
{
    public static void MapQuotationApprovalEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/tenant/{companyId:int}/quotations")
                       .RequireAuthorization();

        // ============================================================
        // APPROVE QUOTATION
        // POST /tenant/{companyId}/quotations/{id}/approve
        // ============================================================
        group.MapPost("/{id:int}/approve", async (
            int companyId,
            int id,
            ApproveQuotationRequest request,
            HttpContext http,
            ITenantDbContextFactory tenantFactory,
            UserManager<ApplicationUser> userManager) =>
        {
            if (!TenantAuthorization.IsAuthorized(http, companyId))
                return Results.Forbid();

            // Only Admin, Super Admin, and Manager can approve
            if (!http.User.IsInRole(ApplicationRoles.Admin)
                && !http.User.IsInRole(ApplicationRoles.SuperAdmin)
                && !http.User.IsInRole(ApplicationRoles.Manager))
            {
                return Results.Forbid();
            }

            await using var db = await tenantFactory.CreateAsync(companyId);

            var quotation = await db.Quotations
                .FirstOrDefaultAsync(q => q.QuotationId == id && q.CompanyId == companyId);

            if (quotation is null)
                return Results.NotFound(new { message = "Quotation not found." });

            if (quotation.ApprovalStatus == "Approved")
                return Results.BadRequest(new { message = "Quotation is already approved." });

            var userId = http.User.FindFirst(
                System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "";

            var user = await userManager.FindByIdAsync(userId);
            var approverName = user?.FullName ?? user?.Email ?? "Manager";

            quotation.ApprovalStatus = "Approved";
            quotation.ApprovedByUserId = userId;
            quotation.ApprovedByName = approverName;
            quotation.ApprovedAt = DateTime.UtcNow;
            quotation.RejectionReason = string.Empty;
            quotation.Status = "Issued";

            // Auto-fill IssuedAt if not set
            quotation.IssuedAt ??= DateTime.UtcNow;
            quotation.IssuedByUserId = userId;

            // Log as activity
            db.Activities.Add(new Activity
            {
                CompanyId = companyId,
                ProjectId = quotation.ProjectId,
                CustomerId = quotation.CustomerId,
                ActivityType = "QuotationApproved",
                Subject = $"Quotation {quotation.QuotationNumber} approved",
                Description = string.IsNullOrWhiteSpace(request.Notes)
                    ? $"Approved by {approverName}"
                    : $"Approved by {approverName}: {request.Notes}",
                ActivityDate = DateTime.UtcNow,
                Status = "Completed"
            });

            await db.SaveChangesAsync();

            return Results.Ok(new
            {
                message = "Quotation approved successfully.",
                quotationId = quotation.QuotationId,
                quotationNumber = quotation.QuotationNumber,
                approvalStatus = quotation.ApprovalStatus,
                approvedBy = approverName,
                approvedAt = quotation.ApprovedAt,
                newStatus = quotation.Status
            });
        });

        // ============================================================
        // REJECT QUOTATION
        // POST /tenant/{companyId}/quotations/{id}/reject
        // ============================================================
        group.MapPost("/{id:int}/reject", async (
            int companyId,
            int id,
            RejectQuotationRequest request,
            HttpContext http,
            ITenantDbContextFactory tenantFactory,
            UserManager<ApplicationUser> userManager) =>
        {
            if (!TenantAuthorization.IsAuthorized(http, companyId))
                return Results.Forbid();

            if (!http.User.IsInRole(ApplicationRoles.Admin)
                && !http.User.IsInRole(ApplicationRoles.SuperAdmin)
                && !http.User.IsInRole(ApplicationRoles.Manager))
            {
                return Results.Forbid();
            }

            if (string.IsNullOrWhiteSpace(request.Reason))
                return Results.BadRequest(new { message = "Rejection reason is required." });

            await using var db = await tenantFactory.CreateAsync(companyId);

            var quotation = await db.Quotations
                .FirstOrDefaultAsync(q => q.QuotationId == id && q.CompanyId == companyId);

            if (quotation is null)
                return Results.NotFound(new { message = "Quotation not found." });

            if (quotation.ApprovalStatus == "Approved")
                return Results.BadRequest(new { message = "Cannot reject an already approved quotation." });

            var userId = http.User.FindFirst(
                System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "";

            var user = await userManager.FindByIdAsync(userId);
            var reviewerName = user?.FullName ?? user?.Email ?? "Manager";

            quotation.ApprovalStatus = "Rejected";
            quotation.ApprovedByUserId = userId;
            quotation.ApprovedByName = reviewerName;
            quotation.ApprovedAt = DateTime.UtcNow;
            quotation.RejectionReason = request.Reason.Trim();
            quotation.Status = QuotationStatus.Draft; // stay draft

            db.Activities.Add(new Activity
            {
                CompanyId = companyId,
                ProjectId = quotation.ProjectId,
                CustomerId = quotation.CustomerId,
                ActivityType = "QuotationRejected",
                Subject = $"Quotation {quotation.QuotationNumber} rejected",
                Description = $"Rejected by {reviewerName}: {request.Reason}",
                ActivityDate = DateTime.UtcNow,
                Status = "Completed"
            });

            await db.SaveChangesAsync();

            return Results.Ok(new
            {
                message = "Quotation rejected.",
                quotationId = quotation.QuotationId,
                quotationNumber = quotation.QuotationNumber,
                approvalStatus = quotation.ApprovalStatus,
                rejectedBy = reviewerName,
                reason = quotation.RejectionReason
            });
        });
    }
}