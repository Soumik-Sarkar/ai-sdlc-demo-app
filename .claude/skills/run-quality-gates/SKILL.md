---
name: run-quality-gates
description: Run unit tests, mutation tests and the Playwright test and report results in the standard format.
---
1. `dotnet test backend/InvoiceApp.sln` (hook prints `TEST_RESULT`).
2. `scripts/mutation.sh` -> Stryker report, copy the "mutation score" line as text.
3. `cd e2e && npm install && npx playwright install chromium && npm test`.
Report each as `TEST_RESULT runner=... passed=.. failed=.. skipped=..` and `MUTATION_SCORE <percent>`.
