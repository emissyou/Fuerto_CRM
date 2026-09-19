using CRM.domain.DTOs;
using CRM.domain.Entities;
using CRM.domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CRM.infrastructure.Services;

public class QuotationWorkflowService : IQuotationWorkflowService
{
    private readonly ITenantDbContextFactory _factory;

    public QuotationWorkflowService(ITenantDbContextFactory factory)
    {
        _factory = factory;
    }

    public async Task<Quotation> IssueAsync(
        int companyId, int quotationId, string actingUserId)
    {
        await using var db = await _factory.CreateAsync(companyId);

        var q = await db.Quotations
            .FirstOrDefaultAsync(x => x.QuotationId == quotationId && x.CompanyId == companyId);

        if (q == null) throw new InvalidOperationException("Quotation not found.");

        if (q.Status != QuotationStatus.Draft && q.Status != QuotationStatus.Issued)
            throw new InvalidOperationException(
                $"Cannot issue a quotation in status '{q.Status}'.");

        // Ensure 50% deposit is set
        if (q.DepositRequired <= 0)
            q.DepositRequired = decimal.Round(q.TotalAmount * 0.5m, 2);

        q.Status = QuotationStatus.Issued;
        q.IssuedAt = DateTime.UtcNow;
        q.IssuedByUserId = actingUserId;

        // Move project into QuotationIssued stage
        var project = await db.Projects
            .FirstOrDefaultAsync(p => p.ProjectId == q.ProjectId && p.CompanyId == companyId);

        if (project != null)
        {
            project.DesignStage = ProjectDesignStage.QuotationIssued;
            project.Status = "QuotationIssued";
        }

        db.Activities.Add(new Activity
        {
            CompanyId = companyId,
            ProjectId = q.ProjectId,
            CustomerId = q.CustomerId,
            ActivityType = "QuotationIssued",
            Subject = $"Quotation {q.QuotationNumber} issued",
            Description = $"Total: {q.TotalAmount:C}, Deposit required: {q.DepositRequired:C}",
            ActivityDate = DateTime.UtcNow,
            Status = "Completed"
        });

        await db.SaveChangesAsync();
        return q;
    }

    public async Task<Quotation> AcceptAsync(
        int companyId, int quotationId, string actingUserId)
    {
        await using var db = await _factory.CreateAsync(companyId);

        var q = await db.Quotations
            .FirstOrDefaultAsync(x => x.QuotationId == quotationId && x.CompanyId == companyId);

        if (q == null) throw new InvalidOperationException("Quotation not found.");

        if (q.Status != QuotationStatus.Issued)
            throw new InvalidOperationException(
                $"Only Issued quotations can be accepted. Current: {q.Status}.");

        q.Status = QuotationStatus.Accepted;
        q.AcceptedAt = DateTime.UtcNow;

        var project = await db.Projects
            .FirstOrDefaultAsync(p => p.ProjectId == q.ProjectId && p.CompanyId == companyId);

        if (project != null)
        {
            project.AcceptedQuotationId = q.QuotationId;
            project.DesignStage = ProjectDesignStage.AwaitingPayment;
            project.Status = "AwaitingPayment";
        }

        db.Activities.Add(new Activity
        {
            CompanyId = companyId,
            ProjectId = q.ProjectId,
            CustomerId = q.CustomerId,
            ActivityType = "QuotationAccepted",
            Subject = $"Quotation {q.QuotationNumber} accepted",
            Description = "Customer accepted the quotation. Awaiting 50% deposit.",
            ActivityDate = DateTime.UtcNow,
            Status = "Completed"
        });

        await db.SaveChangesAsync();
        return q;
    }

    public async Task<Quotation> RecordPaymentAsync(
        int companyId, int quotationId, string actingUserId, RecordPaymentRequest request)
    {
        if (request.Amount <= 0)
            throw new InvalidOperationException("Payment amount must be greater than zero.");

        await using var db = await _factory.CreateAsync(companyId);

        var q = await db.Quotations
            .FirstOrDefaultAsync(x => x.QuotationId == quotationId && x.CompanyId == companyId);

        if (q == null) throw new InvalidOperationException("Quotation not found.");

        if (q.Status != QuotationStatus.Accepted)
            throw new InvalidOperationException(
                "Payments can only be recorded on Accepted quotations.");

        if (q.DepositRequired <= 0)
            q.DepositRequired = decimal.Round(q.TotalAmount * 0.5m, 2);

        // Guard against overpayment
        var remaining = q.TotalAmount - q.AmountPaid;
        if (request.Amount > remaining)
            throw new InvalidOperationException(
                $"Payment exceeds remaining balance ({remaining:C}).");

        q.AmountPaid += request.Amount;
        q.PaymentMethod = request.PaymentMethod;
        q.PaymentReference = request.Reference ?? string.Empty;

        // Determine new payment status
        if (q.AmountPaid >= q.TotalAmount)
        {
            q.PaymentStatus = PaymentStatus.FullyPaid;
            q.FullyPaidDate = DateTime.UtcNow;
        }
        else if (q.AmountPaid >= q.DepositRequired)
        {
            q.PaymentStatus = PaymentStatus.DepositReceived;
            q.DepositPaidDate ??= DateTime.UtcNow;
        }
        else
        {
            q.PaymentStatus = PaymentStatus.PartiallyPaid;
        }

        // Update Project workflow
        var project = await db.Projects
            .FirstOrDefaultAsync(p => p.ProjectId == q.ProjectId && p.CompanyId == companyId);

        if (project != null && q.PaymentStatus == PaymentStatus.DepositReceived)
        {
            project.DesignStage = ProjectDesignStage.DepositReceived;
            project.Status = "DepositReceived";
        }
        else if (project != null && q.PaymentStatus == PaymentStatus.FullyPaid)
        {
            project.Status = "FullyPaid";
        }

        db.Activities.Add(new Activity
        {
            CompanyId = companyId,
            ProjectId = q.ProjectId,
            CustomerId = q.CustomerId,
            ActivityType = "PaymentReceived",
            Subject = $"Payment of {request.Amount:C} received",
            Description =
                $"Method: {q.PaymentMethod}, Ref: {q.PaymentReference}, " +
                $"Total paid: {q.AmountPaid:C}/{q.TotalAmount:C}, Status: {q.PaymentStatus}",
            ActivityDate = DateTime.UtcNow,
            Status = "Completed"
        });

        await db.SaveChangesAsync();
        return q;
    }
}