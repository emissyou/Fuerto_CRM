namespace CRM.domain.Entities;

public class QuotationItem
{
    public int QuotationItemId { get; set; }

    public int QuotationId { get; set; }

    public string Description { get; set; } = string.Empty;

    public decimal Quantity { get; set; }

    public string Unit { get; set; } = string.Empty;

    public decimal UnitPrice { get; set; }

    public decimal Amount { get; set; }

    public int SortOrder { get; set; }

    public Quotation? Quotation { get; set; }
}