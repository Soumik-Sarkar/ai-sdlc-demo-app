#!/usr/bin/env node
// After a `dotnet test` or `npx playwright test` command, add one line in a fixed format
// so every agent reports test results the same way:
//   TEST_RESULT runner=<dotnet|playwright> passed=<n> failed=<n> skipped=<n>
import { readFileSync } from 'node:fs';

let input = {};
try { input = JSON.parse(readFileSync(0, 'utf8') || '{}'); } catch { process.exit(0); }

const command = input.tool_input?.command || '';
const isDotnet = /dotnet\s+test/.test(command);
const isPlaywright = /playwright\s+test|npm\s+(run\s+)?test/.test(command);
if (!isDotnet && !isPlaywright) process.exit(0);

const response = input.tool_response;
const output = typeof response === 'string' ? response : [response?.stdout, response?.stderr, response?.output].filter(Boolean).join('\n');
const pick = (regex) => Number((output.match(regex) || [])[1] || 0);

let passed, failed, skipped;
if (isDotnet) {
  passed = pick(/Passed:\s*(\d+)/i);
  failed = pick(/Failed:\s*(\d+)/i);
  skipped = pick(/Skipped:\s*(\d+)/i);
} else {
  passed = pick(/(\d+)\s+passed/i);
  failed = pick(/(\d+)\s+failed/i);
  skipped = pick(/(\d+)\s+skipped/i);
}

console.log(JSON.stringify({
  hookSpecificOutput: {
    hookEventName: 'PostToolUse',
    additionalContext: `TEST_RESULT runner=${isDotnet ? 'dotnet' : 'playwright'} passed=${passed} failed=${failed} skipped=${skipped}`,
  },
}));
