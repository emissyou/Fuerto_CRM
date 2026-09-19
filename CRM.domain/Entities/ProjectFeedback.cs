namespace CRM.domain.Entities;

public class ProjectFeedback : CompanyEntity
{
    public int ProjectFeedbackId { get; set; }

    public int ProjectId { get; set; }
    public int CustomerId { get; set; }

    // ---- Star ratings (1-5) ----
    public int OverallRating { get; set; }   // 1-5
    public int TimelinessRating { get; set; }   // 1-5
    public int CommunicationRating { get; set; }   // 1-5
    public int ValueRating { get; set; }   // 1-5

    // ---- Free text ----
    public string Comments { get; set; } = string.Empty;
    public string DesignLikes { get; set; } = string.Empty;   // what they liked
    public string DesignImprovements { get; set; } = string.Empty; // what to improve

    public bool WouldRecommend { get; set; }

    // ---- Submission ----
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    public string SubmittedByUserId { get; set; } = string.Empty;

    // ---- Navigation ----
    public Project? Project { get; set; }
    public Customer? Customer { get; set; }
}