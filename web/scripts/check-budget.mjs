/**
 * CI gate on the load budget and on the content set.
 *
 * The budget is not a nicety. The measured funnel says 30-50% of paid clicks ever become a
 * session at all, and the biggest single leak is asset loading; past ~3s on 4G the drop-off
 * rate climbs sharply. If the bundle creeps, the test stops measuring the mechanic and starts
 * measuring the loader - and a good idea gets killed by a progress bar.
 *
 * The content check is the other half: content_set "internal" is the four Unity demo
 * templates, which contain third-party copyrighted characters. They are fine for dev/QA and
 * must never be what an ad points at.
 *
 * Run: npm run check:budget   (after vite build)
 */

import { gzipSync } from 'node:zlib';
import { readFileSync, readdirSync, existsSync, statSync } from 'node:fs';
import { dirname, join, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

const here = dirname(fileURLToPath(import.meta.url));
const dist = resolve(here, '../dist');

if (!existsSync(dist)) {
  console.error('check-budget: no dist/ - run vite build first');
  process.exit(1);
}

const KB = 1024;
const BUDGET = {
  /** index.html carries the inlined CSS and the base64 subset font. */
  html: 26 * KB,
  js: 60 * KB,
  /** catalog.json + the first picture: everything else streams in during play. */
  firstContent: 10 * KB,
  total: 110 * KB,
  requests: 4,
};

const gz = (buf) => gzipSync(buf, { level: 9 }).length;
const fmt = (bytes) => `${(bytes / KB).toFixed(1)} KB`;

function walk(dir) {
  const out = [];
  for (const entry of readdirSync(dir)) {
    const full = join(dir, entry);
    if (statSync(full).isDirectory()) out.push(...walk(full));
    else out.push(full);
  }
  return out;
}

const files = walk(dist);
let errors = 0;

// ---------------------------------------------------------------- content set

const catalogPath = resolve(dist, 'puzzles/catalog.json');
if (!existsSync(catalogPath)) {
  console.error('check-budget: dist/puzzles/catalog.json is missing');
  process.exit(1);
}
const catalog = JSON.parse(readFileSync(catalogPath, 'utf8'));
const allowInternal = process.env.ALLOW_INTERNAL_CONTENT === '1';

console.log(`content_set: ${catalog.content_set} (${catalog.pictures.length} pictures)`);
if (catalog.content_set === 'internal' && !allowInternal) {
  console.error('FAIL content_set is "internal" - third-party copyrighted artwork.');
  console.error('     A public build must ship the clean-room set (npm run content:safe).');
  console.error('     For an internal QA build only: ALLOW_INTERNAL_CONTENT=1');
  errors++;
}

// ---------------------------------------------------------------- sizes

// Only the game's own entry page counts toward the first load. /soon and /privacy are
// separate destinations reached by a deliberate tap, not part of getting into the game.
const html = files.filter((f) => f.endsWith('index.html') && dirname(f) === dist);
const otherPages = files.filter((f) => f.endsWith('.html') && dirname(f) !== dist);
const js = files.filter((f) => f.endsWith('.js'));
const css = files.filter((f) => f.endsWith('.css'));

const htmlBytes = html.reduce((sum, f) => sum + gz(readFileSync(f)), 0);
const jsBytes = js.reduce((sum, f) => sum + gz(readFileSync(f)), 0);
const cssBytes = css.reduce((sum, f) => sum + gz(readFileSync(f)), 0);

const firstPicture = resolve(dist, `puzzles/${catalog.pictures[0]}.json`);
const contentBytes =
  gz(readFileSync(catalogPath)) + (existsSync(firstPicture) ? gz(readFileSync(firstPicture)) : 0);

const total = htmlBytes + jsBytes + cssBytes + contentBytes;

// index.html + one JS chunk + catalog.json + first picture. CSS and the font are inlined into
// the HTML precisely so they do not add round trips.
const requests = html.length + js.length + css.length + 2;

const rows = [
  ['index.html (incl. inlined CSS + font)', htmlBytes, BUDGET.html],
  ['js', jsBytes, BUDGET.js],
  ['first content (catalog + picture 1)', contentBytes, BUDGET.firstContent],
  ['TOTAL first load', total, BUDGET.total],
];

console.log('');
for (const [label, bytes, budget] of rows) {
  const ok = bytes <= budget;
  console.log(`${ok ? 'ok  ' : 'FAIL'} ${label.padEnd(38)} ${fmt(bytes).padStart(9)} / ${fmt(budget)}`);
  if (!ok) errors++;
}

if (cssBytes > 0) {
  console.log(`note  ${cssBytes} bytes of CSS shipped as a separate file - inlining did not run`);
}

const pageList = otherPages.map((f) => f.slice(dist.length + 1).split('\\').join('/')).join(', ');
console.log(`note  ${otherPages.length} standalone page(s): ${pageList}`);

const requestsOk = requests <= BUDGET.requests;
console.log(`${requestsOk ? 'ok  ' : 'FAIL'} first-load requests${' '.repeat(20)} ${requests} / ${BUDGET.requests}`);
if (!requestsOk) errors++;

// Analytics SDKs are excluded on purpose: they are loaded after first_frame, which is why
// page_open is queued rather than sent immediately. They cost nothing before interactive.

if (errors > 0) {
  console.error(`\ncheck-budget: ${errors} problem(s).`);
  process.exit(1);
}
console.log('\ncheck-budget: within budget.');
