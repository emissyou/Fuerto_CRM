namespace CRM.api.Models;

public class QuotationPaymentRequest
{
    public decimal AmountPaid { get; set; }

    public string? PaymentMethod { get; set; }
}
