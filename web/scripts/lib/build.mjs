/**
 * Offline content pipeline: artwork -> sections -> regions -> the JSON the web build eats.
 *
 * WHY THIS EXISTS AT ALL. The plan says content is authored in Unity (Template Designer +
 * Puzzle Designer + Verify) and shipped out through ShidakuWebExporter.cs, and that is still
 * the primary path for anything that came from Unity. But the four Unity templates cannot be
 * used publicly (third-party IP), so the publishable set had to be drawn from scratch - and
 * drawing it needed a partitioner. This is that partitioner: the same job PixelShikakuGenerator
 * does inside Unity, ported to Node so clean-room art can be authored without the editor.
 *
 * The rules it enforces are the spec's, not new ones:
 *   - a section is 5..9 cells wide (hard cap 9) and 5..12 tall, because a portrait phone
 *     cannot show a wider board with cells big enough to drag accurately (>= 40 CSS px);
 *   - a region is at most 15 cells, or the clue stops carrying deductive meaning;
 *   - a 1-cell region is the thing to avoid above all else - it is a move that costs a tap
 *     and teaches nothing;
 *   - a region NEVER spans two colours. This is why the picture's shape decides the puzzle's
 *     shape, and not the other way round.
 */

const MAX_REGION_AREA = 15;
const MAX_SECTION_WIDTH = 8;
/** A section is a funnel step; fewer than three moves does not deserve one. */
const MIN_SECTION_REGIONS = 3;
const B64 = 'ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-_';

/** Deterministic PRNG - content must be byte-identical on every machine and every run. */
export function mulberry32(seed) {
  let a = seed >>> 0;
  return () => {
    a = (a + 0x6d2b79f5) >>> 0;
    let t = Math.imul(a ^ (a >>> 15), 1 | a);
    t = (t + Math.imul(t ^ (t >>> 7), 61 | t)) ^ t;
    return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
  };
}

/**
 * Char rows (top-to-bottom) -> a canvas-order grid. Palette index 0 is reserved for
 * "not part of the artwork", matching Template.cs's alpha==0 convention.
 */
export function gridFromArt(art) {
  const h = art.rows.length;
  const w = art.rows[0].length;
  for (const [i, row] of art.rows.entries()) {
    if (row.length !== w) {
      throw new Error(`${art.id}: row ${i} is ${row.length} chars, expected ${w}`);
    }
  }

  const chars = [];
  for (const row of art.rows) {
    for (const ch of row) {
      if (ch !== '.' && !chars.includes(ch)) chars.push(ch);
    }
  }
  for (const ch of chars) {
    if (!art.palette[ch]) throw new Error(`${art.id}: char ${ch} has no palette entry`);
  }

  const palette = ['#00000000', ...chars.map((ch) => art.palette[ch])];
  const px = new Uint8Array(w * h);
  for (let y = 0; y < h; y++) {
    for (let x = 0; x < w; x++) {
      const ch = art.rows[y][x];
      px[y * w + x] = ch === '.' ? 0 : chars.indexOf(ch) + 1;
    }
  }
  return { w, h, palette, px };
}

// ---------------------------------------------------------------- sections

/**
 * Vertical strips (never wider than 9), each cut into horizontal bands - exactly the layout
 * the spec settled on ("dai doc roi cat ngang, rong toi da 9 o"). Sections are then ordered
 * BOTTOM-UP, left to right, so the picture reveals itself upward like the reference game
 * does, and so the first sections the player meets are the small ones at the base.
 */
