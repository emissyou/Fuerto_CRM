namespace CRM.domain.Entities;

public class Activity : CompanyEntity
{
    public int ActivityId { get; set; }

    public int? CustomerId { get; set; }

    public int? LeadId { get; set; }

    public int? ProjectId { get; set; }

    public int? BranchId { get; set; }

    public string ActivityType { get; set; } = "Other";

    public string Subject { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public DateTime ActivityDate { get; set; } = DateTime.UtcNow;

    public DateTime? FollowUpDate { get; set; }

    public string Status { get; set; } = "Completed";

    public string Notes { get; set; } = string.Empty;

    public Customer? Customer { get; set; }

    public Lead? Lead { get; set; }

    public Project? Project { get; set; }
}