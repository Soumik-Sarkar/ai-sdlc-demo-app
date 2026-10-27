using InvoiceApp.Domain;
using Xunit;

namespace InvoiceApp.Tests;

public class ApprovalPolicyTests
{
    private static Invoice PendingInvoice(decimal amount) =>
        new() { Id = "INV-1", CustomerName = "Test School", Amount = amount, Status = InvoiceStatus.PendingApproval };

    [Theory]
    [InlineData(0.01)]
    [InlineData(49_999.99)]
    [InlineData(50_000.0)]
    public void Evaluate_ManagerUpToTheLimit_IsAllowed(double amount)
    {
        var outcome = ApprovalPolicy.Evaluate(PendingInvoice((decimal)amount), "demo.manager", ApproverRoles.Manager);

        Assert.Equal(ApprovalOutcome.Final, outcome);
    }

    [Theory]
    [InlineData(50_000.01)]
    [InlineData(100_000.0)]
    [InlineData(500_000.0)]
    public void Evaluate_ManagerAboveTheLimit_IsRefusedWithManagerReason(double amount)
    {
        var error = Assert.Throws<ApprovalNotPermittedException>(() =>
            ApprovalPolicy.Evaluate(PendingInvoice((decimal)amount), "demo.manager", ApproverRoles.Manager));

        Assert.Contains("A Manager can approve up to 50,000.00", error.Message);
    }

    [Theory]
    [InlineData(50_000.01)]
    [InlineData(250_000.0)]
    [InlineData(500_000.0)]
    public void Evaluate_DirectorUpToTheLimit_IsAllowed(double amount)
    {
        var outcome = ApprovalPolicy.Evaluate(PendingInvoice((decimal)amount), "demo.director", ApproverRoles.Director);

        Assert.Equal(ApprovalOutcome.Final, outcome);
    }

    [Theory]
    [InlineData(ApproverRoles.Manager)]
    [InlineData(ApproverRoles.Director)]
    public void Evaluate_AboveDirectorLimitWithNoFirstApproval_RecordsAFirstApprovalOnly(string role)
    {
        var outcome = ApprovalPolicy.Evaluate(PendingInvoice(500_000.01m), "demo.first", role);

        Assert.Equal(ApprovalOutcome.FirstOfTwo, outcome);
    }

    [Fact]
    public void Evaluate_AboveDirectorLimit_SecondApproverWhoIsADifferentDirector_IsFinal()
    {
        var invoice = PendingInvoice(900_000m);
        invoice.FirstApprovedBy = "demo.manager";

        var outcome = ApprovalPolicy.Evaluate(invoice, "demo.director", ApproverRoles.Director);

        Assert.Equal(ApprovalOutcome.Final, outcome);
    }

    [Fact]
    public void Evaluate_AboveDirectorLimit_SecondApproverWhoIsAManager_IsRefused()
    {
        var invoice = PendingInvoice(900_000m);
        invoice.FirstApprovedBy = "demo.director";

        var error = Assert.Throws<ApprovalNotPermittedException>(() =>
            ApprovalPolicy.Evaluate(invoice, "demo.manager", ApproverRoles.Manager));

        Assert.Contains("second approver must be a Director", error.Message);
    }

    [Theory]
    [InlineData(ApproverRoles.Director, "demo.director", "demo.director")]
    [InlineData(ApproverRoles.Director, "DEMO.DIRECTOR", "demo.director")]
    [InlineData(ApproverRoles.Manager, "demo.manager", "demo.manager")]
    public void Evaluate_AboveDirectorLimit_SameUserCannotBeBothApprovers(string role, string approver, string firstApprover)
    {
        var invoice = PendingInvoice(900_000m);
        invoice.FirstApprovedBy = firstApprover;

        var error = Assert.Throws<ApprovalNotPermittedException>(() =>
            ApprovalPolicy.Evaluate(invoice, approver, role));

        Assert.Contains("two different approvers", error.Message);
    }

    [Fact]
    public void Evaluate_ExactlyAtDirectorLimit_NeedsNoSecondApprover()
    {
        var outcome = ApprovalPolicy.Evaluate(PendingInvoice(500_000m), "demo.director", ApproverRoles.Director);

        Assert.Equal(ApprovalOutcome.Final, outcome);
    }

    [Theory]
    [InlineData("Admin")]
    [InlineData("Approver")]
    [InlineData("manager")]
    [InlineData("")]
    public void Evaluate_OtherRoles_AreRefusedEvenForSmallAmounts(string role)
    {
        var error = Assert.Throws<ApprovalNotPermittedException>(() =>
            ApprovalPolicy.Evaluate(PendingInvoice(1m), "someone", role));

        Assert.Contains("only a Manager or a Director can", error.Message);
    }
}
