namespace CRM.domain.Enums;

public static class PaymentStatus
{
    public const string Pending = "Pending";
    public const string PartiallyPaid = "PartiallyPaid";
    public const string DepositReceived = "DepositReceived";
    public const string FullyPaid = "FullyPaid";
    public const string Refunded = "Refunded";
}