namespace CRM.domain.Entities;

public class RetentionAction : CompanyEntity
{
    public int RetentionActionId { get; set; }

    public int CustomerId { get; set; }
    public int? ProjectId { get; set; }

    public int? PromotionId { get; set; }

    // ---- Offer ----
    public string OfferType { get; set; } = "Percentage";  // Percentage, FixedAmount, FreeService, Custom
    public decimal? OfferValue { get; set; }               // 15 (for 15%), 5000 (for ₱5,000)
    public string OfferDescription { get; set; } = string.Empty;  // "15% off next project"

    // ---- Context ----
    public string Segment { get; set; } = string.Empty;    // Champion, Loyal, Manual, etc.
    public string Basis { get; set; } = string.Empty;      // Why are we retaining them?
    public string Notes { get; set; } = string.Empty;
    public string ScriptUsed { get; set; } = string.Empty; // Optional: the message script

    // ---- Source ----
    public string Source { get; set; } = "Automated";      // Automated | Manual
    public string ActionTaken { get; set; } = string.Empty;

    // ---- Status ----
    public string Status { get; set; } = "Logged";         // Logged, Contacted, Accepted, Declined, Expired
    public DateTime? FollowUpDate { get; set; }

    // ---- Audit ----
    public string CreatedByUserId { get; set; } = string.Empty;
    public string CreatedByName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // ---- Navigation ----
    public Customer? Customer { get; set; }
    public Project? Project { get; set; }
}