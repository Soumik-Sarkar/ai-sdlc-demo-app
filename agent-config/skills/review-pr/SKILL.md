---
name: review-pr
description: Review a pull request diff against the spec and emit findings in one fixed format with a verdict per finding.
---
# Review a diff

Review `git diff main...HEAD` against `openspec/changes/*/specs`. Output **only** this structure, one block per finding, highest severity first:

```
FINDING-<n>
severity: blocker | major | minor | nit
rule: <spec requirement or AGENTS.md rule it relates to>
location: <file>:<line>
problem: <one sentence>
evidence: <input/state -> wrong result, or why a test cannot fail>
suggestion: <one sentence>
verdict: pending
```

End with `SUMMARY findings=<n> blockers=<n>`. The human then replaces each `pending` with `fixed`, `dismissed: <reason>` or `deferred: <reason>`. Do not invent findings to reach a count; if there are none, output only the SUMMARY line.

Always check: boundaries, both negative rules, atomic refusal, role check vs. controller, tests that assert too little, hard-coded secrets, layering violations.
