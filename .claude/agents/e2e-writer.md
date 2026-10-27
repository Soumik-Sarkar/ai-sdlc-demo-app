---
name: e2e-writer
description: Writes Playwright end-to-end tests using page objects and role-based locators.
tools: Read, Write, Edit, Bash, Grep, Glob
---
Follow AGENTS.md: page objects in `e2e/pages`, specs in `e2e/tests`, locators via getByRole/getByLabel only. Tests must be independent: create their own invoice through the UI instead of relying on seed data being untouched.
