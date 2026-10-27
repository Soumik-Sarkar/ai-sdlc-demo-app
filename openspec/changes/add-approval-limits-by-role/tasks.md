## 1. Step 1 – single-approver limits

- [ ] 1.1 Add roles Manager/Director to demo users (Manager, two Directors) and widen controller role check
- [ ] 1.2 Add `ApprovalNotPermittedException` and map it to 403 in `ApiControllerBase`
- [ ] 1.3 Add `ApprovalPolicy` with Manager and Director limits (inclusive boundaries)
- [ ] 1.4 Pass the approver's role from the controller into `ApprovalService.Approve`
- [ ] 1.5 Unit tests: boundaries (50,000 / 50,000.01 / 500,000), unknown role refused, refusal leaves invoice unchanged
- [ ] 1.6 Update existing tests for the new `Approve` signature

## 2. Step 2 – two-approver rule above 500,000

- [ ] 2.1 Record first approval on `Invoice`; keep status Pending
- [ ] 2.2 Require a Director as second approver; refuse Manager as second
- [ ] 2.3 Refuse the same user (case-insensitive) as both approvers
- [ ] 2.4 Unit tests for each rule, including both negative rules and "nothing changes"
- [ ] 2.5 Show first approver / "awaiting second approval" in the Approvals tab

## 3. End-to-end and quality gates

- [ ] 3.1 Playwright test: Manager attempts an over-limit approval, sees the reason, invoice stays pending
- [ ] 3.2 Mutation run (Stryker 4.13.0) before step 1 and after step 2; record scores
- [ ] 3.3 Update AGENTS.md, README demo accounts

## 4. Review

- [ ] 4.1 Run the review prompt against the PR diff; record each finding as fixed / dismissed / deferred
