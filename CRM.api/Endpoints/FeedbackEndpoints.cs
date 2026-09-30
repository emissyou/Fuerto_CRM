using CRM.api.Security;
using CRM.domain.DTOs;
using CRM.domain.Entities;
using CRM.infrastructure.Data;
using CRM.infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace CRM.api.Endpoints;

public static class FeedbackEndpoints
{
    public static void MapFeedbackEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/tenant/{companyId:int}")
                       .RequireAuthorization();

        // ============================================================
        // SUBMIT FEEDBACK for a completed project
        // POST /tenant/{companyId}/projects/{projectId}/feedback
        // ============================================================
        group.MapPost("/projects/{projectId:int}/feedback", async (
            int companyId,
            int projectId,
            CreateFeedbackRequest request,
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

            // Only allow feedback on completed projects
            if (project.DesignStage != "Completed")
                return Results.BadRequest(new
                {
                    message = "Feedback can only be submitted for completed projects."
                });

            // Validate ratings 1-5
            foreach (var rating in new[]
            {
                request.OverallRating, request.TimelinessRating,
                request.CommunicationRating, request.ValueRating
            })
            {
                if (rating < 1 || rating > 5)
                    return Results.BadRequest(new
                    {
                        message = "All ratings must be between 1 and 5."
                    });
            }

            // Prevent duplicate feedback (one per project)
            var existing = await db.ProjectFeedbacks
                .FirstOrDefaultAsync(f => f.ProjectId == projectId
                                       && f.CompanyId == companyId);

            if (existing is not null)
                return Results.BadRequest(new
                {
                    message = "Feedback for this project has already been submitted."
                });

            var userId = http.User.FindFirst(
                System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "";

            var feedback = new ProjectFeedback
            {
                CompanyId = companyId,
                ProjectId = projectId,
                CustomerId = project.CustomerId,
                OverallRating = request.OverallRating,
                TimelinessRating = request.TimelinessRating,
                CommunicationRating = request.CommunicationRating,
                ValueRating = request.ValueRating,
                Comments = request.Comments?.Trim() ?? "",
                DesignLikes = request.DesignLikes?.Trim() ?? "",
                DesignImprovements = request.DesignImprovements?.Trim() ?? "",
                WouldRecommend = request.WouldRecommend,
                SubmittedAt = DateTime.UtcNow,
                SubmittedByUserId = userId
            };

            db.ProjectFeedbacks.Add(feedback);

            // Also log as an Activity
            db.Activities.Add(new Activity
            {
                CompanyId = companyId,
                ProjectId = projectId,
                CustomerId = project.CustomerId,
                ActivityType = "FeedbackSubmitted",
                Subject = $"Customer rated project {request.OverallRating}/5",
                Description = request.Comments ?? "",
                ActivityDate = DateTime.UtcNow,
                Status = "Completed"
            });

            await db.SaveChangesAsync();

            return Results.Created(
                $"/tenant/{companyId}/projects/{projectId}/feedback",
                feedback);
        });

        // ============================================================
        // GET FEEDBACK for a project
        // ============================================================
        group.MapGet("/projects/{projectId:int}/feedback", async (
            int companyId,
            int projectId,
            HttpContext http,
            ITenantDbContextFactory tenantFactory) =>
        {
            if (!TenantAuthorization.IsAuthorized(http, companyId))
                return Results.Forbid();

            await using var db = await tenantFactory.CreateAsync(companyId);

            var feedback = await db.ProjectFeedbacks
                .AsNoTracking()
                .Where(f => f.ProjectId == projectId && f.CompanyId == companyId)
                .ToListAsync();

            return Results.Ok(feedback);
        });

        // ============================================================
        // GET ALL FEEDBACK (feedback dashboard)
        // ============================================================
        group.MapGet("/feedback", async (
            int companyId,
            HttpContext http,
            ITenantDbContextFactory tenantFactory) =>
        {
            if (!TenantAuthorization.IsAuthorized(http, companyId))
                return Results.Forbid();

            await using var db = await tenantFactory.CreateAsync(companyId);

            var feedback = await db.ProjectFeedbacks
                .AsNoTracking()
                .Where(f => f.CompanyId == companyId)
                .OrderByDescending(f => f.ProjectFeedbackId)
                .ToListAsync();

            return Results.Ok(feedback);
        });
    }
}