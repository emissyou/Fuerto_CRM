namespace CRM.domain.DTOs;

public class CreateIssueRequest
{
    public int ProjectId { get; set; }
    public int? CustomerId { get; set; }
    public string IssueType { get; set; } = "Complaint";
    public string Severity { get; set; } = "Medium";
    public string Status { get; set; } = "Open";
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string RequestedAction { get; set; } = string.Empty;
    public decimal? DisputedAmount { get; set; }
    public string? PaymentReference { get; set; }
    public DateTime? TargetResolutionDate { get; set; }
}