export function assignSections(grid, { bandRows = 6, minSectionCells = 6 } = {}) {
  const { w, h, px } = grid;

  let minX = w;
  let maxX = -1;
  let minY = h;
  let maxY = -1;
  for (let y = 0; y < h; y++) {
    for (let x = 0; x < w; x++) {
      if (px[y * w + x] === 0) continue;
      if (x < minX) minX = x;
      if (x > maxX) maxX = x;
      if (y < minY) minY = y;
      if (y > maxY) maxY = y;
    }
  }
  if (maxX < minX) throw new Error('artwork has no visible pixels');

  const artW = maxX - minX + 1;
  const stripCount = Math.ceil(artW / MAX_SECTION_WIDTH);
  const stripW = Math.ceil(artW / stripCount);
  const strips = [];
  for (let i = 0; i < stripCount; i++) {
    const x0 = minX + i * stripW;
    strips.push([x0, Math.min(maxX, x0 + stripW - 1)]);
  }

  // Bands walk from the BOTTOM row upward, so the bottom band is always a full-height one
  // and any short leftover lands at the top (where it gets merged into its neighbour).
  const bands = [];
  for (let y1 = maxY; y1 >= minY; y1 -= bandRows) {
    bands.push([Math.max(minY, y1 - bandRows + 1), y1]);
  }
  if (bands.length > 1) {
    const top = bands[bands.length - 1];
    if (top[1] - top[0] + 1 < 3) {
      bands.pop();
      bands[bands.length - 1][0] = top[0];
    }
  }

  // Collect candidate section cell sets, in reveal order: bottom band first, left to right.
  const candidates = [];
  for (const [y0, y1] of bands) {
    for (const [x0, x1] of strips) {
      const cells = [];
      for (let y = y0; y <= y1; y++) {
        for (let x = x0; x <= x1; x++) {
          if (px[y * w + x] !== 0) cells.push([x, y]);
        }
      }
      if (cells.length > 0) candidates.push({ cells, x0, x1, y0, y1 });
    }
  }

  // A sliver of a section is worse than a slightly oversized one: it costs a whole camera
  // move and a celebration for three taps. Fold it into the nearest kept section that
  // shares columns.
  const kept = [];
  for (const cand of candidates) {
    if (cand.cells.length >= minSectionCells) {
      kept.push(cand);
      continue;
    }
    let host = null;
    for (let i = kept.length - 1; i >= 0; i--) {
      if (kept[i].x0 === cand.x0 && kept[i].x1 === cand.x1) {
        host = kept[i];
        break;
      }
    }
    if (!host) host = kept[kept.length - 1];
    if (host) {
      host.cells.push(...cand.cells);
      host.y0 = Math.min(host.y0, cand.y0);
      host.y1 = Math.max(host.y1, cand.y1);
    } else {
      kept.push(cand);
    }
  }

  const sectionOf = new Int16Array(w * h).fill(-1);
  kept.forEach((section, index) => {
    for (const [x, y] of section.cells) sectionOf[y * w + x] = index;
  });

  return { sectionOf, sections: kept };
}

// ---------------------------------------------------------------- region tiling

function canPlace(grid, sectionOf, covered, sIndex, color, rect) {
  const { w, px } = grid;
  for (let y = rect.y; y < rect.y + rect.h; y++) {
    for (let x = rect.x; x < rect.x + rect.w; x++) {
      const flat = y * w + x;
      if (sectionOf[flat] !== sIndex) return false;
      if (covered[flat]) return false;
      if (px[flat] !== color) return false;
    }
  }
  return true;
}

/**
 * One tiling attempt. Seeds from the top-left uncovered cell (so the rect's own top-left
 * corner is always the seed and the tiling stays valid), enumerates every legal width, and
 * picks among them with a random bias - which is what makes repeated attempts differ.
 */
function tryTiling(grid, sectionOf, sIndex, cells, rnd) {
  const { w, px } = grid;
  const covered = new Uint8Array(grid.w * grid.h);
  const remaining = cells.slice().sort((a, b) => a[1] - b[1] || a[0] - b[0]);
  const regions = [];

  for (const [sx, sy] of remaining) {
    const seedFlat = sy * w + sx;
    if (covered[seedFlat]) continue;
    const color = px[seedFlat];

    let maxW = 0;
    while (
      maxW < MAX_SECTION_WIDTH &&
      canPlace(grid, sectionOf, covered, sIndex, color, { x: sx, y: sy, w: maxW + 1, h: 1 })
    ) {
      maxW++;
    }

    const options = [];
    for (let rw = 1; rw <= maxW; rw++) {
      let rh = 1;
      while (
        rw * (rh + 1) <= MAX_REGION_AREA &&
        canPlace(grid, sectionOf, covered, sIndex, color, { x: sx, y: sy, w: rw, h: rh + 1 })
      ) {
        rh++;
      }
      if (rw * rh <= MAX_REGION_AREA) options.push({ w: rw, h: rh });
    }
    if (options.length === 0) options.push({ w: 1, h: 1 });

    // Weight by area, so big satisfying regions are the default and the odd small one still
    // happens. A pure argmax(area) tiling looks mechanical and always leaves the same crumbs.
    //
    // The exponent is deliberately mild (1.15, not 2+): maximising area produces sections
    // solvable in two drags, which starves the reward loop. The spec wants a rewarded move
    // every 3-8 seconds, so mid-sized regions (4-9 cells) are what we actually want most of.
    const weights = options.map((o) => {
      const area = o.w * o.h;
      const bulk = Math.pow(area, 1.15);
      const tooBig = area > 10 ? 0.45 : 1; // 12+ cell regions eat too much picture per move
      return bulk * tooBig * (0.75 + rnd() * 0.5);
    });
    let total = weights.reduce((a, b) => a + b, 0);
    let roll = rnd() * total;
    let pick = options[options.length - 1];
    for (let i = 0; i < options.length; i++) {
      roll -= weights[i];
      if (roll <= 0) {
        pick = options[i];
        break;
      }
    }

    const rect = { x: sx, y: sy, w: pick.w, h: pick.h };
    for (let y = rect.y; y < rect.y + rect.h; y++) {
      for (let x = rect.x; x < rect.x + rect.w; x++) covered[y * w + x] = 1;
    }
    regions.push(rect);
  }

  return regions;
}

