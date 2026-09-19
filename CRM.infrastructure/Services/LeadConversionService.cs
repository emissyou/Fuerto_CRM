using CRM.domain.DTOs;
using CRM.domain.Entities;
using CRM.domain.Enums;
using CRM.infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CRM.infrastructure.Services;

public class LeadConversionService : ILeadConversionService
{
    private readonly ITenantDbContextFactory _factory;

    public LeadConversionService(ITenantDbContextFactory factory)
    {
        _factory = factory;
    }

    public async Task<(Customer, Project)> ConvertAsync(
        int companyId, int leadId, string actingUserId, ConvertLeadRequest request)
    {
        await using var db = await _factory.CreateAsync(companyId);

        var lead = await db.Leads
            .FirstOrDefaultAsync(l => l.LeadId == leadId && l.CompanyId == companyId);

        if (lead == null)
            throw new InvalidOperationException("Lead not found.");

        if (lead.Status == "Converted")
            throw new InvalidOperationException("Lead has already been converted.");

        // 1) Customer
        var customer = new Customer
        {
            CompanyId = companyId,
            FirstName = lead.FirstName,
            LastName = lead.LastName,
            Email = lead.Email,
            Phone = lead.Phone,
            CustomerType = string.IsNullOrWhiteSpace(request.CustomerType)
                ? "Regular" : request.CustomerType!.Trim(),
            Notes = lead.Notes,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            ConvertedFromLeadId = lead.LeadId
        };
        db.Customers.Add(customer);
        await db.SaveChangesAsync();

        // 2) Project
        var project = new Project
        {
            CompanyId = companyId,
            ProjectCode = await GenerateProjectCodeAsync(db, companyId),
            ProjectName = string.IsNullOrWhiteSpace(request.ProjectName)
                ? $"{lead.FirstName} {lead.LastName} - Project"
                : request.ProjectName!.Trim(),
            CustomerId = customer.CustomerId,
            ProjectType = request.ProjectType?.Trim() ?? lead.ServiceInterest,
            Location = request.Location?.Trim() ?? string.Empty,
            Description = request.Description?.Trim() ?? lead.Notes,
            Status = "Planning",
            DesignStage = ProjectDesignStage.Inquiry,
            ProgressPercentage = 0,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        db.Projects.Add(project);
        await db.SaveChangesAsync();

        // 3) Lead converted
        lead.Status = "Converted";
        lead.ConvertedToCustomerId = customer.CustomerId;
        lead.ConvertedToProjectId = project.ProjectId;
        lead.ConvertedAt = DateTime.UtcNow;
        lead.ConvertedByUserId = actingUserId;
        await db.SaveChangesAsync();

        // 4) Activity
        db.Activities.Add(new Activity
        {
            CompanyId = companyId,
            LeadId = lead.LeadId,
            CustomerId = customer.CustomerId,
            ProjectId = project.ProjectId,
            ActivityType = "LeadConverted",
            Subject = "Lead converted to Customer & Project",
            Description = $"Lead '{lead.FirstName} {lead.LastName}' converted.",
            ActivityDate = DateTime.UtcNow,
            Status = "Completed"
        });
        await db.SaveChangesAsync();

        return (customer, project);
    }

    // ⬇️ FIXED: parameter type is TenantErpDbContext, not TenantDbContext
    private static async Task<string> GenerateProjectCodeAsync(
        TenantErpDbContext db, int companyId)
    {
        var count = await db.Projects.CountAsync(p => p.CompanyId == companyId);
        return $"PRJ-{DateTime.UtcNow:yyyyMM}-{(count + 1):D4}";
    }
}