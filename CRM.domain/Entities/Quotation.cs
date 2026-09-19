using CRM.domain.Enums;

namespace CRM.domain.Entities;

public class Quotation : CompanyEntity
{
    public int QuotationId { get; set; }

    public string QuotationNumber { get; set; } = string.Empty;

    public int ProjectId { get; set; }
    public int CustomerId { get; set; }

    public DateTime QuotationDate { get; set; } = DateTime.UtcNow;

    public decimal Subtotal { get; set; }
    public decimal Discount { get; set; }
    public decimal TotalAmount { get; set; }

    public string Status { get; set; } = QuotationStatus.Draft;

    // ---- Payment ----
    public decimal AmountPaid { get; set; } = 0;
    public decimal DepositRequired { get; set; } = 0;   // usually 50% of TotalAmount
    public string PaymentStatus { get; set; } = Enums.PaymentStatus.Pending;

    public DateTime? DepositPaidDate { get; set; }
    public DateTime? FullyPaidDate { get; set; }

    public string PaymentMethod { get; set; } = string.Empty;
    public string PaymentReference { get; set; } = string.Empty;

    public string Notes { get; set; } = string.Empty;

    public DateTime? ValidUntil { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // ---- Issuance ----
    public DateTime? IssuedAt { get; set; }
    public string IssuedByUserId { get; set; } = string.Empty;

    public DateTime? AcceptedAt { get; set; }

    // ---- Navigation ----
    public Project? Project { get; set; }
    public Customer? Customer { get; set; }
}