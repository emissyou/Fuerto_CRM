namespace CRM.domain.DTOs;

public class RecordPaymentRequest
{
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string? Reference { get; set; }
    public string? Notes { get; set; }
}