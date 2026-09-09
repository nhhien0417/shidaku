/**
 * Builds public/puzzles/*.json + catalog.json for content_set "safe".
 *
 * Run: npm run content:safe
 *
 * Fails loudly on any content-rule violation, because a broken board discovered mid-test
 * costs a whole ad spend, not a rebuild.
 */

import { mkdirSync, writeFileSync } from 'node:fs';
import { dirname, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';
import { SAFE_ART } from './art-safe.mjs';
import { buildPuzzle, emitPictureJson, gridFromArt, statsFor } from './lib/build.mjs';

const here = dirname(fileURLToPath(import.meta.url));
const outDir = resolve(here, '../public/puzzles');

/**
 * Per-picture band height. Smaller bands = smaller, easier sections.
 *
 * p01 gets the shortest bands on purpose: its first sections are where the "is this legible"
 * gate (section 1 completion >= 70%) is actually measured, so they have to be small enough
 * that failing them means "did not understand", never "ran out of patience".
 */
const LAYOUT = {
  p01: { bandRows: 5 },
  p02: { bandRows: 5 },
  p03: { bandRows: 6 },
};

/** The page colour. Artwork must stay clear of it - see the check below. */
const PAPER = '#F7F4ED';
const lumOf = (hex) => {
  const n = parseInt(hex.slice(1), 16);
  return (0.2126 * ((n >> 16) & 255) + 0.7152 * ((n >> 8) & 255) + 0.0722 * (n & 255)) / 255;
};
const PAPER_LUM = lumOf(PAPER);
/**
 * A colour this close to the paper cannot read as "filled in" once it is solved, whatever the
 * tint formula does with it - the block, its backing and the page are all the same value. Found
 * by looking at the running game: the mushroom stem and the cat's chest solved into nothing.
 */
const MIN_PAPER_CONTRAST = 0.1;

let failures = 0;
const pictures = [];

for (const art of SAFE_ART) {
  for (const [ch, hex] of Object.entries(art.palette)) {
    const delta = Math.abs(lumOf(hex) - PAPER_LUM);
    if (delta < MIN_PAPER_CONTRAST) {
      console.error(`FAIL ${art.id}: colour ${ch} ${hex} is ${delta.toFixed(3)} from the paper`);
      console.error(`     (need >= ${MIN_PAPER_CONTRAST}). A solved region in it looks unsolved.`);
      failures++;
    }
  }

  const grid = gridFromArt(art);
  const layout = LAYOUT[art.id] ?? { bandRows: 6 };
  const seed = art.id.charCodeAt(1) * 131 + art.id.charCodeAt(2);
  const { sectionOf, sectionRegions, meta, cellSets } = buildPuzzle(grid, layout, seed);

  // Every cell of every section must end up inside exactly one region, or that section can
  // never be finished and the player is trapped. Checked here as well as in loader.ts.
  sectionRegions.forEach((regions, index) => {
    const owned = new Set();
    for (const r of regions) {
      for (let y = r.y; y < r.y + r.h; y++) {
        for (let x = r.x; x < r.x + r.w; x++) {
          const key = `${x},${y}`;
          if (owned.has(key)) {
            console.error(`FATAL ${art.id}/s${index}: cell ${key} covered twice`);
            failures++;
          }
          owned.add(key);
          if (sectionOf[y * grid.w + x] !== index) {
            console.error(`FATAL ${art.id}/s${index}: region leaks into another section at ${key}`);
            failures++;
          }
        }
      }
    }
    for (const [x, y] of cellSets[index]) {
      if (!owned.has(`${x},${y}`)) {
        console.error(`FATAL ${art.id}/s${index}: cell ${x},${y} uncovered`);
        failures++;
      }
    }
  });

  const json = emitPictureJson({
    id: art.id,
    name: art.name,
    grid,
    sectionRegions,
    source: 'clean-room artwork authored in scripts/art-safe.mjs',
  });

  const stats = statsFor(json);
  console.log(
    `${art.id} "${art.name}" ${grid.w}x${grid.h}  ` +
      `sections=${stats.sections} regions=${stats.regions} avgArea=${stats.avgArea} ` +
      `1-cell=${stats.ones} over15=${stats.over15}`,
  );
  console.log(`   sizes  ${meta.map((m) => `${m.w}x${m.h}(${m.cells})`).join(' ')}`);
  console.log(
    `   areas  ${Object.keys(stats.hist)
      .sort((a, b) => +a - +b)
      .map((k) => `${k}:${stats.hist[k]}`)
      .join(' ')}`,
  );

  if (stats.ones > 0) {
    console.error(`FAIL ${art.id}: ${stats.ones} one-cell region(s) survived cleanup - adjust the`);
    console.error('     artwork so every colour blob is at least 2 cells thick in both directions.');
    failures++;
  }
  sectionRegions.forEach((regions, i) => {
    if (regions.length < 3) {
      console.error(`FAIL ${art.id}/s${i}: only ${regions.length} region(s). A section is a funnel`);
      console.error('     step, and a level solvable in one or two drags measures nothing.');
      failures++;
    }
  });
  if (stats.over15 > 0) {
    console.error(`FAIL ${art.id}: ${stats.over15} region(s) over 15 cells`);
    failures++;
  }
  meta.forEach((m, i) => {
    if (m.w > 9) {
      console.error(`FAIL ${art.id}/s${i}: section ${m.w} cells wide (cap 9)`);
      failures++;
    }
    if (m.h > 12) {
      console.error(`FAIL ${art.id}/s${i}: section ${m.h} cells tall (cap 12)`);
      failures++;
    }
    // Below the 5-cell minimum is a warning, not a failure: it only happens where the
    // silhouette itself is narrow (a balloon basket, a cat's ear), and a smaller section just
    // means the camera frames it larger - which helps dragging rather than hurting it. The
    // maximum is the one that actually matters, because it is what shrinks cells below 40 CSS px.
    if (m.w < 5 || m.h < 5) {
      console.log(`   note s${i}: ${m.w}x${m.h} is under the 5-cell guideline (narrow silhouette)`);
    }
  });

  pictures.push(json);
}

if (failures > 0) {
  console.error(`\n${failures} content failure(s). Nothing written.`);
  process.exit(1);
}

mkdirSync(outDir, { recursive: true });
for (const picture of pictures) {
  writeFileSync(resolve(outDir, `${picture.id}.json`), JSON.stringify(picture), 'utf8');
}
writeFileSync(
  resolve(outDir, 'catalog.json'),
  JSON.stringify({ version: 1, content_set: 'safe', pictures: pictures.map((p) => p.id) }),
  'utf8',
);

const totalSections = pictures.reduce((a, p) => a + p.sections.length, 0);
console.log(`\nwrote ${pictures.length} pictures, ${totalSections} sections -> public/puzzles/`);
if (totalSections < 7) {
  console.error(`WARNING: only ${totalSections} sections. The funnel needs >= 7 playable levels.`);
  process.exit(1);
}
