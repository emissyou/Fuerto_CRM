using CRM.domain.DTOs;
using CRM.domain.Entities;

namespace CRM.infrastructure.Services;

public interface IQuotationWorkflowService
{
    Task<Quotation> IssueAsync(int companyId, int quotationId, string actingUserId);
    Task<Quotation> AcceptAsync(int companyId, int quotationId, string actingUserId);
    Task<Quotation> RecordPaymentAsync(
        int companyId, int quotationId, string actingUserId, RecordPaymentRequest request);
}