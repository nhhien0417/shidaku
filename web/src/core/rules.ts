/**
 * THE RULES. Pure functions, zero imports beyond types, fully unit-tested.
 *
 * Everything else in this prototype (render, audio, analytics) can misbehave and the
 * measurement still works. If this file is wrong, every number the test produces is
 * meaningless. Treat it accordingly: no side effects, no DOM, no randomness, no time.
 */

import type { BoardQuery, CellPos, Rect, Region, Section } from './types';

/** Normalises two dragged corners into a rectangle. Inclusive of both corners. */
export function makeRect(a: CellPos, b: CellPos): Rect {
  const minX = Math.min(a.x, b.x);
  const minY = Math.min(a.y, b.y);
  const maxX = Math.max(a.x, b.x);
  const maxY = Math.max(a.y, b.y);
  return { x: minX, y: minY, w: maxX - minX + 1, h: maxY - minY + 1 };
}

export function rectArea(rect: Rect): number {
  return rect.w * rect.h;
}

export function rectEquals(a: Rect, b: Rect): boolean {
  return a.x === b.x && a.y === b.y && a.w === b.w && a.h === b.h;
}

export function rectContains(rect: Rect, x: number, y: number): boolean {
  return x >= rect.x && x < rect.x + rect.w && y >= rect.y && y < rect.y + rect.h;
}

/**
 * Can this rectangle be selected at all? Every cell must belong to the section being played
 * and must not already be solved. This is the rule that makes the two boundaries of the
 * board (solved cells, other sections/holes) physically impassable rather than merely wrong.
 */
export function isRectSelectable(q: BoardQuery, sectionIndex: number, rect: Rect): boolean {
  if (rect.w <= 0 || rect.h <= 0) return false;
  if (rect.x < 0 || rect.y < 0 || rect.x + rect.w > q.w || rect.y + rect.h > q.h) return false;
  for (let y = rect.y; y < rect.y + rect.h; y++) {
    for (let x = rect.x; x < rect.x + rect.w; x++) {
      if (q.sectionAt(x, y) !== sectionIndex) return false;
      if (q.isSolved(x, y)) return false;
    }
  }
  return true;
}

/**
 * Where the selection is allowed to end up, given where the finger actually is.
 *
 * The demo (Board.cs UpdateDragPreview) simply refuses any candidate whose rect is not
 * selectable, freezing the whole selection. This does the same thing when the candidate is
 * valid, but when it is not, it grows as far as it legally can on each axis instead - so
 * dragging past a wall slides the frame ALONG the wall rather than freezing it dead. The
 * spec calls the boundary "something you feel with your hand"; this is that, made literal.
 *
 * Grids are at most 9x12, so the greedy walk is free.
 */
export function clampDragTo(
  q: BoardQuery,
  sectionIndex: number,
  start: CellPos,
  candidate: CellPos,
): CellPos {
  if (isRectSelectable(q, sectionIndex, makeRect(start, candidate))) return candidate;

  let cur: CellPos = { x: start.x, y: start.y };
  // Two passes: expand X, then Y, then X again. The second X pass matters when the Y growth
  // opened up (or closed off) columns the first pass could not reach.
  for (let pass = 0; pass < 2; pass++) {
    cur = { x: walkAxis(q, sectionIndex, start, cur, candidate, 'x'), y: cur.y };
    cur = { x: cur.x, y: walkAxis(q, sectionIndex, start, cur, candidate, 'y') };
  }
  return cur;
}

function walkAxis(
  q: BoardQuery,
  sectionIndex: number,
  start: CellPos,
  cur: CellPos,
  candidate: CellPos,
  axis: 'x' | 'y',
): number {
  const from = start[axis];
  const to = candidate[axis];
  const step = to >= from ? 1 : -1;
  let best = from;
  for (let v = from; step > 0 ? v <= to : v >= to; v += step) {
    const probe: CellPos = axis === 'x' ? { x: v, y: cur.y } : { x: cur.x, y: v };
    if (!isRectSelectable(q, sectionIndex, makeRect(start, probe))) break;
    best = v;
  }
  return best;
}

/**
 * The core difference from classic Shikaku: an EXACT match against the one accepted answer.
 * A rectangle that is perfectly legal Shikaku-wise (right area, one clue inside, no overlap)
 * is still rejected unless it is byte-for-byte the authored region.
 */
export function matchRegion(section: Section, rect: Rect): Region | null {
  for (const region of section.regions) {
    if (region.solved) continue;
    if (rectEquals(region.rect, rect)) return region;
  }
  return null;
}

/**
 * Hint target: the largest unsolved region of the current section. Ties broken topmost then
 * leftmost so the choice is deterministic (a nondeterministic hint would make the hint-usage
 * metric impossible to reason about across sessions).
 */
export function pickHintRegion(section: Section): Region | null {
  let best: Region | null = null;
  for (const region of section.regions) {
    if (region.solved) continue;
    if (best === null) {
      best = region;
      continue;
    }
    if (region.area > best.area) {
      best = region;
    } else if (region.area === best.area) {
      if (region.rect.y < best.rect.y || (region.rect.y === best.rect.y && region.rect.x < best.rect.x)) {
        best = region;
      }
    }
  }
  return best;
}

/**
 * The clue whose area is closest to what the player actually drew, among the unsolved clues
 * the drawn rect touches. Reported with region_rejected so the analysis can tell "drew the
 * right size in the wrong shape" apart from "has no idea what the number means".
 */
export function nearestClueArea(section: Section, rect: Rect): number | null {
  let best: number | null = null;
  let bestDelta = Number.POSITIVE_INFINITY;
  const drawn = rectArea(rect);
  for (const region of section.regions) {
    if (region.solved) continue;
    if (!rectContains(rect, region.clue.x, region.clue.y)) continue;
    const delta = Math.abs(region.area - drawn);
    if (delta < bestDelta) {
      bestDelta = delta;
      best = region.area;
    }
  }
  return best;
}

/** Cells of a rect, top-to-bottom then left-to-right - the one fixed wave order. */
export function rowMajorCells(rect: Rect): CellPos[] {
  const out: CellPos[] = [];
  for (let y = rect.y; y < rect.y + rect.h; y++) {
    for (let x = rect.x; x < rect.x + rect.w; x++) {
      out.push({ x, y });
    }
  }
  return out;
}
