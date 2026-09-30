namespace CRM.domain.DTOs;

public class AdminUpdateUserRequest
{
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = "Staff";
    public bool IsActive { get; set; } = true;
    public int? BranchId { get; set; }
    public string? NewPassword { get; set; }
}