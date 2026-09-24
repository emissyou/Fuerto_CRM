namespace CRM.domain.Entities;

public class Promotion : CompanyEntity
{
    public int PromotionId { get; set; }

    public string Name { get; set; } = string.Empty;          // "Summer 2026 Loyalty"
    public string Code { get; set; } = string.Empty;          // "SUMMER26"
    public string Description { get; set; } = string.Empty;

    // ---- Offer ----
    public string OfferType { get; set; } = "Percentage";     // Percentage | FixedAmount | FreeService | Custom
    public decimal? OfferValue { get; set; }                  // 15 or 5000

    // ---- Audience ----
    public string TargetSegment { get; set; } = "Any";        // Any | Champion | Loyal | Promising | Detractor | AtRisk | Dormant | Lost | Active

    // ---- Validity ----
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidUntil { get; set; }
    public bool IsActive { get; set; } = true;

    // ---- Usage limits ----
    public int? MaxUses { get; set; }                          // null = unlimited
    public int UsedCount { get; set; } = 0;

    // ---- Meta ----
    public string Notes { get; set; } = string.Empty;
    public string CreatedByUserId { get; set; } = string.Empty;
    public string CreatedByName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}