function scoreTiling(regions) {
  let score = 0;
  for (const r of regions) {
    const area = r.w * r.h;
    // A 1-cell region is disqualifying, not merely bad.
    if (area === 1) score += 1000;
    if (area === 2) score += 5;
    if (area > 10) score += (area - 10) * 3; // a huge region reveals too much for one move
    const long = Math.max(r.w, r.h);
    const short = Math.min(r.w, r.h);
    if (long / short >= 5) score += 3; // 1x5+ strips read as noise in the finished picture
  }
  return score;
}

/**
 * Best-of-N randomized tiling. The search space for a 9x12 section is small enough that a
 * few hundred attempts reliably finds a tiling with zero 1-cell regions whenever the artwork
 * admits one - which is cheaper and far more robust than trying to repair a greedy tiling.
 */
export function tileSection(grid, sectionOf, sIndex, cells, attempts = 800, seed = 1) {
  const rnd = mulberry32(seed * 2654435761 + sIndex * 40503);
  let best = null;
  let bestScore = Infinity;
  for (let i = 0; i < attempts; i++) {
    const regions = tryTiling(grid, sectionOf, sIndex, cells, rnd);
    const score = scoreTiling(regions);
    if (score < bestScore) {
      bestScore = score;
      best = regions;
      if (score === 0) break; // clean tiling: no orphans, no giants, no slivers
    }
  }
  return best ?? [];
}

// ---------------------------------------------------------------- sliver cleanup

export function cellsBySection(grid, sectionOf, count) {
  const out = Array.from({ length: count }, () => []);
  for (let y = 0; y < grid.h; y++) {
    for (let x = 0; x < grid.w; x++) {
      const s = sectionOf[y * grid.w + x];
      if (s >= 0) out[s].push([x, y]);
    }
  }
  return out;
}

function bboxOf(cells, extra) {
  const all = extra ? cells.concat([extra]) : cells;
  let x0 = Infinity;
  let x1 = -Infinity;
  let y0 = Infinity;
  let y1 = -Infinity;
  for (const [x, y] of all) {
    if (x < x0) x0 = x;
    if (x > x1) x1 = x;
    if (y < y0) y0 = y;
    if (y > y1) y1 = y;
  }
  return { x0, x1, y0, y1, w: x1 - x0 + 1, h: y1 - y0 + 1 };
}

function tileAll(grid, sectionOf, cellSets, seed, attempts) {
  return cellSets.map((cells, index) => tileSection(grid, sectionOf, index, cells, attempts, seed));
}

/**
 * Which section should absorb section `thin`: the neighbour it shares the most edge with,
 * provided the merged shape still fits the 8x12 framing envelope. Sharing the most boundary
 * keeps the merged section a coherent blob rather than a barbell.
 */
