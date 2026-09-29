namespace CRM_DesignServices.winforms;

public class PromotionOfferItem
{
    public int? PromotionId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string OfferType { get; set; } = "Percentage";
    public decimal? OfferValue { get; set; }
    public string Description { get; set; } = string.Empty;
    public string TargetSegment { get; set; } = "Any";

    public override string ToString()
    {
        if (PromotionId == null)
            return Name;

        string valueDisplay = OfferType switch
        {
            "Percentage" => $"{OfferValue:0.##}% off",
            "FixedAmount" => $"₱{OfferValue:N0} off",
            "FreeService" => "Free Service",
            _ => "Custom"
        };

        string seg = !string.IsNullOrEmpty(TargetSegment) && !string.Equals(TargetSegment, "Any", StringComparison.OrdinalIgnoreCase)
            ? $" [{TargetSegment}]"
            : "";

        if (!string.IsNullOrEmpty(Code))
            return $"{Name} ({Code}) — {valueDisplay}{seg}";

        return $"{Name} — {valueDisplay}{seg}";
    }
}
