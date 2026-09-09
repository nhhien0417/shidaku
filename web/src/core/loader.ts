/**
 * Turns authored JSON into the runtime Picture, and validates it hard on the way through.
 *
 * Two classes of problem, deliberately handled differently:
 *  - FATAL   the puzzle is unplayable (a hole a region never covers = the player is stuck
 *            forever; overlapping regions = ambiguous ownership). Throws. Never ships.
 *  - WARNING the puzzle plays but breaks a content rule (a 1-cell region, area > 15, a
 *            region spanning two colors, a section wider than 9). Reported, not fatal -
 *            a warning must never stop a live test from running.
 *
 * This mirrors the three checks Puzzle Designer's Verify runs in Unity before saving, so a
 * puzzle that was exported cleanly can never fail here - and one that was hand-edited will.
 */

import type { CatalogJson, Picture, PictureJson, Rect, Region, Section } from './types';

const B64 = 'ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-_';

const MAX_REGION_AREA = 15;
const MAX_SECTION_WIDTH = 9;

export class PuzzleFatalError extends Error {}

export interface LoadResult {
  picture: Picture;
  warnings: string[];
}

/** Decodes one base64url char to its palette index. */
function decodeChar(ch: string, at: number): number {
  const idx = B64.indexOf(ch);
  if (idx < 0) throw new PuzzleFatalError(`pixels: bad char ${JSON.stringify(ch)} at ${at}`);
  return idx;
}

