/**
 * Shared shapes for the whole prototype.
 *
 * COORDINATE CONVENTION - read this once, then never think about it again.
 *
 * The authored JSON keeps Unity's convention (y grows UPWARD, pixel index 0 = bottom-left
 * row) so ShidakuWebExporter.cs can dump PuzzleData/Template verbatim with no flipping - one
 * less place for an off-by-one to hide. Everything in TypeScript uses CANVAS convention
 * (y grows DOWNWARD, row 0 = top row).
 *
 * The flip happens EXACTLY ONCE, in loader.ts. Anything downstream of the loader is canvas
 * space. If you find yourself flipping a y anywhere else, that is a bug.
 */

/** Integer grid rectangle, canvas space (y down). */
export interface Rect {
  x: number;
  y: number;
  w: number;
  h: number;
}

/** Integer grid cell, canvas space (y down). */
export interface CellPos {
  x: number;
  y: number;
}

export type SolveSource = 'drag' | 'hint';

// ---------------------------------------------------------------- authored JSON (on disk)

export interface RegionJson {
  /** [x, y, w, h] in UNITY space (y up). */
  r: [number, number, number, number];
  /** [x, y] clue cell in UNITY space (y up). */
  c: [number, number];
}

export interface SectionJson {
  regions: RegionJson[];
}

export interface PictureJson {
  id: string;
  name: string;
  w: number;
  h: number;
  /** Hex colors. Index 0 is ALWAYS the transparent/not-part-of-artwork entry. */
  palette: string[];
  /** w*h chars, one base64url char per pixel = palette index. Index 0 = bottom-left (Unity). */
  pixels: string;
  /** Solve order == array order. */
  sections: SectionJson[];
  /** Optional provenance written by the exporter. */
  source?: string;
  exported_at?: string;
}

export interface CatalogJson {
  version: number;
  /** "safe" = clean-room art, publishable. "internal" = dev/QA only, third-party IP. */
  content_set: 'safe' | 'internal';
  /** Picture ids, in play order. */
  pictures: string[];
}

// ---------------------------------------------------------------- runtime model

export interface Region {
  rect: Rect;
  clue: CellPos;
  /** Number of cells == the clue number the player must match. */
  area: number;
  solved: boolean;
  solvedBy: SolveSource | null;
}

export interface Section {
  /** Index within the picture; also the solve order. */
  index: number;
  regions: Region[];
  /** Tight bounding box of this section's playable cells, canvas space. */
  bounds: Rect;
  unsolvedCount: number;
}

export interface Picture {
  id: string;
  name: string;
  w: number;
  h: number;
  /** Packed RGB, 3 bytes per palette entry. Entry 0 is unused (transparent). */
  palette: string[];
  /** Canvas order (row 0 = top), value = palette index. 0 = not part of the artwork. */
  pixels: Uint8Array;
  /** Canvas order. Section index owning that cell, or -1 for holes / non-artwork. */
  sectionOf: Int16Array;
  /** Canvas order. 1 once the cell's region has been solved. */
  solved: Uint8Array;
  sections: Section[];
  totalRegions: number;
  solvedRegions: number;
}

/**
 * The read-only slice of board state that core/rules.ts is allowed to see. Keeping rules.ts
 * behind this interface is what lets it stay a pure, dependency-free, fully unit-tested
 * module - the one part of the codebase that is not allowed to be wrong.
 */
export interface BoardQuery {
  readonly w: number;
  readonly h: number;
  /** Section index owning (x, y), or -1 if the cell is a hole / outside the artwork. */
  sectionAt(x: number, y: number): number;
  /** True once the region containing (x, y) has been solved. */
  isSolved(x: number, y: number): boolean;
}
