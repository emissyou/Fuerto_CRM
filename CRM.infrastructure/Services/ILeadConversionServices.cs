using CRM.domain.DTOs;
using CRM.domain.Entities;

namespace CRM.infrastructure.Services;

public interface ILeadConversionService
{
    Task<(Customer customer, Project project)> ConvertAsync(
        int companyId,
        int leadId,
        string actingUserId,
        ConvertLeadRequest request);
}