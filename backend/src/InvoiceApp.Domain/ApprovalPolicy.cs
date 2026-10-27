using System.Globalization;

namespace InvoiceApp.Domain;

/// <summary>
/// Pure approval rules: given an invoice, an approver and their role, either returns normally
/// (the approval may go ahead) or throws <see cref="ApprovalNotPermittedException"/> with the reason.
/// It never changes the invoice.
/// </summary>
public static class ApprovalPolicy
{
    public const decimal ManagerLimit = 50_000m;
    public const decimal DirectorLimit = 500_000m;

    public static void EnsureMayApprove(Invoice invoice, string approver, string role)
    {
        if (role != ApproverRoles.Manager && role != ApproverRoles.Director)
        {
            throw new ApprovalNotPermittedException(
                $"Role '{role}' cannot approve invoices; only a Manager or a Director can.");
        }

        if (invoice.Amount > DirectorLimit)
        {
            throw new ApprovalNotPermittedException(
                $"Invoice {invoice.Id} is {Format(invoice.Amount)}, above the Director limit of {Format(DirectorLimit)}.");
        }

        if (role == ApproverRoles.Manager && invoice.Amount > ManagerLimit)
        {
            throw new ApprovalNotPermittedException(
                $"A Manager can approve up to {Format(ManagerLimit)}; invoice {invoice.Id} is {Format(invoice.Amount)}. A Director must approve it.");
        }
    }

    private static string Format(decimal amount) => amount.ToString("N2", CultureInfo.InvariantCulture);
}
