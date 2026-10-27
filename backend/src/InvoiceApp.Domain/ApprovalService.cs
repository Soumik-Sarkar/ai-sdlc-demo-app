namespace InvoiceApp.Domain;

/// <summary>
/// Moves an invoice through its lifecycle: Draft -> PendingApproval -> Approved or Rejected.
/// One invoice at a time. Who may approve, and up to what amount, is decided by <see cref="ApprovalPolicy"/>.
/// </summary>
public class ApprovalService
{
    // Approve and Reject read, check and then change an invoice. Without this lock two Directors approving at the
    // same moment could both be treated as the first approver. One lock is enough for the in-memory demo store.
    private static readonly object DecisionLock = new();

    private readonly IInvoiceRepository _repository;

    public ApprovalService(IInvoiceRepository repository)
    {
        _repository = repository;
    }

    public Invoice Submit(string invoiceId)
    {
        var invoice = Find(invoiceId);

        if (invoice.Status != InvoiceStatus.Draft)
        {
            throw new InvalidOperationException(
                $"Invoice {invoiceId} is {invoice.Status}; only drafts can be submitted.");
        }

        invoice.Status = InvoiceStatus.PendingApproval;
        _repository.Update(invoice);
        return invoice;
    }

    public Invoice Approve(string invoiceId, string approver, string role)
    {
        lock (DecisionLock)
        {
            var invoice = Find(invoiceId);
            EnsurePending(invoice);
            var outcome = ApprovalPolicy.Evaluate(invoice, approver, role);

            if (outcome == ApprovalOutcome.FirstOfTwo)
            {
                invoice.FirstApprovedBy = approver;
                invoice.FirstApprovedAt = DateTime.UtcNow;
                _repository.Update(invoice);
                return invoice;
            }

            invoice.Status = InvoiceStatus.Approved;
            invoice.DecidedBy = approver;
            invoice.DecidedAt = DateTime.UtcNow;
            _repository.Update(invoice);
            return invoice;
        }
    }

    public Invoice Reject(string invoiceId, string approver, string reason)
    {
        lock (DecisionLock)
        {
            if (string.IsNullOrWhiteSpace(reason))
            {
                throw new ArgumentException("A reason is required to reject an invoice.", nameof(reason));
            }

            var invoice = Find(invoiceId);
            EnsurePending(invoice);

            invoice.Status = InvoiceStatus.Rejected;
            invoice.DecidedBy = approver;
            invoice.DecidedAt = DateTime.UtcNow;
            invoice.DecisionNote = reason;
            _repository.Update(invoice);
            return invoice;
        }
    }

    private Invoice Find(string invoiceId) =>
        _repository.GetById(invoiceId)
        ?? throw new KeyNotFoundException($"Invoice {invoiceId} was not found.");

    private static void EnsurePending(Invoice invoice)
    {
        if (invoice.Status != InvoiceStatus.PendingApproval)
        {
            throw new InvalidOperationException(
                $"Invoice {invoice.Id} is {invoice.Status}; only pending invoices can be decided.");
        }
    }
}
