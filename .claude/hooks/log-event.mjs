#!/usr/bin/env node
// Appends one JSON line per AI interaction to logs/ai-interactions.jsonl.
// Fixed schema: ts, event, session_id, agent, tool, summary, detail
import { appendFileSync, mkdirSync, readFileSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';

const root = join(dirname(fileURLToPath(import.meta.url)), '..', '..');
const logFile = join(root, 'logs', 'ai-interactions.jsonl');
const clip = (value, max = 400) => String(value ?? '').replace(/\s+/g, ' ').slice(0, max);

let input = {};
try { input = JSON.parse(readFileSync(0, 'utf8') || '{}'); } catch { /* ignore bad input */ }

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
  session_id: input.session_id || '',
  agent: input.agent_type || 'main',
  tool,
  summary,
  detail,
};

mkdirSync(dirname(logFile), { recursive: true });
appendFileSync(logFile, JSON.stringify(record) + '\n');
