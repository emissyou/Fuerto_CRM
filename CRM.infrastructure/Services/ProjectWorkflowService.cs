using CRM.domain.DTOs;
using CRM.domain.Entities;
using CRM.domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CRM.infrastructure.Services;

public class ProjectWorkflowService : IProjectWorkflowService
{
    private readonly ITenantDbContextFactory _factory;

    public ProjectWorkflowService(ITenantDbContextFactory factory)
    {
        _factory = factory;
    }

    public async Task<Project> AssignDesignerAsync(
        int companyId, int projectId, string actingUserId,
        AssignDesignerRequest request, string designerFullName)
    {
        if (string.IsNullOrWhiteSpace(request.DesignerId))
            throw new InvalidOperationException("DesignerId is required.");

        await using var db = await _factory.CreateAsync(companyId);

        var project = await db.Projects
            .FirstOrDefaultAsync(p => p.ProjectId == projectId && p.CompanyId == companyId);

        if (project == null) throw new InvalidOperationException("Project not found.");

        // ---- WORKFLOW GATE: deposit must be received ----
        var quotation = await db.Quotations
            .FirstOrDefaultAsync(q => q.QuotationId == project.AcceptedQuotationId
                                   && q.CompanyId == companyId);

        if (quotation == null)
            throw new InvalidOperationException(
                "Cannot assign designer: no accepted quotation found on this project.");

        if (quotation.PaymentStatus != PaymentStatus.DepositReceived &&
            quotation.PaymentStatus != PaymentStatus.FullyPaid)
        {
            throw new InvalidOperationException(
                "Cannot assign designer: 50% deposit has not been received yet.");
        }
        // ---------------------------------------------------

        project.DesignerId = request.DesignerId;
        project.DesignerName = designerFullName;
        project.DesignerAssignedAt = DateTime.UtcNow;
        project.DesignerAssignedBy = actingUserId;
        project.DesignStage = ProjectDesignStage.DesignerAssigned;
        project.Status = "DesignerAssigned";
        project.DesignStartDate ??= DateTime.UtcNow;

        db.Activities.Add(new Activity
        {
            CompanyId = companyId,
            ProjectId = project.ProjectId,
            CustomerId = project.CustomerId,
            ActivityType = "DesignerAssigned",
            Subject = $"Designer assigned: {designerFullName}",
            Description = request.Notes ?? string.Empty,
            ActivityDate = DateTime.UtcNow,
            Status = "Completed"
        });

        await db.SaveChangesAsync();
        return project;
    }

    public async Task<Project> UpdateProgressAsync(
        int companyId, int projectId, string actingUserId,
        UpdateProgressRequest request)
    {
        if (request.ProgressPercentage < 0 || request.ProgressPercentage > 100)
            throw new InvalidOperationException("ProgressPercentage must be between 0 and 100.");

        await using var db = await _factory.CreateAsync(companyId);

        var project = await db.Projects
            .FirstOrDefaultAsync(p => p.ProjectId == projectId && p.CompanyId == companyId);

        if (project == null) throw new InvalidOperationException("Project not found.");

        if (string.IsNullOrWhiteSpace(project.DesignerId))
            throw new InvalidOperationException(
                "Cannot update progress: no designer has been assigned yet.");

        // Progress must be monotonically non-decreasing
        if (request.ProgressPercentage < project.ProgressPercentage)
            throw new InvalidOperationException(
                $"Progress cannot go backwards. Current: {project.ProgressPercentage}%.");

        project.ProgressPercentage = request.ProgressPercentage;

        if (!string.IsNullOrWhiteSpace(request.DesignNotes))
            project.DesignNotes = request.DesignNotes!.Trim();

        // Auto-stage transitions
        if (request.ProgressPercentage > 0 && request.ProgressPercentage < 100)
        {
            project.DesignStage = ProjectDesignStage.InProgress;
            project.Status = "InProgress";
        }
        else if (request.ProgressPercentage == 100)
        {
            project.DesignStage = ProjectDesignStage.Completed;
            project.Status = "Completed";
            project.DesignCompletionDate = DateTime.UtcNow;
        }

        if (!string.IsNullOrWhiteSpace(request.Stage))
        {
            project.DesignStage = request.Stage!;
            project.Status = request.Stage!;
        }

        db.Activities.Add(new Activity
        {
            CompanyId = companyId,
            ProjectId = project.ProjectId,
            CustomerId = project.CustomerId,
            ActivityType = "ProgressUpdate",
            Subject = $"Progress updated to {project.ProgressPercentage}%",
            Description = request.DesignNotes ?? string.Empty,
            ActivityDate = DateTime.UtcNow,
            Status = "Completed"
        });

        await db.SaveChangesAsync();
        return project;
    }
}