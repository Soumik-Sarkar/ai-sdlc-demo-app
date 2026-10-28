#!/usr/bin/env node
// Appends one redacted JSONL record per AI interaction.
// Fixed schema: ts, event, session_id, agent, tool, summary, detail
import { appendFileSync, mkdirSync, readFileSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';

const root = join(dirname(fileURLToPath(import.meta.url)), '..', '..');
const logFile = join(root, 'logs', 'ai-interactions.jsonl');

const redact = (value) => String(value ?? '')
  .replace(/("(?:password|passwd|token|secret|api[_-]?key|authorization|cookie)"\s*:\s*")[^"]*(")/gi, '$1[REDACTED]$2')
  .replace(/\bBearer\s+[A-Za-z0-9._~+\/-]+=*/gi, 'Bearer [REDACTED]')
  .replace(/\b(password|passwd|token|secret|api[_-]?key|authorization|cookie)(\s*[:=]\s*)([^\s,;]+)/gi, '$1$2[REDACTED]');

const clip = (value, max = 400) => redact(value).replace(/[\r\n\t]+/g, ' ').replace(/\s{2,}/g, ' ').trim().slice(0, max);

let input = {};
try { input = JSON.parse(readFileSync(0, 'utf8') || '{}'); } catch { /* tolerate empty or invalid hook input */ }

const event = input.hook_event_name || process.argv[2] || 'unknown';
const tool = input.tool_name || '';
const toolInput = input.tool_input || {};
let summary = '';
let detail = '';

switch (event) {
  case 'UserPromptSubmit':
    summary = clip(input.prompt);
    break;
  case 'PostToolUse':
    summary = clip(toolInput.command || toolInput.file_path || toolInput.description || JSON.stringify(toolInput));
    detail = clip(typeof input.tool_response === 'string' ? input.tool_response : JSON.stringify(input.tool_response ?? ''), 300);
    break;
  case 'SubagentStop':
    summary = clip(input.last_assistant_message || input.agent_type || 'subagent finished');
    break;
  default:
    summary = clip(input.last_assistant_message || event);
}

const record = {
  ts: new Date().toISOString(),
  event,
  session_id: clip(input.session_id || 'unspecified', 120),
  agent: clip(input.agent_type || 'main', 120),
  tool: clip(tool, 120),
  summary,
  detail,
};

mkdirSync(dirname(logFile), { recursive: true });
let needsSeparator = false;
try {
  const existing = readFileSync(logFile, 'utf8');
  needsSeparator = existing.length > 0 && !existing.endsWith('\n');
} catch { /* first log record */ }
appendFileSync(logFile, (needsSeparator ? '\n' : '') + JSON.stringify(record) + '\n', 'utf8');