function bestMergeTarget(grid, sectionOf, cellSets, thin) {
  const contact = new Map();
  for (const [x, y] of cellSets[thin]) {
    for (const [nx, ny] of [
      [x + 1, y],
      [x - 1, y],
      [x, y + 1],
      [x, y - 1],
    ]) {
      if (nx < 0 || ny < 0 || nx >= grid.w || ny >= grid.h) continue;
      const other = sectionOf[ny * grid.w + nx];
      if (other < 0 || other === thin) continue;
      contact.set(other, (contact.get(other) ?? 0) + 1);
    }
  }

  let best = -1;
  let bestContact = 0;
  for (const [candidate, shared] of contact) {
    const box = bboxOf(cellSets[candidate].concat(cellSets[thin]));
    if (box.w > MAX_SECTION_WIDTH || box.h > 12) continue;
    if (shared > bestContact) {
      bestContact = shared;
      best = candidate;
    }
  }
  return best;
}

/** Folds `thin` into `target` and renumbers, preserving the bottom-up reveal order. */
function mergeSections(grid, sectionOf, count, thin, target) {
  const next = Int16Array.from(sectionOf);
  for (let i = 0; i < next.length; i++) {
    if (next[i] === thin) next[i] = target;
  }
  // Close the gap left by the removed index.
  for (let i = 0; i < next.length; i++) {
    if (next[i] > thin) next[i] -= 1;
  }
  return { sectionOf: next, count: count - 1 };
}

/**
 * The final cleanup pass the spec calls for ("mot buoc don cuoi de gom not cac vung 1 o con sot").
 *
 * 1-cell regions here are almost never the artwork's fault - they appear where a strip/band
 * boundary slices a colour blob and leaves a single orphan cell behind. Sections are allowed
 * to be organic shapes, so the fix is to hand that cell to the neighbouring section that
 * already owns the same colour next to it, then re-tile and look again.
 *
 * Two strategies, tried in order:
 *   1. push the orphan out into a neighbouring section of the same colour;
 *   2. failing that, pull a same-coloured neighbour IN, so the orphan becomes a 2-cell region.
 */
export function buildPuzzle(grid, { bandRows = 6, minSectionCells = 6 } = {}, seed = 1, attempts = 800) {
  const assigned = assignSections(grid, { bandRows, minSectionCells });
  let sectionOf = assigned.sectionOf;
  let count = assigned.sections.length;
  let cellSets = cellsBySection(grid, sectionOf, count);
  let regions = tileAll(grid, sectionOf, cellSets, seed, attempts);

  /**
   * A section IS a level: it gets its own camera move, its own celebration and its own step
   * in the funnel. One or two regions is not a level, it is a tap - and it would spend a
   * whole funnel step on something that measures nothing. Merge those into a neighbour.
   *
   * This has to run after tiling, because region COUNT (not cell count) is what is wrong:
   * a 15-cell section that happens to tile as one clean rectangle is still a single move.
   */
  for (let pass = 0; pass < 8; pass++) {
    const thin = regions.findIndex((list) => list.length < MIN_SECTION_REGIONS);
    if (thin < 0) break;
    const target = bestMergeTarget(grid, sectionOf, cellSets, thin);
    if (target < 0) break; // nothing it can legally join; leave it and report it
    const merged = mergeSections(grid, sectionOf, count, thin, target);
    sectionOf = merged.sectionOf;
    count = merged.count;
    cellSets = cellsBySection(grid, sectionOf, count);
    regions = tileAll(grid, sectionOf, cellSets, seed + 17 + pass, attempts);
  }

  const MAX_PASSES = 10;
  for (let pass = 0; pass < MAX_PASSES; pass++) {
    const orphans = [];
    regions.forEach((list, sIndex) => {
      for (const r of list) {
        if (r.w === 1 && r.h === 1) orphans.push({ x: r.x, y: r.y, s: sIndex });
      }
    });
    if (orphans.length === 0) break;

    let moved = 0;
    for (const orphan of orphans) {
      const color = grid.px[orphan.y * grid.w + orphan.x];
      const neighbours = [
        [orphan.x + 1, orphan.y],
        [orphan.x - 1, orphan.y],
        [orphan.x, orphan.y + 1],
        [orphan.x, orphan.y - 1],
      ].filter(([nx, ny]) => nx >= 0 && ny >= 0 && nx < grid.w && ny < grid.h);

      // Strategy 1: give the orphan away.
      let done = false;
      for (const [nx, ny] of neighbours) {
        const nFlat = ny * grid.w + nx;
        const target = sectionOf[nFlat];
        if (target < 0 || target === orphan.s) continue;
        if (grid.px[nFlat] !== color) continue;
        const box = bboxOf(cellSets[target], [orphan.x, orphan.y]);
        if (box.w > MAX_SECTION_WIDTH || box.h > 12) continue;
        if (cellSets[orphan.s].length <= 4) continue; // do not gut a small section
        sectionOf[orphan.y * grid.w + orphan.x] = target;
        moved++;
        done = true;
        break;
      }
      if (done) continue;

      // Strategy 2: adopt a same-coloured neighbour instead.
      for (const [nx, ny] of neighbours) {
        const nFlat = ny * grid.w + nx;
        const donor = sectionOf[nFlat];
        if (donor < 0 || donor === orphan.s) continue;
        if (grid.px[nFlat] !== color) continue;
        const box = bboxOf(cellSets[orphan.s], [nx, ny]);
        if (box.w > MAX_SECTION_WIDTH || box.h > 12) continue;
        if (cellSets[donor].length <= 4) continue;
        sectionOf[nFlat] = orphan.s;
        moved++;
        break;
      }
    }

    if (moved === 0) break;
    cellSets = cellsBySection(grid, sectionOf, count);
    regions = tileAll(grid, sectionOf, cellSets, seed + pass + 1, attempts);
  }

  cellSets = cellsBySection(grid, sectionOf, count);
  const meta = cellSets.map((cells) => {
    const box = bboxOf(cells);
    return { cells: cells.length, w: box.w, h: box.h, x0: box.x0, y0: box.y0, x1: box.x1, y1: box.y1 };
  });

  return { sectionOf, sectionRegions: regions, meta, cellSets };
}

