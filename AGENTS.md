# Project conventions for AI agents

Invoice approval demo app: ASP.NET Core 8 Web API with a small static web UI.

## Layout

- `backend/src/InvoiceApp.Domain`: business rules, no web or storage code. `ApprovalService` owns the invoice lifecycle (Draft, PendingApproval, Approved, Rejected).
- `backend/src/InvoiceApp.Api`: controllers, sign-in (`Auth/`), in-memory repository, and the web UI under `wwwroot/`.
- `backend/tests/InvoiceApp.Tests`: xUnit tests, one test class per class under test, named `<ClassUnderTest>Tests`.
- `e2e`: Playwright end-to-end tests (`tests/`) and page objects (`pages/`).
- `docs/use-cases`: short briefs for features we may build.

## Conventions

- Controllers live in `Controllers/`, one per resource, route `api/[controller]`. Request bodies are `record` types declared inside the controller.
- Controllers inherit `ApiControllerBase` and use `Handle(...)` so service errors map to 404, 409 and 400.
- Every endpoint is behind sign-in. Restrict actions with `[RequireRole("...")]`: destructive actions are Admin only, approvals are Manager or Director (see Approval rules).
- Business rules go in the Domain project, never in controllers.
- Register services in `Program.cs`. Repositories are interfaces in Domain with an in-memory implementation in Api.
- Every new rule or endpoint needs tests. Use `FakeInvoiceRepository` in tests.
- Use clear names; no one-letter variables or `flag` parameters.
- End-to-end tests use page objects and role-based locators (`getByRole`, `getByLabel`), never CSS or XPath selectors.

## Commands

- Run the app: `dotnet run --project backend/src/InvoiceApp.Api` then open http://localhost:5000
- Run the tests: `dotnet test backend/InvoiceApp.sln`
- Mutation tests (needs `dotnet tool install -g dotnet-stryker --version 4.13.0`; newer versions need a later runtime): `scripts/mutation.sh`
- Run end-to-end tests: `cd e2e && npm install && npx playwright install chromium && npm test`

## Demo accounts (not real credentials)

- `demo.manager` / `ChangeMe123!` (role Manager)
- `demo.director` / `ChangeMe123!` (role Director)
- `demo.director2` / `ChangeMe123!` (role Director, a second person for two-approver tests)
- `demo.admin` / `ChangeMe123!` (role Admin; can delete invoices, cannot approve)

## Never

- Commit real credentials, tokens or customer data.
- Add business logic to `Program.cs` or controllers.

## Approval rules (source of truth: `openspec/changes/add-approval-limits-by-role/specs`)

| Amount | Who may approve |
| --- | --- |
| up to and including 50,000 | Manager or Director, alone |
| above 50,000 up to and including 500,000 | Director, alone |
| above 500,000 | two **different** users; the second must be a Director (the first may be a Manager or a Director) |

- Limits are inclusive. 50,000 is a Manager amount; 50,000.01 is not. 500,000 is a Director amount; 500,000.01 needs two approvers.
- The same user may never be both approvers. Compare usernames case-insensitively.
- Any other role (Admin, unknown) cannot approve.
- Rejecting is not amount-limited. Any Manager or Director may reject, with a reason.
- A refused approval throws `ApprovalNotPermittedException` with a plain-language reason (API: 403 `{ "error": "..." }`). It must check everything first and mutate nothing: status, first approver, `DecidedBy`, `DecidedAt` stay as they were.
- An invoice above 500,000 stays `PendingApproval` after the first approval; the first approver is stored on the invoice.

## Layering

Controller (HTTP, reads role/username from session, no rules) -> `ApprovalService` (lifecycle, mutation) -> `ApprovalPolicy` (pure rules: invoice + approver + role -> allow or reason) -> `IInvoiceRepository`. Rules live in Domain only. Tests target the policy and service directly; the web layer is covered by one Playwright test.

## Review expectations

- Every change is reviewed against the OpenSpec spec, not just for style. The review must name the rule each finding relates to.
- Tests must be able to fail: for each rule there is a test that goes red if the rule is broken. Check boundaries (limit, limit + 0.01), both negative rules, and "nothing changed after refusal" (assert every field).
- Reject tests that only assert `NotNull`, assert the mock, or repeat the implementation.
- Review output uses the fixed format in `agent-config/skills/review-pr/SKILL.md`. Every finding ends as `fixed`, `dismissed (reason)` or `deferred (reason)`.
- Commits: one logical step each, message says what changed and why. Step 1 single-approver limits, step 2 two-approver rule.

## Agents, skills, hooks

- Subagents in `agent-config/agents/`: `spec-writer`, `domain-implementer`, `test-writer`, `e2e-writer`, `reviewer` (read-only), `mutation-analyst`.
- Skills in `agent-config/skills/`: `approval-rules`, `review-pr`, `run-quality-gates` (plus the OpenSpec skills).
- Hooks in `agent-config/settings.json` write every prompt, tool call and subagent result to `logs/ai-interactions.jsonl` in one fixed schema, and print test results in one fixed `TEST_RESULT` line.
- Do not hand-edit `logs/ai-interactions.jsonl`; it is append-only.