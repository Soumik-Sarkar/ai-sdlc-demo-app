## Approval limits by role (Manager 50,000 / Director 500,000 / two approvers above)

Spec: `openspec/changes/add-approval-limits-by-role/`. Write-up: `ASSIGNMENT.md`.

### Summary
- Manager approves up to 50,000, Director up to 500,000 (inclusive).
- Above 500,000: two different users, second must be a Director. The same user can never be both (case-insensitive).
- Refusals return a reason (403 `{ error }`) and change nothing; verified field by field and by "nothing saved".
- Demo role `Approver` replaced by `Manager`; `Director` accounts added.

### Tests
- 49 unit cases (policy + service), including both negative rules.
- 1 Playwright test: Manager over-limit approval is refused with the reason, invoice stays pending.

### Review findings (reviewer agent, `review-pr` format)

| # | Severity | Finding | Verdict |
| --- | --- | --- | --- |
| 1 | major | Approve is read-check-write with no lock; two Directors at once can both become "first approver" | **fixed**: lock in `ApprovalService` |
| 2 | minor | Admin gets a bare 403 with no reason | **fixed**: `RequireRole` returns `{ error }` |
| 3 | minor | Reject has no role check in Domain, only the controller attribute | **deferred**: spec covers approving only; every role gate here is a controller attribute; moving it changes `Reject`'s signature, separate change |
| 4 | minor | Most refusal tests did not assert nothing was saved | **fixed**: `Assert.Empty(repository.Updated)` in each |
| 5 | minor | Timestamp asserts only `NotNull`; `FirstApprovedAt` preservation unchecked | **fixed** |
| 6 | minor | Only one e2e; two-approver UI flow not covered end to end | **deferred**: assignment asks for one e2e; flow covered by unit tests and an API smoke test |
| 7 | minor | Stryker config excludes `DiscountCalculator.cs`, `break: 0` | **dismissed**: deliberate scoping so before/after scores measure approval code; baseline run must complete |
| 8 | nit | Boundary amounts passed as `double` then cast | **dismissed**: attributes cannot hold `decimal`; the cast is exact at 15 significant digits and boundary cases would fail otherwise |
| 9 | nit | README lists removed `demo.approver` | **fixed** |

Full text: `docs/review/review-findings.md`.
