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
    public void EnsureMayApprove_ManagerUpToTheLimit_IsAllowed(double amount)
    {
        var exception = Record.Exception(() =>
            ApprovalPolicy.EnsureMayApprove(PendingInvoice((decimal)amount), "demo.manager", ApproverRoles.Manager));

        Assert.Null(exception);
    }

    [Theory]
    [InlineData(50_000.01)]
    [InlineData(100_000.0)]
    [InlineData(500_000.0)]
    public void EnsureMayApprove_ManagerAboveTheLimit_IsRefusedWithManagerReason(double amount)
    {
        var error = Assert.Throws<ApprovalNotPermittedException>(() =>
            ApprovalPolicy.EnsureMayApprove(PendingInvoice((decimal)amount), "demo.manager", ApproverRoles.Manager));

        Assert.Contains("A Manager can approve up to 50,000.00", error.Message);
    }

    [Theory]
    [InlineData(50_000.01)]
    [InlineData(250_000.0)]
    [InlineData(500_000.0)]
    public void EnsureMayApprove_DirectorUpToTheLimit_IsAllowed(double amount)
    {
        var exception = Record.Exception(() =>
            ApprovalPolicy.EnsureMayApprove(PendingInvoice((decimal)amount), "demo.director", ApproverRoles.Director));

        Assert.Null(exception);
    }

    [Fact]
    public void EnsureMayApprove_DirectorAboveTheLimit_IsRefusedWithDirectorReason()
    {
        var error = Assert.Throws<ApprovalNotPermittedException>(() =>
            ApprovalPolicy.EnsureMayApprove(PendingInvoice(500_000.01m), "demo.director", ApproverRoles.Director));

        Assert.Contains("Director limit of 500,000.00", error.Message);
    }

    [Theory]
    [InlineData("Admin")]
    [InlineData("Approver")]
    [InlineData("manager")]
    [InlineData("")]
    public void EnsureMayApprove_OtherRoles_AreRefusedEvenForSmallAmounts(string role)
    {
        var error = Assert.Throws<ApprovalNotPermittedException>(() =>
            ApprovalPolicy.EnsureMayApprove(PendingInvoice(1m), "someone", role));

        Assert.Contains("only a Manager or a Director can", error.Message);
    }
}
