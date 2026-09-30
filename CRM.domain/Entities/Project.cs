using CRM.domain.Enums;

namespace CRM.domain.Entities;

public class Project : CompanyEntity
{
    public int ProjectId { get; set; }

    public string ProjectCode { get; set; } = string.Empty;
    public string ProjectName { get; set; } = string.Empty;

    public int CustomerId { get; set; }
    public int? BranchId { get; set; }

    // ---- Designer assignment ----
    // NOTE: ApplicationUser lives in the Master DB (Identity).
    // We store only the UserId as string + denormalized name.
    public string? DesignerId { get; set; }
    public string? DesignerName { get; set; }
    public DateTime? DesignerAssignedAt { get; set; }
    public string? DesignerAssignedBy { get; set; }

    public string? ProjectType { get; set; }
    public string? Location { get; set; }
    public string? Description { get; set; }

    public DateTime? StartDate { get; set; }
    public DateTime? TargetEndDate { get; set; }

    public string? Status { get; set; } = "Planning";

    // Workflow progression
    public string? DesignStage { get; set; } = ProjectDesignStage.Inquiry;

    // Progress
    public int ProgressPercentage { get; set; } = 0;    // 0..100
    public DateTime? DesignStartDate { get; set; }
    public DateTime? DesignCompletionDate { get; set; }
    public string? DesignNotes { get; set; }

    // Accepted quotation link
    public int? AcceptedQuotationId { get; set; }

    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // ---- Navigation ----
    public Customer? Customer { get; set; }
}