namespace CRM.domain.Entities;

public class CompanySubscription
{
    public int SubscriptionId { get; set; }

    public int CompanyId { get; set; }

    public Company? Company { get; set; }

    public string PlanName { get; set; } = "Professional";

    public string Status { get; set; } = "Active";

    public decimal MonthlyFee { get; set; } = 0m;

    public DateTime StartDate { get; set; } = DateTime.UtcNow;

    public DateTime? EndDate { get; set; }

    public string AvailedModules { get; set; } = "All";

    public List<string> GetModuleList()
    {
        if (string.IsNullOrWhiteSpace(AvailedModules)) return new List<string>();
        if (AvailedModules.Equals("All", StringComparison.OrdinalIgnoreCase))
        {
            return new List<string>
            {
                "Main Transaction",
                "Data Collection",
                "Business Intelligence",
                "Action",
                "Team & Branching"
            };
        }
        return AvailedModules
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();
    }
}
