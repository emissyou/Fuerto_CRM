namespace CRM.domain.Entities;

public class ProjectIssue : CompanyEntity
{
    public int ProjectIssueId { get; set; }

    public int ProjectId { get; set; }
    public int CustomerId { get; set; }

    // ---- Classification ----
    public string IssueType { get; set; } = "Complaint";  // Complaint, Adjustment, PaymentDispute, Rework, Other
    public string Severity { get; set; } = "Medium";     // Low, Medium, High, Critical
    public string Status { get; set; } = "Open";       // Open, InProgress, Resolved, Closed, Rejected

    // ---- Content ----
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    // ---- Payment dispute fields (optional) ----
    public decimal? DisputedAmount { get; set; }   // only for PaymentDispute
    public string? PaymentReference { get; set; }

    // ---- Resolution ----
    public string ResolutionNotes { get; set; } = string.Empty;
    public string ResolvedByUserId { get; set; } = string.Empty;
    public DateTime? ResolvedAt { get; set; }

    // ---- Requested action ----
    public string RequestedAction { get; set; } = string.Empty;  // e.g. "Rework wall color"

    // ---- Meta ----
    public string ReportedByUserId { get; set; } = string.Empty;
    public DateTime ReportedAt { get; set; } = DateTime.UtcNow;
    public DateTime? TargetResolutionDate { get; set; }

    public bool IsActive { get; set; } = true;

    // ---- Navigation ----
    public Project? Project { get; set; }
    public Customer? Customer { get; set; }
}