/**
 * Clue placement. Deterministic but varied: where the number sits inside its rectangle
 * changes how strong a hint it is (a corner constrains more than a centre), and the spec
 * lists that as one of the three legitimate sources of difficulty.
 */
export function placeClue(rect) {
  const area = rect.w * rect.h;
  const index = (rect.x * 7 + rect.y * 13 + area * 3) % area;
  return { x: rect.x + (index % rect.w), y: rect.y + Math.floor(index / rect.w) };
}

// ---------------------------------------------------------------- emit

/**
 * Canvas-space model -> on-disk JSON, flipping back to Unity's y-up convention so the JSON
 * schema stays identical whether it was written by this script or by ShidakuWebExporter.cs.
 * loader.ts flips it once on the way in; nothing else ever touches a y.
 */
export function emitPictureJson({ id, name, grid, sectionRegions, source }) {
  const { w, h, palette, px } = grid;

  let pixels = '';
  for (let uy = 0; uy < h; uy++) {
    const cy = h - 1 - uy;
    for (let x = 0; x < w; x++) pixels += B64[px[cy * w + x]];
  }

  const sections = sectionRegions.map((regions) => ({
    regions: regions.map((rect) => {
      const clue = placeClue(rect);
      return {
        r: [rect.x, h - rect.y - rect.h, rect.w, rect.h],
        c: [clue.x, h - 1 - clue.y],
      };
    }),
  }));

  return {
    id,
    name,
    w,
    h,
    palette,
    pixels,
    sections,
    source: source ?? 'authored',
    exported_at: new Date().toISOString().slice(0, 10),
  };
}

export function statsFor(picture) {
  const areas = [];
  for (const s of picture.sections) for (const r of s.regions) areas.push(r.r[2] * r.r[3]);
  const hist = {};
  for (const a of areas) hist[a] = (hist[a] ?? 0) + 1;
  return {
    sections: picture.sections.length,
    regions: areas.length,
    ones: areas.filter((a) => a === 1).length,
    over15: areas.filter((a) => a > MAX_REGION_AREA).length,
    avgArea: +(areas.reduce((a, b) => a + b, 0) / areas.length).toFixed(2),
    hist,
  };
}

export { MAX_REGION_AREA, MAX_SECTION_WIDTH, B64 };

/*
 * NOTE ON MAX_SECTION_WIDTH = 8 (the spec's guideline says up to 9).
 *
 * The binding constraint is not the grid, it is the thumb. A cell has to render at >= 40 CSS
 * px to be draggable accurately, and the camera frames a section with a 14% margin:
 *     360 px viewport / 1.14 / 9 cells = 35 px   -> too small on a 360px-wide phone
 *     360 px viewport / 1.14 / 8 cells = 39.5 px -> at target once the camera trims padding
 * So the content pipeline caps at 8. The loader still only *warns* above 9, matching the
 * spec, because a hand-authored Unity puzzle is allowed to push it.
 */
