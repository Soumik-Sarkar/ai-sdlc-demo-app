using InvoiceApp.Domain;
using Xunit;

namespace InvoiceApp.Tests;

public class ApprovalServiceTests
{
    private static Invoice InvoiceWith(string id, InvoiceStatus status) =>
        new() { Id = id, CustomerName = "Test School", Amount = 100m, Status = status };

    private static ApprovalService ServiceFor(params Invoice[] invoices) =>
        new(new FakeInvoiceRepository(invoices));

    [Fact]
    public void Submit_MovesDraftToPendingApproval()
    {
        var invoice = InvoiceWith("INV-1", InvoiceStatus.Draft);

        var result = ServiceFor(invoice).Submit("INV-1");

        Assert.Equal(InvoiceStatus.PendingApproval, result.Status);
    }

    [Fact]
    public void Submit_RejectsInvoiceThatIsNotADraft()
    {
        var service = ServiceFor(InvoiceWith("INV-1", InvoiceStatus.Approved));

        Assert.Throws<InvalidOperationException>(() => service.Submit("INV-1"));
    }

    [Fact]
    public void Approve_SetsStatusApproverAndTimestamp()
    {
        var service = ServiceFor(InvoiceWith("INV-1", InvoiceStatus.PendingApproval));

        var result = service.Approve("INV-1", "demo.manager", ApproverRoles.Manager);

        Assert.Equal(InvoiceStatus.Approved, result.Status);
        Assert.Equal("demo.manager", result.DecidedBy);
        Assert.NotNull(result.DecidedAt);
    }

    [Fact]
    public void Approve_RejectsInvoiceThatIsStillADraft()
    {
        var service = ServiceFor(InvoiceWith("INV-1", InvoiceStatus.Draft));

        Assert.Throws<InvalidOperationException>(() => service.Approve("INV-1", "demo.manager", ApproverRoles.Manager));
    }

    [Fact]
    public void Approve_ThrowsWhenInvoiceDoesNotExist()
    {
        var service = ServiceFor();

        Assert.Throws<KeyNotFoundException>(() => service.Approve("INV-404", "demo.manager", ApproverRoles.Manager));
    }

    [Fact]
    public void Reject_RecordsReasonAndApprover()
    {
        var service = ServiceFor(InvoiceWith("INV-1", InvoiceStatus.PendingApproval));

        var result = service.Reject("INV-1", "demo.manager", "Duplicate invoice");

        Assert.Equal(InvoiceStatus.Rejected, result.Status);
        Assert.Equal("Duplicate invoice", result.DecisionNote);
    }

    [Fact]
    public void Reject_RequiresAReason()
    {
        var service = ServiceFor(InvoiceWith("INV-1", InvoiceStatus.PendingApproval));

        Assert.Throws<ArgumentException>(() => service.Reject("INV-1", "demo.manager", "  "));
    }

    private static void AssertIsRecent(DateTime? timestamp)
    {
        Assert.NotNull(timestamp);
        Assert.True(DateTime.UtcNow - timestamp!.Value < TimeSpan.FromMinutes(1));
        Assert.True(timestamp.Value <= DateTime.UtcNow);
    }

    private static Invoice PendingInvoice(decimal amount) =>
        new() { Id = "INV-1", CustomerName = "Test School", Amount = amount, Status = InvoiceStatus.PendingApproval };

    // Everything an approval could touch, so "nothing changed" is checked field by field.
    private static (InvoiceStatus Status, string? FirstApprovedBy, DateTime? FirstApprovedAt, string? DecidedBy, DateTime? DecidedAt, string? DecisionNote, decimal Amount) Snapshot(Invoice invoice) =>
        (invoice.Status, invoice.FirstApprovedBy, invoice.FirstApprovedAt, invoice.DecidedBy, invoice.DecidedAt, invoice.DecisionNote, invoice.Amount);

