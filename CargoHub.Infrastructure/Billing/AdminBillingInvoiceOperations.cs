using CargoHub.Application.Auth;
using CargoHub.Application.Billing.Admin;
using CargoHub.Application.Billing.AdminInvoicing;
using CargoHub.Application.Couriers;
using CargoHub.Domain.Billing;
using CargoHub.Infrastructure.Identity;
using CargoHub.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CargoHub.Infrastructure.Billing;

public sealed class AdminBillingInvoiceOperations : IAdminBillingInvoiceOperations
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAdminBillingReader _billingReader;
    private readonly IEmailSender _emailSender;
    private readonly IBillingInvoicePdfGenerator _pdfGenerator;

    public AdminBillingInvoiceOperations(
        ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        IAdminBillingReader billingReader,
        IEmailSender emailSender,
        IBillingInvoicePdfGenerator pdfGenerator)
    {
        _db = db;
        _userManager = userManager;
        _billingReader = billingReader;
        _emailSender = emailSender;
        _pdfGenerator = pdfGenerator;
    }

    public async Task<SendInvoiceEmailResult> SendInvoiceEmailAsync(
        Guid periodId,
        string recipientAdminUserId,
        string sentBySuperAdminUserId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(recipientAdminUserId))
            return SendInvoiceEmailResult.Fail("RecipientRequired", "Recipient admin user id is required.");

        var model = await _billingReader.GetInvoicePdfModelAsync(periodId, cancellationToken);
        if (model == null)
            return SendInvoiceEmailResult.Fail("NotFound", "Billing period not found.");

        var company = await _db.Companies.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == model.CompanyId, cancellationToken);
        if (company == null)
            return SendInvoiceEmailResult.Fail("CompanyNotFound", "Company for this period was not found.");

        var recipient = await _userManager.FindByIdAsync(recipientAdminUserId);
        if (recipient == null)
            return SendInvoiceEmailResult.Fail("RecipientNotFound", "Recipient user not found.");

        if (!recipient.IsActive)
            return SendInvoiceEmailResult.Fail("RecipientInactive", "Recipient user is inactive.");

        var roles = await _userManager.GetRolesAsync(recipient);
        if (!roles.Contains(RoleNames.Admin))
            return SendInvoiceEmailResult.Fail("RecipientNotAdmin", "Recipient must be a company Admin.");

        if (string.IsNullOrWhiteSpace(company.BusinessId) || string.IsNullOrWhiteSpace(recipient.BusinessId))
            return SendInvoiceEmailResult.Fail(
                "BusinessIdMismatch",
                "Both the company and the recipient must have a business id set to send invoice email.");

        if (!string.Equals(
                recipient.BusinessId.Trim(),
                company.BusinessId.Trim(),
                StringComparison.OrdinalIgnoreCase))
            return SendInvoiceEmailResult.Fail("RecipientWrongCompany", "Recipient is not an admin for this company.");

        var email = recipient.Email?.Trim();
        if (string.IsNullOrEmpty(email))
            return SendInvoiceEmailResult.Fail("RecipientNoEmail", "Recipient has no email address.");

        var pdf = _pdfGenerator.GeneratePdf(model);
        var fileName = $"invoice-{model.YearUtc}-{model.MonthUtc:00}.pdf";
        var subject = $"Invoice {model.YearUtc}-{model.MonthUtc:00} — {model.CompanyName}";
        var html =
            $"<p>Hello,</p><p>Please find attached the billing summary for <strong>{System.Net.WebUtility.HtmlEncode(model.CompanyName)}</strong> " +
            $"for <strong>{model.YearUtc}-{model.MonthUtc:00}</strong> (UTC).</p>" +
            $"<p>Ledger total: <strong>{model.LedgerTotal:N2} {model.Currency}</strong><br/>" +
            $"Amount due (invoice): <strong>{model.PayableTotal:N2} {model.Currency}</strong></p>" +
            "<p>Regards</p>";

        await _emailSender.SendAsync(
            email,
            subject,
            html,
            new[]
            {
                new EmailAttachment
                {
                    FileName = fileName,
                    ContentType = "application/pdf",
                    Content = pdf
                }
            },
            cancellationToken);

        _db.SubscriptionInvoiceSends.Add(new SubscriptionInvoiceSend
        {
            Id = Guid.NewGuid(),
            CompanyBillingPeriodId = periodId,
            SentAtUtc = DateTime.UtcNow,
            SentBySuperAdminUserId = sentBySuperAdminUserId,
            RecipientAdminUserId = recipientAdminUserId,
            RecipientEmailSnapshot = email,
            LedgerTotalSnapshot = model.LedgerTotal,
            InvoiceTotalSnapshot = model.PayableTotal
        });
        await _db.SaveChangesAsync(cancellationToken);

        return SendInvoiceEmailResult.Ok();
    }

    public async Task<UpdateLineExcludedResult> UpdateLineExcludedAsync(
        Guid lineItemId,
        bool excludedFromInvoice,
        string superAdminUserId,
        CancellationToken cancellationToken = default)
    {
        var line = await _db.BillingLineItems.FirstOrDefaultAsync(l => l.Id == lineItemId, cancellationToken);
        if (line == null)
            return UpdateLineExcludedResult.Fail("NotFound", "Billing line item not found.");

        line.ExcludedFromInvoice = excludedFromInvoice;
        line.InvoiceExclusionUpdatedAtUtc = DateTime.UtcNow;
        line.InvoiceExclusionUpdatedByUserId = superAdminUserId;
        await _db.SaveChangesAsync(cancellationToken);
        return UpdateLineExcludedResult.Ok();
    }
}