export function buildPicture(json: PictureJson): LoadResult {
  const warnings: string[] = [];
  const { w, h } = json;

  if (!Number.isInteger(w) || !Number.isInteger(h) || w <= 0 || h <= 0) {
    throw new PuzzleFatalError(`${json.id}: bad dimensions ${w}x${h}`);
  }
  if (json.pixels.length !== w * h) {
    throw new PuzzleFatalError(`${json.id}: pixels length ${json.pixels.length}, expected ${w * h}`);
  }
  if (json.palette.length < 1) throw new PuzzleFatalError(`${json.id}: empty palette`);
  if (json.palette.length > B64.length) {
    throw new PuzzleFatalError(`${json.id}: palette too large (${json.palette.length} > ${B64.length})`);
  }

  // --- pixels: Unity order (row 0 = bottom) -> canvas order (row 0 = top). The one flip.
  const pixels = new Uint8Array(w * h);
  for (let uy = 0; uy < h; uy++) {
    const cy = h - 1 - uy;
    for (let x = 0; x < w; x++) {
      const src = uy * w + x;
      const idx = decodeChar(json.pixels.charAt(src), src);
      if (idx >= json.palette.length) {
        throw new PuzzleFatalError(`${json.id}: palette index ${idx} out of range at ${src}`);
      }
      pixels[cy * w + x] = idx;
    }
  }

  // --- sections + regions, flipped into canvas space
  const sectionOf = new Int16Array(w * h).fill(-1);
  const sections: Section[] = [];
  let totalRegions = 0;

  json.sections.forEach((sectionJson, sectionIndex) => {
    if (sectionJson.regions.length === 0) {
      throw new PuzzleFatalError(`${json.id}: section ${sectionIndex} has no regions`);
    }

    const regions: Region[] = [];
    let minX = w;
    let minY = h;
    let maxX = -1;
    let maxY = -1;

    for (const rj of sectionJson.regions) {
      const [ux, uy, rw, rh] = rj.r;
      if (rw <= 0 || rh <= 0) {
        throw new PuzzleFatalError(`${json.id}/s${sectionIndex}: non-positive rect ${rj.r.join(',')}`);
      }
      const rect: Rect = { x: ux, y: h - uy - rh, w: rw, h: rh };
      const clue = { x: rj.c[0], y: h - 1 - rj.c[1] };

      if (rect.x < 0 || rect.y < 0 || rect.x + rect.w > w || rect.y + rect.h > h) {
        throw new PuzzleFatalError(`${json.id}/s${sectionIndex}: rect out of bounds ${rj.r.join(',')}`);
      }
      if (
        clue.x < rect.x ||
        clue.x >= rect.x + rect.w ||
        clue.y < rect.y ||
        clue.y >= rect.y + rect.h
      ) {
        throw new PuzzleFatalError(
          `${json.id}/s${sectionIndex}: clue ${rj.c.join(',')} outside its rect ${rj.r.join(',')}`,
        );
      }

      const area = rect.w * rect.h;
      if (area > MAX_REGION_AREA) warnings.push(`${json.id}/s${sectionIndex}: region area ${area} > ${MAX_REGION_AREA}`);
      if (area === 1) warnings.push(`${json.id}/s${sectionIndex}: 1-cell region at ${rect.x},${rect.y}`);

      // Ownership + monochrome, in one sweep.
      let firstColor = -1;
      for (let y = rect.y; y < rect.y + rect.h; y++) {
        for (let x = rect.x; x < rect.x + rect.w; x++) {
          const flat = y * w + x;
          if (sectionOf[flat] !== -1) {
            throw new PuzzleFatalError(
              `${json.id}: cell ${x},${y} claimed by section ${sectionOf[flat]} and ${sectionIndex}`,
            );
          }
          if (pixels[flat] === 0) {
            throw new PuzzleFatalError(`${json.id}/s${sectionIndex}: region covers transparent cell ${x},${y}`);
          }
          sectionOf[flat] = sectionIndex;
          if (firstColor === -1) firstColor = pixels[flat];
          else if (pixels[flat] !== firstColor) {
            warnings.push(`${json.id}/s${sectionIndex}: region at ${rect.x},${rect.y} spans two colors`);
          }
        }
      }

      if (rect.x < minX) minX = rect.x;
      if (rect.y < minY) minY = rect.y;
      if (rect.x + rect.w - 1 > maxX) maxX = rect.x + rect.w - 1;
      if (rect.y + rect.h - 1 > maxY) maxY = rect.y + rect.h - 1;

      regions.push({ rect, clue, area, solved: false, solvedBy: null });
      totalRegions++;
    }

    const bounds: Rect = { x: minX, y: minY, w: maxX - minX + 1, h: maxY - minY + 1 };
    if (bounds.w > MAX_SECTION_WIDTH) {
      warnings.push(`${json.id}/s${sectionIndex}: section is ${bounds.w} cells wide (> ${MAX_SECTION_WIDTH})`);
    }

    sections.push({ index: sectionIndex, regions, bounds, unsolvedCount: regions.length });
  });

  // --- coverage: every artwork pixel must belong to exactly one section. A miss here means
  // a section can never be completed and the player is stuck forever, so it is fatal.
  for (let i = 0; i < pixels.length; i++) {
    if (pixels[i] !== 0 && sectionOf[i] === -1) {
      const x = i % w;
      const y = Math.floor(i / w);
      throw new PuzzleFatalError(`${json.id}: artwork cell ${x},${y} is not covered by any region`);
    }
  }

  return {
    picture: {
      id: json.id,
      name: json.name,
      w,
      h,
      palette: json.palette,
      pixels,
      sectionOf,
      solved: new Uint8Array(w * h),
      sections,
      totalRegions,
      solvedRegions: 0,
    },
    warnings,
  };
}

// ---------------------------------------------------------------- fetching

async function fetchJson<T>(url: string): Promise<T> {
  const res = await fetch(url, { cache: 'default' });
  if (!res.ok) throw new PuzzleFatalError(`fetch ${url} failed: ${res.status}`);
  return (await res.json()) as T;
}

export function loadCatalog(base = 'puzzles'): Promise<CatalogJson> {
  return fetchJson<CatalogJson>(`${base}/catalog.json`);
}

export async function loadPicture(id: string, base = 'puzzles'): Promise<LoadResult> {
  const json = await fetchJson<PictureJson>(`${base}/${id}.json`);
  return buildPicture(json);
}
