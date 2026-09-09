/**
 * Fallback content path: reads the Unity .asset files directly and writes the internal
 * content set, without needing the editor open.
 *
 * The primary path is Assets/_Game/Demo/Scripts/Editor/ShidakuWebExporter.cs. This exists for
 * the case the plan calls out - working on the web prototype on a machine that has no Unity
 * licence or no time to wait for a domain reload. Same JSON schema, same checks.
 *
 * Run: npm run content:internal
 *
 * The four puzzles it reads are cross-stitch charts from outside sources containing
 * third-party copyrighted characters, so it writes content_set "internal" and
 * check-budget.mjs refuses to ship it publicly.
 */

import { mkdirSync, readFileSync, readdirSync, writeFileSync, existsSync } from 'node:fs';
import { dirname, resolve, basename } from 'node:path';
import { fileURLToPath } from 'node:url';

const here = dirname(fileURLToPath(import.meta.url));
const web = resolve(here, '..');
const repo = resolve(web, '..');
const demo = resolve(repo, 'Assets/_Game/Demo');
const outDir = resolve(web, 'public/puzzles');

const B64 = 'ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-_';
const MAX_REGION_AREA = 15;

if (!existsSync(demo)) {
  console.error(`no Unity demo folder at ${demo}`);
  process.exit(1);
}

/** guid -> asset path, from the .meta files, so SourceArt references can be resolved. */
function guidIndex(dir) {
  const map = new Map();
  for (const entry of readdirSync(dir)) {
    if (!entry.endsWith('.meta')) continue;
    const text = readFileSync(resolve(dir, entry), 'utf8');
    const guid = /guid:\s*([0-9a-f]+)/.exec(text);
    if (guid) map.set(guid[1], resolve(dir, basename(entry, '.meta')));
  }
  return map;
}

/**
 * Template.cs stores Colors as a Color32[]. Unity serialises each as `rgba: <uint32>` in
 * little-endian byte order, so the low byte is red and the high byte is alpha.
 */
function parseTemplate(path) {
  const text = readFileSync(path, 'utf8');
  const width = Number(/^\s*Width:\s*(\d+)/m.exec(text)?.[1]);
  const height = Number(/^\s*Height:\s*(\d+)/m.exec(text)?.[1]);
  if (!Number.isFinite(width) || !Number.isFinite(height)) {
    throw new Error(`${basename(path)}: could not read Width/Height`);
  }

  const rgba = [];
  for (const match of text.matchAll(/^\s*rgba:\s*(\d+)/gm)) rgba.push(Number(match[1]));
  if (rgba.length !== width * height) {
    throw new Error(`${basename(path)}: ${rgba.length} colours for a ${width}x${height} grid`);
  }

  const colors = rgba.map((value) => ({
    r: value & 0xff,
    g: (value >>> 8) & 0xff,
    b: (value >>> 16) & 0xff,
    a: (value >>> 24) & 0xff,
  }));
  return { width, height, colors };
}

