namespace CRM.domain.Enums;

public static class IssueType
{
    public const string Complaint = "Complaint";
    public const string Adjustment = "Adjustment";
    public const string PaymentDispute = "PaymentDispute";
    public const string Rework = "Rework";
    public const string Other = "Other";
}

public static class IssueSeverity
{
    public const string Low = "Low";
    public const string Medium = "Medium";
    public const string High = "High";
    public const string Critical = "Critical";
}

public static class IssueStatus
{
    public const string Open = "Open";
    public const string InProgress = "InProgress";
    public const string Resolved = "Resolved";
    public const string Closed = "Closed";
    public const string Rejected = "Rejected";
}