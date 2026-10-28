## Context

`ApprovalService` in the Domain project owns the invoice lifecycle. Controllers must stay thin (see AGENTS.md). Today `Approve(invoiceId, approver)` has no notion of role or amount.

## Goals / Non-Goals

**Goals:** enforce the limits and two-approver rule in Domain; refused approvals are atomic (no partial mutation); API maps refusals to a clear HTTP response.
**Non-Goals:** configurable limits per customer, audit history beyond first/final approver, notifications, bulk approval (UC-1), partial approval (UC-2).

## Decisions

1. **Pure rule class `ApprovalPolicy` in Domain.** It takes (invoice, approver, role) and returns allow/deny with a reason. `ApprovalService` calls it, then mutates. All checks run before any mutation, which gives "nothing changes" for free and makes the rule easy to unit-test and mutation-test.
2. **Limits are constants** (`ManagerLimit = 50_000m`, `DirectorLimit = 500_000m`), compared with `<=` so the boundary is inclusive.
3. **First approval is stored on `Invoice`** (`FirstApprovedBy`, `FirstApprovedAt`); `Status` stays `PendingApproval` until the final approval. Final approver goes in the existing `DecidedBy`.
4. **New `ApprovalNotPermittedException : InvalidOperationException`** carrying the reason. `ApiControllerBase.Handle` catches it before the generic `InvalidOperationException` and returns 403 with `{ error }`. This keeps existing 404/409/400 mapping unchanged.
5. **Role comes from the session** and is passed into the service by the controller; the controller does no rule checks. `[RequireRole]` on the controller is widened to Manager, Director so Admin and others get 403 before reaching the service; the policy still refuses them if called directly (defence in depth).
6. **Same-user check** uses `string.Equals(..., OrdinalIgnoreCase)`.

## Risks / Trade-offs

- Replacing the `Approver` role changes demo credentials and docs. Mitigation: update AGENTS.md, README and the demo users together.
- Two-approver state lives on the invoice, so an invoice cannot be "un-approved" by a first approver. Acceptable for this scope; the invoice can still be rejected.
