FINDING-1
severity: major
rule: Two approvers above 500,000 / One user cannot be both approvers (atomic approval, double-submit)
location: backend/src/InvoiceApp.Domain/ApprovalService.cs:31-48
problem: Approve does read, evaluate and mutate with no lock, and the repository returns the live shared Invoice object, so concurrent requests can race.
evidence: Invoice at 750,000, no first approver. demo.director and demo.director2 approve at the same moment; both see FirstApprovedBy == null and both get FirstOfTwo. The last writer overwrites the first approver and the invoice stays pending although two Directors approved.
suggestion: Serialise Approve and Reject with a lock, or use compare-and-set in the repository and return copies.
verdict: fixed

FINDING-2
severity: minor
rule: Roles that cannot approve / Refused approvals return a clear reason
location: backend/src/InvoiceApp.Api/Auth/RequireRoleAttribute.cs:21-24
problem: An Admin never reaches ApprovalPolicy; the controller attribute returns a bare 403 with no body, so no reason is shown.
evidence: Admin POSTs /api/approvals/INV-1001/approve -> 403 with an empty body.
suggestion: Give RequireRole a 403 { error } body.
verdict: fixed

FINDING-3
severity: minor
rule: AGENTS.md Layering (rules live in Domain only)
location: backend/src/InvoiceApp.Domain/ApprovalService.cs:50-64
problem: Reject takes no role, so "only Manager or Director may reject" is enforced only by the controller attribute.
evidence: A direct caller of ApprovalService.Reject(id, "demo.admin", "x") can reject a pending invoice; no unit test covers it.
suggestion: Pass the role into Reject and check it in ApprovalPolicy, with tests for Admin and unknown roles.
verdict: deferred: the specified rules cover approving only (rejecting by role is an assumption in proposal.md), and every role gate in this app is a [RequireRole] attribute on the controller. Moving the Reject gate into Domain changes the Reject signature and is a separate change; tracked as a follow-up.

FINDING-4
severity: minor
rule: Tests must be able to fail (nothing changed after refusal)
location: backend/tests/InvoiceApp.Tests/ApprovalServiceTests.cs:379-389
problem: Only one refusal case checks that Update was never called; the others check only in-memory fields.
evidence: A change that saves before throwing would pass the refusal theories because they inspect only the snapshot.
suggestion: Assert Assert.Empty(repository.Updated) in every refusal test.
verdict: fixed

FINDING-5
severity: minor
rule: Two approvers above 500,000 (first approval records the first approver)
location: backend/tests/InvoiceApp.Tests/ApprovalServiceTests.cs:272-298
problem: Timestamp assertions are only NotNull, and FirstApprovedAt preservation on second approval is never checked.
evidence: A bug that clears or overwrites FirstApprovedAt on the second approval stays green.
suggestion: Assert FirstApprovedAt equals the seeded value and that timestamps are recent.
verdict: fixed

FINDING-6
severity: minor
rule: Review expectations (tests must be able to fail)
location: e2e/tests/approval-limits.spec.ts:1-26
problem: Only one e2e test exists and it covers the Manager over-limit refusal, not the two-approver flow or the new "First approval by" line.
evidence: Nothing end to end checks the first-approval text, a same-user refusal, or a second Director completing the invoice.
suggestion: Add a second e2e: director first approval, same director refused, director2 approves.
verdict: deferred: the assignment asks for exactly one e2e test (rejected over-limit approval). The two-approver flow is covered by unit tests and was smoke-tested over the real API; a second e2e is the next addition if the UI line grows.

FINDING-7
severity: minor
rule: Review expectations (mutation tests)
location: backend/tests/InvoiceApp.Tests/stryker-config.json:3-4
problem: Config excludes DiscountCalculator.cs and sets break to 0, so the mutation gate can never fail.
evidence: A 0% score still exits successfully.
suggestion: Set break to at least 70 and drop the unrelated exclusion.
verdict: dismissed: the exclusion is deliberate so the before and after scores measure the approval code and not an untested, unrelated class; break stays 0 because the baseline run on the original code must finish and print its score. Scores are reported in ASSIGNMENT.md rather than gated in CI.

FINDING-8
severity: nit
rule: Tests must check boundaries precisely
location: backend/tests/InvoiceApp.Tests/ApprovalPolicyTests.cs:12-38
problem: Amounts are passed as double and cast to decimal, so boundary inputs are not exact decimal literals.
evidence: (decimal)50_000.01 goes through binary floating point.
suggestion: Use MemberData or decimal constants.
verdict: dismissed: C# attribute arguments cannot be decimal, and the double to decimal cast rounds to 15 significant digits so these values convert exactly. The 50,000.01 and 500,000.01 cases sit on the boundary and fail if the conversion were off; mutation of the limits was caught.

FINDING-9
severity: nit
rule: Demo accounts documentation (AGENTS.md)
location: README.md:15
problem: README still lists the removed demo.approver account.
evidence: Signing in with the README credentials fails.
suggestion: Update the README account table.
verdict: fixed

SUMMARY findings=9 blockers=0
