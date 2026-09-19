namespace CRM.domain.DTOs;

public class CreateFeedbackRequest
{
    public int ProjectId { get; set; }
    public int OverallRating { get; set; }
    public int TimelinessRating { get; set; }
    public int CommunicationRating { get; set; }
    public int ValueRating { get; set; }
    public string Comments { get; set; } = string.Empty;
    public string DesignLikes { get; set; } = string.Empty;
    public string DesignImprovements { get; set; } = string.Empty;
    public bool WouldRecommend { get; set; }
}