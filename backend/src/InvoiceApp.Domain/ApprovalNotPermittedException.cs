namespace InvoiceApp.Domain;

/// <summary>An approval was refused by the approval rules. The message is the reason shown to the user.</summary>
public class ApprovalNotPermittedException : InvalidOperationException
{
    public ApprovalNotPermittedException(string reason) : base(reason)
    {
    }
}
