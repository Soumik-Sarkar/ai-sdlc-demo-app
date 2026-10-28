using System.Globalization;

namespace InvoiceApp.Domain;

/// <summary>What a permitted approval does to the invoice.</summary>
public enum ApprovalOutcome
{
    /// <summary>The invoice becomes Approved.</summary>
    Final,

    /// <summary>The invoice needs a second approver; this one is recorded as the first.</summary>
    FirstOfTwo
}

/// <summary>
/// Pure approval rules: given an invoice, an approver and their role, either returns what the approval
/// would do or throws <see cref="ApprovalNotPermittedException"/> with the reason.
/// It never changes the invoice, so a refusal cannot leave a half-applied approval behind.
/// </summary>
public static class ApprovalPolicy
{
    public const decimal ManagerLimit = 50_000m;
    public const decimal DirectorLimit = 500_000m;

    public static ApprovalOutcome Evaluate(Invoice invoice, string approver, string role)
    {
        if (role != ApproverRoles.Manager && role != ApproverRoles.Director)
        {
            throw new ApprovalNotPermittedException(
                $"Role '{role}' cannot approve invoices; only a Manager or a Director can.");
        }

        if (invoice.Amount > DirectorLimit)
        {
            return EvaluateTwoApproverInvoice(invoice, approver, role);
        }

        if (role == ApproverRoles.Manager && invoice.Amount > ManagerLimit)
        {
            throw new ApprovalNotPermittedException(
                $"A Manager can approve up to {Format(ManagerLimit)}; invoice {invoice.Id} is {Format(invoice.Amount)}. A Director must approve it.");
        }

        return ApprovalOutcome.Final;
    }

    private static ApprovalOutcome EvaluateTwoApproverInvoice(Invoice invoice, string approver, string role)
    {
        if (invoice.FirstApprovedBy is null)
        {
            return ApprovalOutcome.FirstOfTwo;
        }

        if (string.Equals(invoice.FirstApprovedBy, approver, StringComparison.OrdinalIgnoreCase))
        {
            throw new ApprovalNotPermittedException(
                $"Invoice {invoice.Id} is above {Format(DirectorLimit)} and needs two different approvers; {invoice.FirstApprovedBy} already gave the first approval.");
        }

        if (role != ApproverRoles.Director)
        {
            throw new ApprovalNotPermittedException(
                $"Invoice {invoice.Id} is above {Format(DirectorLimit)}; the second approver must be a Director.");
        }

        return ApprovalOutcome.Final;
    }

    private static string Format(decimal amount) => amount.ToString("N2", CultureInfo.InvariantCulture);
}