/** PuzzleData.cs: SourceArt guid plus the Sections/Regions list, in Unity coordinates. */
function parsePuzzle(path) {
  const text = readFileSync(path, 'utf8');
  const sourceGuid = /SourceArt:\s*\{fileID:\s*\d+,\s*guid:\s*([0-9a-f]+)/.exec(text)?.[1];
  if (!sourceGuid) throw new Error(`${basename(path)}: no SourceArt reference`);

  const sectionsStart = text.indexOf('\n  Sections:');
  if (sectionsStart < 0) throw new Error(`${basename(path)}: no Sections block`);
  const body = text.slice(sectionsStart);

  const sections = [];
  let current = null;
  const lines = body.split(/\r?\n/);
  let pending = null;

  for (const line of lines) {
    if (/^\s*- Regions:\s*$/.test(line)) {
      current = { regions: [] };
      sections.push(current);
      continue;
    }
    if (/^\s*- Rect:\s*$/.test(line)) {
      pending = { r: [0, 0, 0, 0], c: [0, 0] };
      continue;
    }
    if (!pending) continue;

    const x = /^\s*x:\s*(-?\d+)\s*$/.exec(line);
    const y = /^\s*y:\s*(-?\d+)\s*$/.exec(line);
    const width = /^\s*width:\s*(-?\d+)\s*$/.exec(line);
    const height = /^\s*height:\s*(-?\d+)\s*$/.exec(line);
    const clue = /^\s*ClueCell:\s*\{x:\s*(-?\d+),\s*y:\s*(-?\d+)\}/.exec(line);

    if (x) pending.r[0] = Number(x[1]);
    else if (y) pending.r[1] = Number(y[1]);
    else if (width) pending.r[2] = Number(width[1]);
    else if (height) pending.r[3] = Number(height[1]);
    else if (clue) {
      pending.c = [Number(clue[1]), Number(clue[2])];
      if (!current) throw new Error(`${basename(path)}: a region appeared before any section`);
      current.regions.push(pending);
      pending = null;
    }
  }

  if (sections.length === 0) throw new Error(`${basename(path)}: parsed 0 sections`);
  return { sourceGuid, sections };
}

// ---------------------------------------------------------------- build

const templateGuids = guidIndex(resolve(demo, 'Templates'));
const puzzleFiles = readdirSync(resolve(demo, 'Puzzles'))
  .filter((f) => f.endsWith('.asset'))
  .sort((a, b) => a.localeCompare(b, 'en', { numeric: true }));

let failures = 0;
const pictures = [];

for (const file of puzzleFiles) {
  const id = `u${basename(file, '.asset')}`;
  try {
    const puzzle = parsePuzzle(resolve(demo, 'Puzzles', file));
    const templatePath = templateGuids.get(puzzle.sourceGuid);
    if (!templatePath) throw new Error(`SourceArt guid ${puzzle.sourceGuid} not found in Templates/`);
    const template = parseTemplate(templatePath);
    const { width: w, height: h, colors } = template;

    // palette + per-pixel index, in Unity order (index 0 = bottom-left row)
    const palette = ['#00000000'];
    const lookup = new Map();
    const indices = new Array(w * h).fill(0);
    for (let i = 0; i < colors.length; i++) {
      const c = colors[i];
      if (c.a === 0) continue;
      const key = (c.r << 16) | (c.g << 8) | c.b;
      let index = lookup.get(key);
      if (index === undefined) {
        index = palette.length;
        lookup.set(key, index);
        palette.push(
          '#' + [c.r, c.g, c.b].map((v) => v.toString(16).padStart(2, '0').toUpperCase()).join(''),
        );
      }
      indices[i] = index;
    }
    if (palette.length > B64.length) {
      throw new Error(`${palette.length - 1} distinct colours, max ${B64.length - 1}`);
    }

    // The same three Verify checks the Unity exporter runs.
    const owner = new Array(w * h).fill(-1);
    const problems = [];
    puzzle.sections.forEach((section, s) => {
      for (const region of section.regions) {
        const [rx, ry, rw, rh] = region.r;
        const area = rw * rh;
        if (area > MAX_REGION_AREA) problems.push(`s${s}: region ${region.r} is ${area} cells`);
        let first = -1;
        for (let y = ry; y < ry + rh; y++) {
          for (let x = rx; x < rx + rw; x++) {
            const flat = y * w + x;
            if (x < 0 || y < 0 || x >= w || y >= h) {
              problems.push(`s${s}: region ${region.r} leaves the canvas`);
              continue;
            }
            if (owner[flat] !== -1) problems.push(`cell ${x},${y} in sections ${owner[flat]} and ${s}`);
            owner[flat] = s;
            if (indices[flat] === 0) problems.push(`s${s}: region ${region.r} covers a transparent cell`);
            else if (first === -1) first = indices[flat];
            else if (indices[flat] !== first) problems.push(`s${s}: region ${region.r} spans two colours`);
          }
        }
      }
    });
    for (let i = 0; i < indices.length; i++) {
      if (indices[i] !== 0 && owner[i] === -1) {
        problems.push(`artwork cell ${i % w},${Math.floor(i / w)} belongs to no region`);
      }
    }

    // De-duplicate: a single mistake tends to repeat across every cell it touches.
    const unique = [...new Set(problems)];
    if (unique.length > 0) {
      console.error(`FAIL ${id}: ${unique.length} problem(s)`);
      for (const problem of unique.slice(0, 5)) console.error(`     ${problem}`);
      failures++;
      continue;
    }

    const regionCount = puzzle.sections.reduce((sum, s) => sum + s.regions.length, 0);
    console.log(
      `${id} ${w}x${h}  sections=${puzzle.sections.length} regions=${regionCount} colours=${palette.length - 1}`,
    );

    pictures.push({
      id,
      name: basename(file, '.asset'),
      w,
      h,
      palette,
      pixels: indices.map((i) => B64[i]).join(''),
      sections: puzzle.sections,
      source: `Unity asset Puzzles/${file} (THIRD-PARTY ARTWORK - dev/QA only)`,
      exported_at: new Date().toISOString().slice(0, 10),
    });
  } catch (err) {
    console.error(`FAIL ${id}: ${err.message}`);
    failures++;
  }
}

if (failures > 0 || pictures.length === 0) {
  console.error(`\n${failures} puzzle(s) failed. Nothing written.`);
  process.exit(1);
}

mkdirSync(outDir, { recursive: true });
for (const picture of pictures) {
  writeFileSync(resolve(outDir, `${picture.id}.json`), JSON.stringify(picture), 'utf8');
}
writeFileSync(
  resolve(outDir, 'catalog.json'),
  JSON.stringify({ version: 1, content_set: 'internal', pictures: pictures.map((p) => p.id) }),
  'utf8',
);

console.log(`\nwrote ${pictures.length} puzzles -> public/puzzles/ (content_set: internal)`);
console.log('WARNING: this artwork is third-party. DEV/QA ONLY - never for a public build.');
console.log('Restore the publishable set with: npm run content:safe');
