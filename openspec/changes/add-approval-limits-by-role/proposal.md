## Why

Today any Approver can approve an invoice of any size on their own. Finance wants spending authority to follow seniority: managers handle routine invoices, directors handle large ones, and very large invoices get a second pair of eyes.

## What Changes

- Introduce two approving roles, **Manager** and **Director**, each with a maximum single-approver amount (50,000 and 500,000).
- Invoices above 500,000 require two distinct approvers; the second must be a Director.
- The same user can never count as both approvers on one invoice.
- A refused approval returns a clear reason and leaves the invoice untouched.
- The legacy `Approver` demo role is replaced by `Manager`; new demo accounts are added for Manager and Director.
- **BREAKING**: `ApprovalService.Approve` now needs the approver's role as well as their username.

## Capabilities

### New Capabilities
- `invoice-approval`: who may approve an invoice, up to what amount, and how many approvals an invoice needs.

### Modified Capabilities
<!-- none: no specs exist yet in openspec/specs -->

## Impact

- `InvoiceApp.Domain`: `ApprovalService`, `Invoice` (records the first approval), new approval-limit rule and exception type.
- `InvoiceApp.Api`: `ApprovalsController`, `ApiControllerBase` (403 for refused approvals), demo users, Approvals tab in the web UI (show first approver / awaiting second approval).
- Tests: new unit tests for each rule; one Playwright test for a rejected over-limit approval.

## Assumptions (recorded instead of asking)

- Limits are inclusive: exactly 50,000 is Manager-approvable, exactly 500,000 is Director-approvable.
- Either role may give the **first** approval on an invoice above 500,000; only a Director may give the second.
- "Same user" is compared by username, case-insensitively.
- Rejecting an invoice is not limited by amount; any Manager or Director may reject.
- Roles other than Manager and Director (including Admin) cannot approve.
