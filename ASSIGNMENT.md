# AI in SDLC assignment: approval limits by role

**Pull request:** https://github.com/Soumik-Sarkar/ai-sdlc-demo-app/pull/1
**Demo video:** <VIDEO_LINK_PENDING> (to be added after recording)

## What was built

Role-based approval limits in the invoice approval service:

| Amount        | Who may approve                            |
| ------------- | ------------------------------------------ |
| up to 50,000  | Manager (or Director)                      |
| up to 500,000 | Director                                   |
| above 500,000 | two different users, the second a Director |

A refused approval returns a plain-language reason (API: 403 `{ "error": "..." }`) and changes nothing on the invoice. Limits are inclusive; the assumptions I made (first approver may be Manager or Director, usernames compared case-insensitively, rejecting is not amount-limited) are listed in `openspec/changes/add-approval-limits-by-role/proposal.md`.

## How the work was done

| Requirement             | Where                                                                                                                                                                                                                                                                             |
| ----------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Context file            | `AGENTS.md`: approval rules table, layering, test and mutation commands, review expectations                                                                                                                                                                                      |
| Skills                  | `agent-config/skills/approval-rules` (rules + the traps agents fall into), `review-pr` (fixed review format), `run-quality-gates`                                                                                                                                                 |
| Hooks                   | `agent-config/settings.json` + `agent-config/hooks/`: `log-event.mjs` is configured to append structured prompt, tool and subagent events to `logs/ai-interactions.jsonl`; `format-test-result.mjs` adds a fixed `TEST_RESULT runner= passed= failed= skipped=` line after test runs |
| Spec-driven tool        | OpenSpec 1.15.0: `openspec/changes/add-approval-limits-by-role/` (proposal, spec, design, tasks)                                                                                                                                                                                  |
| Agents and sub-agents   | `agent-config/agents/`: spec-writer, domain-implementer, test-writer, e2e-writer, reviewer (read-only), mutation-analyst. The independent review was run by a fresh agent that had not seen my reasoning                                                                          |
| Two-step implementation | Commit 3 = single-approver limits + tests; commit 4 = two-approver rule + tests                                                                                                                                                                                                   |
| Unit tests              | `backend/tests/InvoiceApp.Tests/ApprovalPolicyTests.cs`, `ApprovalServiceTests.cs` (49 cases)                                                                                                                                                                                     |
| Playwright              | `e2e/tests/approval-limits.spec.ts` + page object `e2e/pages/InvoiceAppPage.ts`                                                                                                                                                                                                   |
| Review with verdicts    | `docs/review/review-findings.md` and `docs/pull-request.md`                                                                                                                                                                                                                       |
| AI log                  | `logs/ai-interactions.jsonl`                                                                                                                                                                                                                                                      |

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

### Stryker mutation score (dotnet-stryker 4.13.0, .NET 8)

Command, run in `backend/tests/InvoiceApp.Tests`: `dotnet stryker --project InvoiceApp.Domain.csproj`

```
Before (commit 66cb3f9, original code and tests; DiscountCalculator excluded)
  22 mutants: 12 killed, 10 survived
  The final mutation score is 54.55 %

After (this branch, HEAD)
  The final mutation score is 80.00 %
  ApprovalPolicy.cs   100.00 %   (the limit and two-approver rules)
  ApprovalService.cs   63.64 %
  Invoice.cs            0.00 %
```

The score rose from 54.55 % to 80.00 %, and `ApprovalPolicy`, where the approval rules live, has no surviving mutants. The remaining survivors are in `ApprovalService` and `Invoice`; the full list is in the Stryker HTML report (`StrykerOutput/.../mutation-report.html`). 100 % overall is not the goal: some survivors are in code that cannot be checked by a deterministic test (for example the lock added after review) or in default values and message text.

### Supplementary: hand-injected bug probe (not Stryker)

To check the tests early I injected 16 realistic bugs one at a time into `ApprovalPolicy.cs` and `ApprovalService.cs` and ran the unit tests (using a small throwaway test runner, before the full suite was run):

- First test set: 15 of 16 caught. Survivor: first approval not saved (`_repository.Update` removed).
- After adding `FakeInvoiceRepository.Updated` and two tests: 16 of 16 caught.

Bugs injected: `>` to `>=` at both limits; both limit constants off by one; role gate `&&` to `||`; Manager treated as non-approver; case-sensitive same-user check; same-user check removed; Director-second check removed; first/second inverted; two-approver rule removed; outcome inverted; first approver not recorded; first approval time not recorded; final approver not recorded; first approval not saved.

## Review of my own pull request

Nine findings from an independent reviewer agent using the `review-pr` format. Fixed: 1, 2, 4, 5, 9. Deferred: 3, 6. Dismissed: 7, 8. Details and reasons are in `docs/review/review-findings.md` (also the PR description).

Agent, skill and hook definitions live in `agent-config/`. To activate them in an AI coding assistant, copy or link the folder to the location that assistant reads its project configuration from; `agent-config/hooks/*.mjs` are plain Node scripts that read the event as JSON on stdin.

## About the AI log

`logs/ai-interactions.jsonl` is written by the hooks in `agent-config/settings.json` (`log-event.mjs`): one JSON line per prompt, tool call and agent result, always with the same fields (`ts, event, session_id, agent, tool, summary, detail`). The checked-in 16-entry file contains a retrospective session recap plus a structured record of the PR hand-in action recorded through the project logger (`session_id: cloud-session-recap`); it is not a verbatim transcript of every prompt and tool call. The hook configuration and logger are included for structured event capture in sessions where the assistant runs those hooks. Entries share the schema `ts, event, session_id, agent, tool, summary, detail`; summaries are concise and tool output is clipped. The file is append-only; do not rewrite historical entries.

## Known limits

- Decisions are serialised with an in-process lock, so the protection against two simultaneous approvals only holds for a single running instance of the in-memory app.
- Only one Playwright test was written, as the assignment asks; the two-approver flow is covered by unit tests.
