/**
 * CI gate on the event contract.
 *
 * Getting analytics wrong is the single most expensive mistake available here: a missing
 * denominator or a duplicated event does not break the game, so nobody notices until the
 * funnel is already three days and one ad budget into being wrong. This script reads every
 * analytics.track(...) call in src/ and checks it against schema.ts.
 *
 * Checks:
 *   1. no event name that is not in EVENT_SPEC;
 *   2. every required param present at the call site (for literal object arguments);
 *   3. every declared event actually fired somewhere (a spec entry nothing emits is a lie).
 *
 * Run: npm run check:events
 */

import { readFileSync, readdirSync, statSync } from 'node:fs';
import { dirname, join, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

const here = dirname(fileURLToPath(import.meta.url));
const srcDir = resolve(here, '../src');
const schemaPath = resolve(srcDir, 'analytics/schema.ts');

// ---- parse EVENT_SPEC out of the schema without importing TypeScript.
const schemaSource = readFileSync(schemaPath, 'utf8');
const specBody = schemaSource.slice(
  schemaSource.indexOf('export const EVENT_SPEC'),
  schemaSource.indexOf('export type EventName'),
);

const spec = new Map();
const entryRe = /^\s{2}([a-z0-9_]+):\s*\{([\s\S]*?)\},\s*$/gm;
for (const match of specBody.matchAll(entryRe)) {
  const name = match[1];
  const body = match[2];
  const once = /once:\s*true/.test(body);
  const requiredMatch = body.match(/required:\s*\[([\s\S]*?)\]/);
  const required = requiredMatch
    ? requiredMatch[1]
        .split(',')
        .map((s) => s.trim().replace(/^['"]|['"]$/g, ''))
        .filter(Boolean)
    : [];
  spec.set(name, { once, required });
}

if (spec.size === 0) {
  console.error('check-events: could not parse EVENT_SPEC from schema.ts');
  process.exit(1);
}

// ---- walk src/ and collect track() calls.
function walk(dir) {
  const out = [];
  for (const entry of readdirSync(dir)) {
    const full = join(dir, entry);
    if (statSync(full).isDirectory()) out.push(...walk(full));
    else if (entry.endsWith('.ts')) out.push(full);
  }
  return out;
}

/**
 * Splits a params object literal into top-level keys, ignoring nested braces and strings.
 * Handles both `key: value` and ES6 shorthand `key` - the shorthand form is common here
 * (`{ muted }`, `{ placement, funnel_level }`) and missing it would produce false failures.
 * A spread (`...props`) makes the call unverifiable and is reported by the caller.
 */
function topLevelKeys(rawText) {
  const text = stripComments(rawText);
  const parts = [];
  let depth = 0;
  let token = '';
  let quote = null;
  for (let i = 0; i < text.length; i++) {
    const ch = text[i];
    if (quote) {
      token += ch;
      if (ch === quote && text[i - 1] !== '\\') quote = null;
      continue;
    }
    if (ch === '"' || ch === "'" || ch === '`') {
      quote = ch;
      token += ch;
      continue;
    }
    if (ch === '{' || ch === '[' || ch === '(') depth++;
    else if (ch === '}' || ch === ']' || ch === ')') depth--;
    if (depth === 0 && ch === ',') {
      parts.push(token);
      token = '';
      continue;
    }
    token += ch;
  }
  parts.push(token);

  const keys = [];
  let hasSpread = false;
  for (const raw of parts) {
    const part = raw.trim();
    if (!part) continue;
    if (part.startsWith('...')) {
      hasSpread = true;
      continue;
    }
    const colon = topLevelColon(part);
    const key = (colon >= 0 ? part.slice(0, colon) : part).trim().replace(/^['"]|['"]$/g, '');
    if (/^[a-z0-9_]+$/i.test(key)) keys.push(key);
  }
  return { keys, hasSpread };
}

/**
 * Removes // and block comments, respecting string literals. Needed because these call sites
 * carry explanatory comments inside the params object, and a comment containing a colon
 * would otherwise be parsed as a key.
 */
function stripComments(text) {
  let out = '';
  let quote = null;
  for (let i = 0; i < text.length; i++) {
    const ch = text[i];
    if (quote) {
      out += ch;
      if (ch === quote && text[i - 1] !== '\\') quote = null;
      continue;
    }
    if (ch === '"' || ch === "'" || ch === '`') {
      quote = ch;
      out += ch;
      continue;
    }
    if (ch === '/' && text[i + 1] === '/') {
      while (i < text.length && text[i] !== '\n') i++;
      out += '\n';
      continue;
    }
    if (ch === '/' && text[i + 1] === '*') {
      i += 2;
      while (i < text.length && !(text[i] === '*' && text[i + 1] === '/')) i++;
      i++;
      continue;
    }
    out += ch;
  }
  return out;
}

function topLevelColon(part) {
  let depth = 0;
  let quote = null;
  for (let i = 0; i < part.length; i++) {
    const ch = part[i];
    if (quote) {
      if (ch === quote && part[i - 1] !== '\\') quote = null;
      continue;
    }
    if (ch === '"' || ch === "'" || ch === '`') {
      quote = ch;
      continue;
    }
    if (ch === '{' || ch === '[' || ch === '(') depth++;
    else if (ch === '}' || ch === ']' || ch === ')') depth--;
    else if (depth === 0 && ch === ':') return i;
  }
  return -1;
}

/** Finds the argument text of a track( ... ) call by matching parentheses. */
function callArgs(source, openParen) {
  let depth = 0;
  let quote = null;
  for (let i = openParen; i < source.length; i++) {
    const ch = source[i];
    if (quote) {
      if (ch === quote && source[i - 1] !== '\\') quote = null;
      continue;
    }
    if (ch === '"' || ch === "'" || ch === '`') {
      quote = ch;
      continue;
    }
    if (ch === '(') depth++;
    else if (ch === ')') {
      depth--;
      if (depth === 0) return source.slice(openParen + 1, i);
    }
  }
  return '';
}

let errors = 0;
const seen = new Set();

for (const file of walk(srcDir)) {
  const source = readFileSync(file, 'utf8');
  const rel = file.slice(resolve(here, '..').length + 1);

  // Only `analytics.track(...)` - the one call convention. This also skips the façade's own
  // method declaration, which is a `track(` but not a call.
  const trackRe = /\banalytics\.track\s*\(/g;
  for (const match of source.matchAll(trackRe)) {
    const args = callArgs(source, match.index + match[0].length - 1);
    const nameMatch = args.match(/^\s*['"]([a-z0-9_]+)['"]/);
    if (!nameMatch) {
      // A dynamic event name would defeat the whole contract.
      console.error(`${rel}: analytics.track() called with a non-literal event name`);
      errors++;
      continue;
    }

    const name = nameMatch[1];
    seen.add(name);
    const entry = spec.get(name);
    if (!entry) {
      console.error(`${rel}: unknown event "${name}" (not in EVENT_SPEC)`);
      errors++;
      continue;
    }

    const objStart = args.indexOf('{');
    if (entry.required.length > 0 && objStart < 0) {
      console.error(`${rel}: ${name} needs params [${entry.required.join(', ')}] but got none`);
      errors++;
      continue;
    }
    if (objStart < 0) continue;

    // Take the first top-level object literal (the params argument).
    let depth = 0;
    let end = -1;
    for (let i = objStart; i < args.length; i++) {
      if (args[i] === '{') depth++;
      else if (args[i] === '}') {
        depth--;
        if (depth === 0) {
          end = i;
          break;
        }
      }
    }
    const objText = end > objStart ? args.slice(objStart + 1, end) : '';
    const { keys, hasSpread } = topLevelKeys(objText);
    if (hasSpread) {
      console.error(`${rel}: ${name} spreads its params - the contract cannot be verified`);
      errors++;
      continue;
    }
    const present = new Set(keys);
    const missing = entry.required.filter((key) => !present.has(key));
    if (missing.length > 0) {
      console.error(`${rel}: ${name} missing required param(s): ${missing.join(', ')}`);
      errors++;
    }
  }
}

for (const name of spec.keys()) {
  if (!seen.has(name)) {
    console.error(`schema.ts: "${name}" is declared but never fired anywhere in src/`);
    errors++;
  }
}

if (errors > 0) {
  console.error(`\ncheck-events: ${errors} problem(s).`);
  process.exit(1);
}
console.log(`check-events: ${spec.size} events declared, all fired, all params present.`);
