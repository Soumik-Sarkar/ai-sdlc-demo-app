# AI in SDLC assignment: approval limits by role

**Video:** `<<< PASTE VIDEO LINK HERE >>>`  
**Pull request:** `<<< PASTE PR LINK HERE >>>`

## What was built

Role-based approval limits in the invoice approval service:

| Amount | Who may approve |
| --- | --- |
| up to 50,000 | Manager (or Director) |
| up to 500,000 | Director |
| above 500,000 | two different users, the second a Director |

A refused approval returns a plain-language reason (API: 403 `{ "error": "..." }`) and changes nothing on the invoice. Limits are inclusive; the assumptions I made (first approver may be Manager or Director, usernames compared case-insensitively, rejecting is not amount-limited) are listed in `openspec/changes/add-approval-limits-by-role/proposal.md`.

## How the work was done

| Requirement | Where |
| --- | --- |
| Context file | `AGENTS.md`: approval rules table, layering, test and mutation commands, review expectations |
| Skills | `.claude/skills/approval-rules` (rules + the traps agents fall into), `review-pr` (fixed review format), `run-quality-gates` |
| Hooks | `.claude/settings.json` + `.claude/hooks/`: `log-event.mjs` writes every prompt, tool call and subagent result to `logs/ai-interactions.jsonl` in one schema; `format-test-result.mjs` adds a fixed `TEST_RESULT runner= passed= failed= skipped=` line after test runs |
| Spec-driven tool | OpenSpec 1.15.0: `openspec/changes/add-approval-limits-by-role/` (proposal, spec, design, tasks) |
| Agents and sub-agents | `.claude/agents/`: spec-writer, domain-implementer, test-writer, e2e-writer, reviewer (read-only), mutation-analyst. The independent review was run by a fresh agent that had not seen my reasoning |
| Two-step implementation | Commit 3 = single-approver limits + tests; commit 4 = two-approver rule + tests |
| Unit tests | `backend/tests/InvoiceApp.Tests/ApprovalPolicyTests.cs`, `ApprovalServiceTests.cs` (49 cases) |
| Playwright | `e2e/tests/approval-limits.spec.ts` + page object `e2e/pages/InvoiceAppPage.ts` |
| Review with verdicts | `docs/review/review-findings.md` and `docs/pull-request.md` |
| AI log | `logs/ai-interactions.jsonl` |

### Commits

1. Add OpenSpec change: approval limits by role
2. Extend AGENTS.md; add agents, skills and logging hooks
3. Enforce approval limits by role (single approver)
4. Require two distinct approvers above 500,000
5. Add Playwright test for a rejected over-limit approval
6. Address review findings
7. Add write-up, review record and AI log

## Where the agent was most likely to be wrong, and what I did

- **Boundaries.** `<` vs `<=` at 50,000 and 500,000. Tests sit on limit and limit + 0.01 for both roles.
- **Same user twice.** A naive check compares the second approver with `DecidedBy`, which is still empty after a first approval. The rule compares with `FirstApprovedBy`, case-insensitively; tests cover Director twice, different letter case, and Manager twice.
- **Manager as second approver.** The amount check alone would let a Manager through; a separate test asserts the refusal.
- **"Nothing changes".** All checks run in `ApprovalPolicy` before any write. Tests snapshot every field (status, first approver, decider, timestamps, note, amount) and assert the repository was never asked to save.
- **A test that could not fail.** My first test set did not catch a bug where the first approval was never saved, because the fake repository's `Update` did nothing. I found this by injecting bugs by hand, then made the fake record updates and added tests for it.

## Test and mutation results

Unit tests: 49 cases. Playwright: 1 passed (`a Manager cannot approve an invoice above the 50,000 limit and the invoice stays pending`).

### Stryker mutation score (to be filled in from your machine)

I could not run Stryker where this was written (the sandbox blocks NuGet, so xunit and Stryker cannot be installed). **Run these and paste the real output here; do not submit placeholders:**

```
dotnet tool install -g dotnet-stryker --version 4.13.0

# BEFORE: baseline on the original code and tests
git stash -u            # if needed
git checkout 66cb3f9
scripts/mutation.sh
git checkout feature/approval-limits-by-role

# AFTER
scripts/mutation.sh
```

```
Before (commit 66cb3f9):   mutation score = ____ %   (killed __ / survived __ / no coverage __)
After  (this branch HEAD): mutation score = ____ %   (killed __ / survived __ / no coverage __)
```

`stryker-config.json` excludes `DiscountCalculator.cs` so both scores measure the approval code. Note: `scripts/mutation.sh` is written for the current layout; on the baseline commit the config file does not exist, so run `cd backend/tests/InvoiceApp.Tests && dotnet stryker --project InvoiceApp.Domain.csproj --mutate "!**/DiscountCalculator.cs"` there.

### Supplementary: hand-injected bug probe (not Stryker)

To check the tests early I injected 16 realistic bugs one at a time into `ApprovalPolicy.cs` and `ApprovalService.cs` and ran the unit tests (through a local stand-in for xunit, because NuGet was blocked):

- First test set: 15 of 16 caught. Survivor: first approval not saved (`_repository.Update` removed).
- After adding `FakeInvoiceRepository.Updated` and two tests: 16 of 16 caught.

Bugs injected: `>` to `>=` at both limits; both limit constants off by one; role gate `&&` to `||`; Manager treated as non-approver; case-sensitive same-user check; same-user check removed; Director-second check removed; first/second inverted; two-approver rule removed; outcome inverted; first approver not recorded; first approval time not recorded; final approver not recorded; first approval not saved.

## Review of my own pull request

Nine findings from an independent reviewer agent using the `review-pr` format. Fixed: 1, 2, 4, 5, 9. Deferred: 3, 6. Dismissed: 7, 8. Details and reasons are in `docs/review/review-findings.md` (also the PR description).

## About the AI log

`logs/ai-interactions.jsonl` is written by the hooks in `.claude/settings.json`. This work was done in a cloud session where those hooks were not active while I worked, so the 15 entries in the file are a summary of the session written through the same logger script (`session_id: cloud-session-recap`), not live captures. From now on, any Claude Code session opened in this repo logs live in the same format.

## Known limits

- NuGet was blocked, so `dotnet test` with real xunit and Stryker were not run in this environment. Run `dotnet test backend/InvoiceApp.sln` before pushing.
- Stored state is in-memory; the lock in `ApprovalService` serialises decisions within one process only.