    [Fact]
    public void Approve_ManagerWithinLimit_ApprovesAndRecordsManager()
    {
        var service = ServiceFor(PendingInvoice(50_000m));

        var result = service.Approve("INV-1", "demo.manager", ApproverRoles.Manager);

        Assert.Equal(InvoiceStatus.Approved, result.Status);
        Assert.Equal("demo.manager", result.DecidedBy);
    }

    [Fact]
    public void Approve_DirectorAtLimit_ApprovesAndRecordsDirector()
    {
        var service = ServiceFor(PendingInvoice(500_000m));

        var result = service.Approve("INV-1", "demo.director", ApproverRoles.Director);

        Assert.Equal(InvoiceStatus.Approved, result.Status);
        Assert.Equal("demo.director", result.DecidedBy);
    }

    [Theory]
    [InlineData(ApproverRoles.Manager, 50_000.01)]
    [InlineData(ApproverRoles.Manager, 500_000.0)]
    [InlineData("Admin", 100.0)]
    [InlineData("Approver", 100.0)]
    [InlineData("", 100.0)]
    public void Approve_WhenRefused_LeavesInvoiceCompletelyUnchanged(string role, double amount)
    {
        var invoice = PendingInvoice((decimal)amount);
        var before = Snapshot(invoice);
        var repository = new FakeInvoiceRepository(invoice);

        Assert.Throws<ApprovalNotPermittedException>(() => new ApprovalService(repository).Approve("INV-1", "someone", role));

        Assert.Equal(before, Snapshot(invoice));
        Assert.Empty(repository.Updated);
        Assert.Null(invoice.DecidedBy);
        Assert.Null(invoice.DecidedAt);
    }

    [Fact]
    public void Approve_WhenRefused_GivesAReasonThatNamesTheLimit()
    {
        var service = ServiceFor(PendingInvoice(60_000m));

        var error = Assert.Throws<ApprovalNotPermittedException>(
            () => service.Approve("INV-1", "demo.manager", ApproverRoles.Manager));

        Assert.Contains("50,000.00", error.Message);
        Assert.Contains("Director", error.Message);
    }

    [Fact]
    public void Reject_IsNotLimitedByAmount()
    {
        var service = ServiceFor(PendingInvoice(900_000m));

        var result = service.Reject("INV-1", "demo.manager", "Duplicate invoice");

        Assert.Equal(InvoiceStatus.Rejected, result.Status);
    }

    private static Invoice PendingInvoiceWithFirstApproval(decimal amount, string firstApprover) =>
        new()
        {
            Id = "INV-1", CustomerName = "Test School", Amount = amount, Status = InvoiceStatus.PendingApproval,
            FirstApprovedBy = firstApprover, FirstApprovedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        };

    [Theory]
    [InlineData(ApproverRoles.Manager)]
    [InlineData(ApproverRoles.Director)]
    public void Approve_AboveDirectorLimit_FirstApprovalKeepsInvoicePendingAndRecordsApprover(string role)
    {
        var invoice = PendingInvoice(500_000.01m);
        var service = ServiceFor(invoice);

        var result = service.Approve("INV-1", "demo.first", role);

        Assert.Equal(InvoiceStatus.PendingApproval, result.Status);
        Assert.Equal("demo.first", result.FirstApprovedBy);
        AssertIsRecent(result.FirstApprovedAt);
        Assert.Null(result.DecidedBy);
        Assert.Null(result.DecidedAt);
    }

