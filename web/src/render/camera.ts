/**
 * The camera the player never controls. The game always frames the work itself: tight on the
 * section being played, or pulled back to the whole picture in Overview.
 *
 * Two constraints fight each other here and both matter:
 *   - the section must fit, with a 14% margin (Board.cs BoardFramePadding = 1.14);
 *   - a cell must never render below MIN_CELL_PX, or accurate dragging with a thumb stops
 *     being possible and the test starts measuring finger precision instead of the mechanic.
 * When they conflict, the margin gives way first - losing a little breathing room is far
 * cheaper than losing drag accuracy.
 */

import type { Rect } from '../core/types';
import { lerp, smoothStep } from '../util/tween';

/** World units per board cell. Arbitrary but fixed, matching Board.cs's CellSize = 100. */
export const CELL = 100;

const FRAME_PADDING = 1.14;
/** How far the padding may be squeezed to keep cells thumb-sized. */
const MIN_FRAME_PADDING = 1.04;
const MIN_CELL_PX = 40;
/** A 3-cell tutorial board would otherwise fill the screen with three enormous slabs. */
const MAX_CELL_PX = 96;

export const SECTION_TWEEN_MS = 450;
export const OVERVIEW_TWEEN_MS = 550;

export interface Viewport {
  /** CSS pixels. */
  w: number;
  h: number;
  /** Space reserved for the top HUD and the bottom buttons, in CSS pixels. */
  insetTop: number;
  insetBottom: number;
}

interface CamState {
  scale: number;
  cx: number;
  cy: number;
  anchorX: number;
  anchorY: number;
}

export class Camera {
  private cur: CamState = { scale: 1, cx: 0, cy: 0, anchorX: 0, anchorY: 0 };
  private from: CamState = { scale: 1, cx: 0, cy: 0, anchorX: 0, anchorY: 0 };
  private to: CamState = { scale: 1, cx: 0, cy: 0, anchorX: 0, anchorY: 0 };
  private t = 1;
  private durationMs = SECTION_TWEEN_MS;

  get scale(): number {
    return this.cur.scale;
  }

  get cellPx(): number {
    return this.cur.scale * CELL;
  }

  get moving(): boolean {
    return this.t < 1;
  }

  /** Frames a rectangle given in CELL coordinates. */
  frameTo(bounds: Rect, vp: Viewport, opts: { instant?: boolean; durationMs?: number } = {}): void {
    const frameW = Math.max(1, vp.w);
    const frameH = Math.max(1, vp.h - vp.insetTop - vp.insetBottom);

    const neededW = bounds.w * CELL;
    const neededH = bounds.h * CELL;

    let scale = Math.min(frameW / neededW, frameH / neededH) / FRAME_PADDING;
    if (scale * CELL < MIN_CELL_PX) {
      // Reclaim margin before giving up cell size.
      const relaxed = Math.min(frameW / neededW, frameH / neededH) / MIN_FRAME_PADDING;
      scale = Math.max(scale, Math.min(relaxed, MIN_CELL_PX / CELL));
    }
    if (scale * CELL > MAX_CELL_PX) scale = MAX_CELL_PX / CELL;
    scale = Math.max(scale, 0.01);

    const target: CamState = {
      scale,
      cx: (bounds.x + bounds.w / 2) * CELL,
      cy: (bounds.y + bounds.h / 2) * CELL,
      anchorX: frameW / 2,
      anchorY: vp.insetTop + frameH / 2,
    };

    if (opts.instant) {
      this.cur = { ...target };
      this.from = { ...target };
      this.to = { ...target };
      this.t = 1;
      return;
    }

    this.from = { ...this.cur };
    this.to = target;
    this.durationMs = opts.durationMs ?? SECTION_TWEEN_MS;
    this.t = 0;
  }

  /** Re-anchors without animating - used on resize/orientation change. */
  reanchor(vp: Viewport): void {
    const frameH = Math.max(1, vp.h - vp.insetTop - vp.insetBottom);
    this.cur.anchorX = vp.w / 2;
    this.cur.anchorY = vp.insetTop + frameH / 2;
    this.to.anchorX = this.cur.anchorX;
    this.to.anchorY = this.cur.anchorY;
    this.from.anchorX = this.cur.anchorX;
    this.from.anchorY = this.cur.anchorY;
  }

  update(dtMs: number): boolean {
    if (this.t >= 1) return false;
    this.t = Math.min(1, this.t + dtMs / this.durationMs);
    const k = smoothStep(this.t);
    this.cur = {
      scale: lerp(this.from.scale, this.to.scale, k),
      cx: lerp(this.from.cx, this.to.cx, k),
      cy: lerp(this.from.cy, this.to.cy, k),
      anchorX: lerp(this.from.anchorX, this.to.anchorX, k),
      anchorY: lerp(this.from.anchorY, this.to.anchorY, k),
    };
    return true;
  }

  worldToScreenX(wx: number): number {
    return (wx - this.cur.cx) * this.cur.scale + this.cur.anchorX;
  }

  worldToScreenY(wy: number): number {
    return (wy - this.cur.cy) * this.cur.scale + this.cur.anchorY;
  }

  screenToWorldX(sx: number): number {
    return (sx - this.cur.anchorX) / this.cur.scale + this.cur.cx;
  }

  screenToWorldY(sy: number): number {
    return (sy - this.cur.anchorY) / this.cur.scale + this.cur.cy;
  }

  /** Screen position of the centre of cell (x, y). */
  cellCenterX(x: number): number {
    return this.worldToScreenX((x + 0.5) * CELL);
  }

  cellCenterY(y: number): number {
    return this.worldToScreenY((y + 0.5) * CELL);
  }
}
