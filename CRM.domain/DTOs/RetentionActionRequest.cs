namespace CRM.domain.DTOs;

public class RetentionActionRequest
{
    public int CustomerId { get; set; }
    public string Segment { get; set; } = string.Empty;
    public string ActionTaken { get; set; } = string.Empty;
    public string? Basis { get; set; }
    public string? Script { get; set; }
    public DateTime? FollowUpDate { get; set; }
}   