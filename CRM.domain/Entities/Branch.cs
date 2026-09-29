namespace CRM.domain.Entities;

public class Branch
{
    public int BranchId { get; set; }

    public int CompanyId { get; set; }

    public string BranchCode { get; set; } = string.Empty;

    public string BranchName { get; set; } = string.Empty;

    public string Address { get; set; } = string.Empty;

    public string ContactNumber { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public bool IsMainBranch { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
