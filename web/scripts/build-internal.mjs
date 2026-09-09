/**
 * Builds the DEV/QA bundle against the internal content set (the four Unity demo templates).
 *
 * Separate script rather than an env prefix on the npm script, because `VAR=x cmd` is not
 * valid on Windows cmd/PowerShell and this repo is developed on Windows.
 *
 * The internal artwork contains third-party copyrighted characters. This build is for
 * checking the pipeline against real Unity-authored puzzles - never for anything public,
 * which is why check-budget still has to be told explicitly to allow it.
 */

import { spawnSync } from 'node:child_process';
import { dirname, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

const here = dirname(fileURLToPath(import.meta.url));
const web = resolve(here, '..');

console.log('Building with content_set=internal (DEV/QA ONLY - not publishable)\n');

const build = spawnSync('npx', ['vite', 'build'], {
  cwd: web,
  stdio: 'inherit',
  shell: true,
  env: { ...process.env, CONTENT_SET: 'internal' },
});
if (build.status !== 0) process.exit(build.status ?? 1);

const check = spawnSync('node', ['scripts/check-budget.mjs'], {
  cwd: web,
  stdio: 'inherit',
  shell: true,
  env: { ...process.env, ALLOW_INTERNAL_CONTENT: '1' },
});
process.exit(check.status ?? 0);
