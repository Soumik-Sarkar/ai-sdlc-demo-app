---
name: approval-rules
description: The invoice approval limits and two-approver rule, with boundary values and the traps agents fall into. Use when implementing, testing or reviewing approval logic.
---
# Approval rules

- Manager: up to and including 50,000. Director: up to and including 500,000.
- Above 500,000: two distinct users, second must be a Director, first may be Manager or Director.
- Same user never counts twice (case-insensitive username).
- Admin / unknown roles cannot approve. Rejecting is not amount-limited.
- Refusal: throw `ApprovalNotPermittedException(reason)`; invoice must be byte-for-byte unchanged.

## Traps (check each one)
1. `<` instead of `<=` at 50,000 and 500,000.
2. Recording the first approver and *then* discovering the second check fails (partial mutation).
3. Comparing the second approver only to `DecidedBy` (which is null until final approval) instead of `FirstApprovedBy`.
4. Case-sensitive user comparison.
5. A Manager accepted as second approver because the amount check alone passes.
6. A Director approving a >500,000 invoice alone and it going straight to Approved.
7. Reason text that is generic ("not allowed") instead of naming the limit or the missing role.
