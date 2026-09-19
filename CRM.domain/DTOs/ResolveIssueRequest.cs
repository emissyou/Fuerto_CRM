namespace CRM.domain.DTOs;

public class ResolveIssueRequest
{
    public string Status { get; set; } = "Resolved";  // Resolved, Closed, Rejected
    public string ResolutionNotes { get; set; } = string.Empty;
}