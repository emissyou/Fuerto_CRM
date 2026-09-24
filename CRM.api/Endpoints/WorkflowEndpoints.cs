using CRM.api.Security;
using CRM.domain.DTOs;
using CRM.domain.Entities;
using CRM.infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace CRM.api.Endpoints;

public static class WorkflowEndpoints
{
    public static void MapWorkflowEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/tenant/{companyId}/workflow")
                       .RequireAuthorization();

        // ============================================================
        // STEP 1: Convert Lead -> Customer + Project
        // ============================================================
        group.MapPost("/leads/{leadId:int}/convert", async (
            int companyId,
            int leadId,
            [FromBody] ConvertLeadRequest request,
            HttpContext http,
            ILeadConversionService service) =>
        {
            if (!TenantAuthorization.IsAuthorized(http, companyId))
                return Results.Forbid();

            var userId = http.User.FindFirst(
                System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "";

            try
            {
                var (customer, project) = await service.ConvertAsync(
                    companyId, leadId, userId, request);

                return Results.Ok(new
                {
                    message = "Lead converted successfully.",
                    customerId = customer.CustomerId,
                    projectId = project.ProjectId,
                    projectCode = project.ProjectCode
                });
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
        });

        // ============================================================
        // STEP 2: Issue Quotation
        // ============================================================
        group.MapPost("/quotations/{quotationId:int}/issue", async (
            int companyId,
            int quotationId,
            HttpContext http,
            IQuotationWorkflowService service) =>
        {
            if (!TenantAuthorization.IsAuthorized(http, companyId))
                return Results.Forbid();

            var userId = http.User.FindFirst(
                System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "";

            try
            {
                var q = await service.IssueAsync(companyId, quotationId, userId);
                return Results.Ok(new
                {
                    message = "Quotation issued.",
                    q.QuotationId,
                    q.QuotationNumber,
                    q.Status,
                    q.TotalAmount,
                    q.DepositRequired
                });
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
        });

        // ============================================================
        // STEP 3: Accept Quotation
        // ============================================================
        group.MapPost("/quotations/{quotationId:int}/accept", async (
            int companyId,
            int quotationId,
            HttpContext http,
            IQuotationWorkflowService service) =>
        {
            if (!TenantAuthorization.IsAuthorized(http, companyId))
                return Results.Forbid();

            var userId = http.User.FindFirst(
                System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "";

            try
            {
                var q = await service.AcceptAsync(companyId, quotationId, userId);
                return Results.Ok(new
                {
                    message = "Quotation accepted. Awaiting 50% deposit.",
                    q.QuotationId,
                    q.Status,
                    q.DepositRequired
                });
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
        });

        // ============================================================
        // STEP 4: Record Payment
        // ============================================================
        group.MapPost("/quotations/{quotationId:int}/payments", async (
            int companyId,
            int quotationId,
            [FromBody] RecordPaymentRequest request,
            HttpContext http,
            IQuotationWorkflowService service) =>
        {
            if (!TenantAuthorization.IsAuthorized(http, companyId))
                return Results.Forbid();

            var userId = http.User.FindFirst(
                System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "";

            try
            {
                var q = await service.RecordPaymentAsync(
                    companyId, quotationId, userId, request);

                return Results.Ok(new
                {
                    message = "Payment recorded.",
                    q.QuotationId,
                    q.AmountPaid,
                    q.TotalAmount,
                    q.DepositRequired,
                    q.PaymentStatus,
                    depositReceived =
                        q.PaymentStatus == "DepositReceived" ||
                        q.PaymentStatus == "FullyPaid"
                });
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
        });

        // ============================================================
        // STEP 5: Assign Designer
        // ============================================================
        group.MapPost("/projects/{projectId:int}/assign-designer", async (
            int companyId,
            int projectId,
            [FromBody] AssignDesignerRequest request,
            HttpContext http,
            IProjectWorkflowService service,
            UserManager<ApplicationUser> userManager) =>
        {
            if (!TenantAuthorization.IsAuthorized(http, companyId))
                return Results.Forbid();

            var userId = http.User.FindFirst(
                System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "";

            var designer = await userManager.FindByIdAsync(request.DesignerId);
            if (designer == null)
                return Results.BadRequest(new { message = "Designer user not found." });

            var isStaff = await userManager.IsInRoleAsync(designer, "Staff");
            var isManager = await userManager.IsInRoleAsync(designer, "Manager");
            var isAdmin = await userManager.IsInRoleAsync(designer, "Admin") ||
                          await userManager.IsInRoleAsync(designer, "Super Admin");

            if (!isStaff && !isManager && !isAdmin)
                return Results.BadRequest(new { message = "Selected user cannot be assigned as a designer." });

            try
            {
                var project = await service.AssignDesignerAsync(
                    companyId, projectId, userId, request,
                    string.IsNullOrWhiteSpace(designer.FullName)
                        ? designer.Email ?? designer.UserName ?? "Designer"
                        : designer.FullName);

                return Results.Ok(new
                {
                    message = "Designer assigned.",
                    project.ProjectId,
                    project.DesignerId,
                    project.DesignerName,
                    project.DesignStage,
                    project.Status
                });
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
        });

        // ============================================================
        // STEP 6: Update Progress
        // ============================================================
        group.MapPost("/projects/{projectId:int}/progress", async (
            int companyId,
            int projectId,
            [FromBody] UpdateProgressRequest request,
            HttpContext http,
            IProjectWorkflowService service) =>
        {
            if (!TenantAuthorization.IsAuthorized(http, companyId))
                return Results.Forbid();

            var userId = http.User.FindFirst(
                System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "";

            try
            {
                var project = await service.UpdateProgressAsync(
                    companyId, projectId, userId, request);

                return Results.Ok(new
                {
                    message = "Progress updated.",
                    project.ProjectId,
                    project.ProgressPercentage,
                    project.DesignStage,
                    project.Status,
                    project.DesignNotes
                });
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
        });
    }
}