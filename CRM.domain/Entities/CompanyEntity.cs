namespace CRM.domain.Entities;

public abstract class CompanyEntity
{
    public int CompanyId { get; set; }

    public Company? Company { get; set; }
}