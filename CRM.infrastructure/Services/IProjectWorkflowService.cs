using CRM.domain.DTOs;
using CRM.domain.Entities;

namespace CRM.infrastructure.Services;

public interface IProjectWorkflowService
{
    Task<Project> AssignDesignerAsync(
        int companyId, int projectId, string actingUserId,
        AssignDesignerRequest request, string designerFullName);

    Task<Project> UpdateProgressAsync(
        int companyId, int projectId, string actingUserId,
        UpdateProgressRequest request);
}