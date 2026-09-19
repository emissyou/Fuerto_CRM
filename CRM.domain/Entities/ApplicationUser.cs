using Microsoft.AspNetCore.Identity;

namespace CRM.domain.Entities;

public class ApplicationUser : IdentityUser
{
    public int? CompanyId { get; set; }
    public Company? Company { get; set; }

    public string FullName { get; set; } = string.Empty;
}