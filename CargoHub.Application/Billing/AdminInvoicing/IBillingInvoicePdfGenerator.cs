using CargoHub.Application.Billing.Admin;

namespace CargoHub.Application.Billing.AdminInvoicing;

/// <summary>Produces PDF bytes for a billing period invoice (API QuestPDF implementation).</summary>
public interface IBillingInvoicePdfGenerator
{
    byte[] GeneratePdf(BillingInvoicePdfModel model);
}