    [Fact]
    public void Approve_AboveDirectorLimit_SecondDifferentDirectorApprovesAndKeepsBothApprovers()
    {
        var invoice = PendingInvoiceWithFirstApproval(750_000m, "demo.manager");
        var service = ServiceFor(invoice);

        var result = service.Approve("INV-1", "demo.director", ApproverRoles.Director);

        Assert.Equal(InvoiceStatus.Approved, result.Status);
        Assert.Equal("demo.manager", result.FirstApprovedBy);
        Assert.Equal(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), result.FirstApprovedAt);
        Assert.Equal("demo.director", result.DecidedBy);
        AssertIsRecent(result.DecidedAt);
    }

    [Fact]
    public void Approve_AboveDirectorLimit_TwoDirectorsCanApproveTogether()
    {
        var service = ServiceFor(PendingInvoice(750_000m));

        service.Approve("INV-1", "demo.director", ApproverRoles.Director);
        var result = service.Approve("INV-1", "demo.director2", ApproverRoles.Director);

        Assert.Equal(InvoiceStatus.Approved, result.Status);
        Assert.Equal("demo.director", result.FirstApprovedBy);
        Assert.Equal("demo.director2", result.DecidedBy);
    }

    [Theory]
    [InlineData("demo.director", ApproverRoles.Director)]
    [InlineData("Demo.Director", ApproverRoles.Director)]
    [InlineData("demo.director", ApproverRoles.Manager)]
    public void Approve_AboveDirectorLimit_SameUserAgain_IsRefusedAndNothingChanges(string approver, string role)
    {
        var invoice = PendingInvoiceWithFirstApproval(750_000m, "demo.director");
        var before = Snapshot(invoice);
        var repository = new FakeInvoiceRepository(invoice);

        var error = Assert.Throws<ApprovalNotPermittedException>(
            () => new ApprovalService(repository).Approve("INV-1", approver, role));

        Assert.Contains("two different approvers", error.Message);
        Assert.Equal(before, Snapshot(invoice));
        Assert.Empty(repository.Updated);
        Assert.Equal(InvoiceStatus.PendingApproval, invoice.Status);
    }

    [Fact]
    public void Approve_AboveDirectorLimit_ManagerAsSecondApprover_IsRefusedAndNothingChanges()
    {
        var invoice = PendingInvoiceWithFirstApproval(750_000m, "demo.director");
        var before = Snapshot(invoice);
        var repository = new FakeInvoiceRepository(invoice);

        var error = Assert.Throws<ApprovalNotPermittedException>(
            () => new ApprovalService(repository).Approve("INV-1", "demo.manager", ApproverRoles.Manager));

        Assert.Contains("second approver must be a Director", error.Message);
        Assert.Equal(before, Snapshot(invoice));
        Assert.Empty(repository.Updated);
    }

    [Fact]
    public void Approve_AtExactlyTheDirectorLimit_DirectorApprovesAloneWithNoFirstApproval()
    {
        var invoice = PendingInvoice(500_000m);
        var service = ServiceFor(invoice);

        var result = service.Approve("INV-1", "demo.director", ApproverRoles.Director);

        Assert.Equal(InvoiceStatus.Approved, result.Status);
        Assert.Null(result.FirstApprovedBy);
    }

    [Fact]
    public void Reject_AfterAFirstApproval_RejectsTheInvoice()
    {
        var invoice = PendingInvoiceWithFirstApproval(750_000m, "demo.manager");
        var service = ServiceFor(invoice);

        var result = service.Reject("INV-1", "demo.director", "Wrong customer");

        Assert.Equal(InvoiceStatus.Rejected, result.Status);
        Assert.Equal("Wrong customer", result.DecisionNote);
    }

    [Fact]
    public void Approve_AboveDirectorLimit_FirstApprovalIsSaved()
    {
        var invoice = PendingInvoice(750_000m);
        var repository = new FakeInvoiceRepository(invoice);

        new ApprovalService(repository).Approve("INV-1", "demo.manager", ApproverRoles.Manager);

        Assert.Same(invoice, Assert.Single(repository.Updated));
    }

    [Fact]
    public void Approve_WhenRefused_SavesNothing()
    {
        var invoice = PendingInvoiceWithFirstApproval(750_000m, "demo.director");
        var repository = new FakeInvoiceRepository(invoice);

        Assert.Throws<ApprovalNotPermittedException>(
            () => new ApprovalService(repository).Approve("INV-1", "demo.director", ApproverRoles.Director));

        Assert.Empty(repository.Updated);
    }
}
