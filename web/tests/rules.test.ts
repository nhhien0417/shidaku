import { describe, expect, it } from 'vitest';
import {
  clampDragTo,
  isRectSelectable,
  makeRect,
  matchRegion,
  nearestClueArea,
  pickHintRegion,
  rowMajorCells,
} from '../src/core/rules';
import type { BoardQuery, Rect, Region, Section } from '../src/core/types';

/**
 * Builds a BoardQuery from an ASCII map, so each test states the board it means.
 *   '.' hole / not part of the artwork
 *   '#' cell of section 0, already solved
 *   '0'..'9' cell of that section index, unsolved
 */
function board(rows: string[]): BoardQuery {
  const h = rows.length;
  const w = rows[0].length;
  return {
    w,
    h,
    sectionAt(x, y) {
      if (x < 0 || y < 0 || x >= w || y >= h) return -1;
      const ch = rows[y][x];
      if (ch === '.') return -1;
      if (ch === '#') return 0;
      return Number(ch);
    },
    isSolved(x, y) {
      if (x < 0 || y < 0 || x >= w || y >= h) return false;
      return rows[y][x] === '#';
    },
  };
}

function region(rect: Rect, clue = { x: rect.x, y: rect.y }): Region {
  return { rect, clue, area: rect.w * rect.h, solved: false, solvedBy: null };
}

function section(regions: Region[]): Section {
  return { index: 0, regions, bounds: { x: 0, y: 0, w: 9, h: 9 }, unsolvedCount: regions.length };
}

describe('makeRect', () => {
  it('normalises corners in any drag direction', () => {
    const expected = { x: 2, y: 1, w: 3, h: 2 };
    expect(makeRect({ x: 2, y: 1 }, { x: 4, y: 2 })).toEqual(expected);
    expect(makeRect({ x: 4, y: 2 }, { x: 2, y: 1 })).toEqual(expected);
    expect(makeRect({ x: 2, y: 2 }, { x: 4, y: 1 })).toEqual(expected);
    expect(makeRect({ x: 4, y: 1 }, { x: 2, y: 2 })).toEqual(expected);
  });

  it('is inclusive of both corners, so a single cell is 1x1', () => {
    expect(makeRect({ x: 3, y: 3 }, { x: 3, y: 3 })).toEqual({ x: 3, y: 3, w: 1, h: 1 });
  });
});

describe('isRectSelectable', () => {
  const q = board([
    '0000.',
    '00##.',
    '0000.',
    '1111.',
  ]);

  it('accepts a rect entirely inside the active section', () => {
    expect(isRectSelectable(q, 0, { x: 0, y: 0, w: 2, h: 3 })).toBe(true);
  });

  it('rejects a rect covering an already-solved cell', () => {
    expect(isRectSelectable(q, 0, { x: 2, y: 0, w: 2, h: 2 })).toBe(false);
  });

  it('rejects a rect covering a hole', () => {
    expect(isRectSelectable(q, 0, { x: 3, y: 0, w: 2, h: 1 })).toBe(false);
  });

  it('rejects a rect reaching into another section', () => {
    expect(isRectSelectable(q, 0, { x: 0, y: 2, w: 2, h: 2 })).toBe(false);
  });

  it('rejects a rect leaving the board', () => {
    expect(isRectSelectable(q, 0, { x: -1, y: 0, w: 2, h: 1 })).toBe(false);
    expect(isRectSelectable(q, 0, { x: 0, y: 3, w: 1, h: 2 })).toBe(false);
  });
});

