using CRM.api.Security;
using CRM.domain.DTOs;
using CRM.domain.Entities;
using CRM.domain.Enums;
using CRM.infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace CRM.api.Endpoints;

public static class IssueEndpoints
{
    public static void MapIssueEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/tenant/{companyId:int}")
                       .RequireAuthorization();

        // ============================================================
        // REPORT ISSUE (Complaint / Adjustment / Payment Dispute)
        // POST /tenant/{companyId}/projects/{projectId}/issues
        // ============================================================
        group.MapPost("/projects/{projectId:int}/issues", async (
            int companyId,
            int projectId,
            CreateIssueRequest request,
            HttpContext http,
            ITenantDbContextFactory tenantFactory) =>
        {
            if (!TenantAuthorization.IsAuthorized(http, companyId))
                return Results.Forbid();

            await using var db = await tenantFactory.CreateAsync(companyId);

            var project = await db.Projects
                .FirstOrDefaultAsync(p => p.ProjectId == projectId
                                       && p.CompanyId == companyId);

            if (project is null)
                return Results.NotFound(new { message = "Project not found." });

            if (string.IsNullOrWhiteSpace(request.Title))
                return Results.BadRequest(new { message = "Title is required." });

            var userId = http.User.FindFirst(
                System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "";

            var issue = new ProjectIssue
            {
                CompanyId = companyId,
                ProjectId = projectId,
                CustomerId = project.CustomerId,
                IssueType = string.IsNullOrWhiteSpace(request.IssueType)
                    ? IssueType.Complaint
                    : request.IssueType,
                Severity = string.IsNullOrWhiteSpace(request.Severity)
                    ? IssueSeverity.Medium
                    : request.Severity,
                Status = IssueStatus.Open,
                Title = request.Title.Trim(),
                Description = request.Description?.Trim() ?? "",
                RequestedAction = request.RequestedAction?.Trim() ?? "",
                DisputedAmount = request.DisputedAmount,
                PaymentReference = request.PaymentReference,
                TargetResolutionDate = request.TargetResolutionDate,
                ReportedByUserId = userId,
                ReportedAt = DateTime.UtcNow,
                IsActive = true
            };

            db.ProjectIssues.Add(issue);

            db.Activities.Add(new Activity
            {
                CompanyId = companyId,
                ProjectId = projectId,
                CustomerId = project.CustomerId,
                ActivityType = "IssueReported",
                Subject = $"{issue.IssueType}: {issue.Title}",
                Description = issue.Description,
                ActivityDate = DateTime.UtcNow,
                Status = "Open"
            });

            await db.SaveChangesAsync();

            return Results.Created(
                $"/tenant/{companyId}/issues/{issue.ProjectIssueId}",
                issue);
        });

        // ============================================================
        // LIST ISSUES (per company, filterable)
        // GET /tenant/{companyId}/issues?status=Open&projectId=1
        // ============================================================
        group.MapGet("/issues", async (
            int companyId,
            HttpContext http,
            ITenantDbContextFactory tenantFactory,
            string? status = null,
            string? type = null,
            int? projectId = null) =>
        {
            if (!TenantAuthorization.IsAuthorized(http, companyId))
                return Results.Forbid();

            await using var db = await tenantFactory.CreateAsync(companyId);

            var query = db.ProjectIssues
                .AsNoTracking()
                .Where(i => i.CompanyId == companyId);

            if (!string.IsNullOrWhiteSpace(status))
                query = query.Where(i => i.Status == status);

            if (!string.IsNullOrWhiteSpace(type))
                query = query.Where(i => i.IssueType == type);

            if (projectId.HasValue)
                query = query.Where(i => i.ProjectId == projectId.Value);

            var issues = await query
                .OrderByDescending(i => i.ReportedAt)
                .ToListAsync();

            return Results.Ok(issues);
        });

        // ============================================================
        // GET ONE ISSUE
        // ============================================================
        group.MapGet("/issues/{issueId:int}", async (
            int companyId,
            int issueId,
            HttpContext http,
            ITenantDbContextFactory tenantFactory) =>
        {
            if (!TenantAuthorization.IsAuthorized(http, companyId))
                return Results.Forbid();

            await using var db = await tenantFactory.CreateAsync(companyId);

            var issue = await db.ProjectIssues
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.ProjectIssueId == issueId
                                       && i.CompanyId == companyId);

            return issue is null
                ? Results.NotFound(new { message = "Issue not found." })
                : Results.Ok(issue);
        });

        // ============================================================
        // RESOLVE / CLOSE / REJECT ISSUE
        // POST /tenant/{companyId}/issues/{issueId}/resolve
        // ============================================================
        group.MapPost("/issues/{issueId:int}/resolve", async (
            int companyId,
            int issueId,
            ResolveIssueRequest request,
            HttpContext http,
            ITenantDbContextFactory tenantFactory) =>
        {
            if (!TenantAuthorization.IsAuthorized(http, companyId))
                return Results.Forbid();

            await using var db = await tenantFactory.CreateAsync(companyId);

            var issue = await db.ProjectIssues
                .FirstOrDefaultAsync(i => i.ProjectIssueId == issueId
                                       && i.CompanyId == companyId);

            if (issue is null)
                return Results.NotFound(new { message = "Issue not found." });

            var allowed = new[]
            {
                IssueStatus.Resolved, IssueStatus.Closed, IssueStatus.Rejected
            };

            if (!allowed.Contains(request.Status))
                return Results.BadRequest(new
                {
                    message = "Status must be Resolved, Closed, or Rejected."
                });

            var userId = http.User.FindFirst(
                System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "";

            issue.Status = request.Status;
            issue.ResolutionNotes = request.ResolutionNotes?.Trim() ?? "";
            issue.ResolvedByUserId = userId;
            issue.ResolvedAt = DateTime.UtcNow;

            db.Activities.Add(new Activity
            {
                CompanyId = companyId,
                ProjectId = issue.ProjectId,
                CustomerId = issue.CustomerId,
                ActivityType = "IssueResolved",
                Subject = $"{issue.IssueType} {request.Status}: {issue.Title}",
                Description = issue.ResolutionNotes,
                ActivityDate = DateTime.UtcNow,
                Status = request.Status
            });

            await db.SaveChangesAsync();

            return Results.Ok(issue);
        });

        // ============================================================
        // CHANGE STATUS (In Progress, etc.)
        // POST /tenant/{companyId}/issues/{issueId}/status
        // ============================================================
        group.MapPost("/issues/{issueId:int}/status", async (
            int companyId,
            int issueId,
            string newStatus,
            HttpContext http,
            ITenantDbContextFactory tenantFactory) =>
        {
            if (!TenantAuthorization.IsAuthorized(http, companyId))
                return Results.Forbid();

            var allStatuses = new[]
            {
                IssueStatus.Open, IssueStatus.InProgress,
                IssueStatus.Resolved, IssueStatus.Closed,
                IssueStatus.Rejected
            };

            if (!allStatuses.Contains(newStatus))
                return Results.BadRequest(new { message = "Invalid status." });

            await using var db = await tenantFactory.CreateAsync(companyId);

            var issue = await db.ProjectIssues
                .FirstOrDefaultAsync(i => i.ProjectIssueId == issueId
                                       && i.CompanyId == companyId);

            if (issue is null)
                return Results.NotFound(new { message = "Issue not found." });

            issue.Status = newStatus;

            await db.SaveChangesAsync();

            return Results.Ok(issue);
        });
    }
}