describe('clampDragTo', () => {
  it('passes the candidate straight through when it is legal', () => {
    const q = board(['0000', '0000']);
    expect(clampDragTo(q, 0, { x: 0, y: 0 }, { x: 3, y: 1 })).toEqual({ x: 3, y: 1 });
  });

  it('stops at the edge instead of crossing a solved cell', () => {
    const q = board(['00#0']);
    expect(clampDragTo(q, 0, { x: 0, y: 0 }, { x: 3, y: 0 })).toEqual({ x: 1, y: 0 });
  });

  it('stops at the edge instead of crossing a hole', () => {
    const q = board(['0.00']);
    expect(clampDragTo(q, 0, { x: 0, y: 0 }, { x: 3, y: 0 })).toEqual({ x: 0, y: 0 });
  });

  it('stops at the section boundary when dragging downward', () => {
    const q = board(['00', '00', '11']);
    expect(clampDragTo(q, 0, { x: 0, y: 0 }, { x: 1, y: 2 })).toEqual({ x: 1, y: 1 });
  });

  it('slides along a wall: blocked on x, still free on y', () => {
    // Column 2 is solved, so x can only reach 1 - but the full height stays available.
    const q = board(['00#', '00#', '00#']);
    expect(clampDragTo(q, 0, { x: 0, y: 0 }, { x: 2, y: 2 })).toEqual({ x: 1, y: 2 });
  });

  it('works dragging up and left, not just down and right', () => {
    const q = board(['#00', '000', '000']);
    expect(clampDragTo(q, 0, { x: 2, y: 2 }, { x: 0, y: 0 })).toEqual({ x: 0, y: 1 });
  });

  it('never returns a cell the drag could not legally reach', () => {
    const q = board(['0000', '00#0', '0000']);
    const got = clampDragTo(q, 0, { x: 0, y: 0 }, { x: 3, y: 2 });
    expect(isRectSelectable(q, 0, makeRect({ x: 0, y: 0 }, got))).toBe(true);
  });
});

describe('matchRegion - the one accepted answer', () => {
  const s = section([region({ x: 0, y: 0, w: 3, h: 1 }), region({ x: 0, y: 1, w: 1, h: 3 })]);

  it('accepts the exact authored rectangle', () => {
    expect(matchRegion(s, { x: 0, y: 0, w: 3, h: 1 })).toBe(s.regions[0]);
  });

  it('REJECTS a rectangle of the right area in the wrong shape', () => {
    // 1x3 at 0,0 has area 3 just like the 3x1 answer, and would be perfectly legal
    // classic Shikaku. Pixel Fill Shikaku still refuses it - this is the core difference.
    expect(matchRegion(s, { x: 0, y: 0, w: 1, h: 3 })).toBe(null);
  });

  it('rejects the right shape in the wrong place', () => {
    expect(matchRegion(s, { x: 1, y: 0, w: 3, h: 1 })).toBe(null);
  });

  it('never matches an already-solved region', () => {
    s.regions[0].solved = true;
    expect(matchRegion(s, { x: 0, y: 0, w: 3, h: 1 })).toBe(null);
    s.regions[0].solved = false;
  });
});

describe('pickHintRegion', () => {
  it('picks the largest unsolved region', () => {
    const s = section([
      region({ x: 0, y: 0, w: 2, h: 1 }),
      region({ x: 0, y: 1, w: 3, h: 2 }),
      region({ x: 4, y: 0, w: 2, h: 2 }),
    ]);
    expect(pickHintRegion(s)).toBe(s.regions[1]);
  });

  it('breaks ties topmost-then-leftmost so the choice is deterministic', () => {
    const s = section([
      region({ x: 4, y: 2, w: 2, h: 2 }),
      region({ x: 1, y: 0, w: 2, h: 2 }),
      region({ x: 6, y: 0, w: 2, h: 2 }),
    ]);
    expect(pickHintRegion(s)).toBe(s.regions[1]);
  });

  it('skips solved regions and returns null when nothing is left', () => {
    const s = section([region({ x: 0, y: 0, w: 2, h: 1 })]);
    s.regions[0].solved = true;
    expect(pickHintRegion(s)).toBe(null);
  });
});

describe('nearestClueArea', () => {
  it('reports the closest-area clue the drawn rect actually covers', () => {
    const s = section([
      region({ x: 0, y: 0, w: 3, h: 1 }, { x: 1, y: 0 }),
      region({ x: 0, y: 1, w: 4, h: 3 }, { x: 2, y: 2 }),
    ]);
    expect(nearestClueArea(s, { x: 0, y: 0, w: 2, h: 1 })).toBe(3);
    expect(nearestClueArea(s, { x: 2, y: 2, w: 1, h: 1 })).toBe(12);
  });

  it('returns null when the rect covers no clue at all', () => {
    const s = section([region({ x: 0, y: 0, w: 3, h: 1 }, { x: 1, y: 0 })]);
    expect(nearestClueArea(s, { x: 5, y: 5, w: 1, h: 1 })).toBe(null);
  });
});

describe('rowMajorCells', () => {
  it('walks top-to-bottom then left-to-right - the one fixed wave order', () => {
    expect(rowMajorCells({ x: 1, y: 1, w: 2, h: 2 })).toEqual([
      { x: 1, y: 1 },
      { x: 2, y: 1 },
      { x: 1, y: 2 },
      { x: 2, y: 2 },
    ]);
  });
});
