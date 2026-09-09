/**
 * The board, drawn on one canvas.
 *
 * No DOM per cell - on purpose. The Unity demo gives every cell its own GameObject, which is
 * fine inside Unity UI, but 300-700 animating DOM nodes inside Facebook's in-app WebView on a
 * low-end Android is exactly how you end up measuring the renderer instead of the mechanic.
 * All per-cell animation state lives in flat typed arrays and is evaluated at draw time.
 *
 * Every timing constant here comes from the demo (UICell.cs / Board.cs) via the spec's
 * appendix, with one deliberate change - see WAVE_MAX_MS.
 */

import { CELL, type Camera, type Viewport } from './camera';
import { COLORS, hexToRgb, mediumTint, rgbToCss, shade, type Rgb } from './palette';
import type { CellPos, Picture, Rect, Region } from '../core/types';
import { clamp01, expSmooth, inBounce, inQuad, outBounce, outQuad } from '../util/tween';

export const POP_MS = 350;
const SQUEEZE_W = 0.95;
const SQUEEZE_H = 1.1;

/** Board.cs: acceptStep = AcceptPopDuration * 0.5. */
const ACCEPT_STEP_MS = POP_MS * 0.5;
/** Board.cs: rejectStep = RejectPopDuration * 0.25 - a wrong answer collapses twice as fast. */
const REJECT_STEP_MS = POP_MS * 0.25;

/**
 * DELIBERATE DEVIATION from the demo. The demo staggers by a flat 175ms per cell, so a
 * 15-cell region takes 15 x 175 + 350 = ~3s to finish rising, during which input is locked.
 * On a phone that reads as lag, not as reward. The stagger is compressed so a whole wave
 * never exceeds ~1.1s: small regions keep the demo's exact cadence, large ones stay snappy
 * while still resolving cell by cell (which is the part that makes a big region satisfying).
 */
const WAVE_MAX_MS = 1100;

const SHAKE_MS = 220;
const SHAKE_AMPLITUDE = 0.12; // of one cell
const BORDER_THICKNESS = 0.07; // of one cell
const BADGE_OFFSET = 0.5; // of one cell
const CLUE_FADE_MS = 300;
const CLUE_FADE_STAGGER_MS = 15;
const SELECTION_SHARPNESS = 22;
const CELL_INSET = 0.05; // cells render at 90% of their slot, leaving a 10% gap
const CORNER_RADIUS = 0.18;
const HINT_FLASH_MS = 260;

/** Sentinel block colours that are not palette entries. */
const BLOCK_HOVER = 254;
const BLOCK_NONE = 255;

const BG_LOCKED = 0;
const BG_ACTIVE = 1;
const BG_SOLVED = 2;

function rrect(ctx: CanvasRenderingContext2D, x: number, y: number, w: number, h: number, r: number): void {
  const radius = Math.min(r, w / 2, h / 2);
  ctx.beginPath();
  // roundRect is missing in older in-app WebViews; the manual path is the fallback.
  const anyCtx = ctx as CanvasRenderingContext2D & { roundRect?: (...a: number[]) => void };
  if (typeof anyCtx.roundRect === 'function') {
    anyCtx.roundRect(x, y, w, h, radius);
    return;
  }
  ctx.moveTo(x + radius, y);
  ctx.lineTo(x + w - radius, y);
  ctx.quadraticCurveTo(x + w, y, x + w, y + radius);
  ctx.lineTo(x + w, y + h - radius);
  ctx.quadraticCurveTo(x + w, y + h, x + w - radius, y + h);
  ctx.lineTo(x + radius, y + h);
  ctx.quadraticCurveTo(x, y + h, x, y + h - radius);
  ctx.lineTo(x, y + radius);
  ctx.quadraticCurveTo(x, y, x + radius, y);
  ctx.closePath();
}

interface Selection {
  visible: boolean;
  /** World-space centre and size, lerped toward the target so the frame trails the finger. */
  cx: number;
  cy: number;
  w: number;
  h: number;
  targetCx: number;
  targetCy: number;
  targetW: number;
  targetH: number;
  area: number;
  badgeVisible: boolean;
  badgeAge: number;
  snapped: boolean;
}

export class BoardView {
  picture: Picture | null = null;

  private cssPalette: string[] = [];
  private cssMedium: string[] = [];
  private cssShade: string[] = [];
  private rgbPalette: Rgb[] = [];

  private blockStart!: Float32Array;
  private blockDur!: Float32Array;
  private blockDir!: Uint8Array;
  private blockColor!: Uint8Array;
  private flashUntil!: Float32Array;
  private clueArea!: Uint8Array;
  private clueFadeStart!: Float32Array;
  private bgMode!: Uint8Array;

  private hovered = new Set<number>();
  private now = 0;

  private shakeStart = -1;
  private shakeCx = 0;
  private shakeCy = 0;
  private shakeW = 0;
  private shakeH = 0;

  private sel: Selection = {
    visible: false,
    cx: 0,
    cy: 0,
    w: 0,
    h: 0,
    targetCx: 0,
    targetCy: 0,
    targetW: 0,
    targetH: 0,
    area: 0,
    badgeVisible: false,
    badgeAge: 0,
    snapped: false,
  };

  private lastFontPx = -1;

  // ---------------------------------------------------------------- setup

  setPicture(picture: Picture): void {
    this.picture = picture;
    const n = picture.w * picture.h;

    this.blockStart = new Float32Array(n).fill(Number.NEGATIVE_INFINITY);
    this.blockDur = new Float32Array(n).fill(POP_MS);
    this.blockDir = new Uint8Array(n);
    this.blockColor = new Uint8Array(n).fill(BLOCK_NONE);
    this.flashUntil = new Float32Array(n);
    this.clueArea = new Uint8Array(n);
    this.clueFadeStart = new Float32Array(n).fill(Number.NEGATIVE_INFINITY);
    this.bgMode = new Uint8Array(n).fill(BG_LOCKED);
    this.hovered.clear();
    this.shakeStart = -1;
    this.sel.visible = false;
    this.sel.badgeVisible = false;

    this.rgbPalette = picture.palette.map((hex) => hexToRgb(hex));
    this.cssPalette = this.rgbPalette.map((c) => rgbToCss(c));
    this.cssMedium = this.rgbPalette.map((c) => rgbToCss(mediumTint(c)));
    this.cssShade = this.rgbPalette.map((c) => rgbToCss(shade(c)));

    for (const section of picture.sections) {
      for (const region of section.regions) {
        this.clueArea[region.clue.y * picture.w + region.clue.x] = region.area;
      }
    }
  }

  /**
   * Recolours every unsolved cell for the section that is now active.
   *
   * The locked state deliberately hides its clue numbers: a number sitting on a cell you
   * cannot drag reads as a false invitation, and the player tries it, gets refused, and has
   * no idea why. Hiding them makes the playable boundary self-evident without a word of
   * explanation - while the faint tiles still show that the picture continues past the frame.
   */
  refreshActivation(currentSectionIndex: number, options: { fadeClues?: boolean } = {}): void {
    const picture = this.picture;
    if (!picture) return;

    for (let i = 0; i < picture.sections.length; i++) {
      const section = picture.sections[i];
      const isCurrent = i === currentSectionIndex;
      for (const region of section.regions) {
        if (region.solved) continue;
        for (let y = region.rect.y; y < region.rect.y + region.rect.h; y++) {
          for (let x = region.rect.x; x < region.rect.x + region.rect.w; x++) {
            const idx = y * picture.w + x;
            this.bgMode[idx] = isCurrent ? BG_ACTIVE : BG_LOCKED;
            this.blockColor[idx] = BLOCK_NONE;
            this.blockStart[idx] = Number.NEGATIVE_INFINITY;
          }
        }
        const clueIdx = region.clue.y * picture.w + region.clue.x;
        this.clueFadeStart[clueIdx] = isCurrent
          ? options.fadeClues === false
            ? Number.NEGATIVE_INFINITY
            : this.now + this.clueStagger(section, region)
          : Number.NEGATIVE_INFINITY;
      }
    }
  }

  private clueStagger(section: { regions: Region[] }, region: Region): number {
    const index = section.regions.indexOf(region);
    return Math.max(0, index) * CLUE_FADE_STAGGER_MS;
  }

  // ---------------------------------------------------------------- interaction feedback

  setHover(cells: CellPos[]): void {
    const picture = this.picture;
    if (!picture) return;

    const next = new Set<number>();
    for (const cell of cells) next.add(cell.y * picture.w + cell.x);

    for (const idx of this.hovered) {
      if (!next.has(idx)) this.startBlock(idx, BLOCK_NONE, 1, 0);
    }
    for (const idx of next) {
      if (!this.hovered.has(idx)) this.startBlock(idx, BLOCK_HOVER, 0, 0);
    }
    this.hovered = next;
  }

  // No clearHover(): dropping the set without animating would leave grey blocks risen with
  // nothing to take them down. Callers use setHover([]) so the wave out always runs.

  /**
   * The heart of the game: the accepted wave.
   *
   * The background goes to a light, desaturated version of the real colour FIRST, and only
   * then does the real-coloured block rise on top of it. Skipping that step makes the block
   * invisible against its own colour and throws away the payoff (spec 4.3).
   */
  popRegion(region: Region, isHint: boolean): void {
    const picture = this.picture;
    if (!picture) return;

    const cells: number[] = [];
    for (let y = region.rect.y; y < region.rect.y + region.rect.h; y++) {
      for (let x = region.rect.x; x < region.rect.x + region.rect.w; x++) {
        cells.push(y * picture.w + x);
      }
    }

    const step = Math.min(ACCEPT_STEP_MS, WAVE_MAX_MS / Math.max(1, cells.length));
    cells.forEach((idx, order) => {
      this.bgMode[idx] = BG_SOLVED;
      this.startBlock(idx, picture.pixels[idx], 0, order * step);
      if (isHint) this.flashUntil[idx] = this.now + order * step + POP_MS + HINT_FLASH_MS;
    });

    this.clueArea[region.clue.y * picture.w + region.clue.x] = 0;
    this.hovered.clear();
  }

  /** Wave back down, faster than it came up: decisive, but not a punishment. */
  rejectRegion(rect: Rect): void {
    const picture = this.picture;
    if (!picture) return;

    const cells: number[] = [];
    for (let y = rect.y; y < rect.y + rect.h; y++) {
      for (let x = rect.x; x < rect.x + rect.w; x++) cells.push(y * picture.w + x);
    }
    const step = Math.min(REJECT_STEP_MS, (WAVE_MAX_MS * 0.5) / Math.max(1, cells.length));
    cells.forEach((idx, order) => this.startBlock(idx, BLOCK_NONE, 1, order * step));
    this.hovered.clear();

    this.shakeStart = this.now;
    this.shakeCx = (rect.x + rect.w / 2) * CELL;
    this.shakeCy = (rect.y + rect.h / 2) * CELL;
    this.shakeW = rect.w * CELL * 0.94;
    this.shakeH = rect.h * CELL * 0.94;
    this.sel.visible = false;
    this.sel.badgeVisible = false;
  }

  private startBlock(idx: number, color: number, dir: 0 | 1, delay: number): void {
    if (dir === 0) this.blockColor[idx] = color;
    else if (color !== BLOCK_NONE) this.blockColor[idx] = color;
    this.blockStart[idx] = this.now + delay;
    this.blockDur[idx] = POP_MS;
    this.blockDir[idx] = dir;
  }

  // ---------------------------------------------------------------- selection

  showSelection(rect: Rect, snap: boolean): void {
    this.sel.targetCx = (rect.x + rect.w / 2) * CELL;
    this.sel.targetCy = (rect.y + rect.h / 2) * CELL;
    this.sel.targetW = rect.w * CELL * 0.94;
    this.sel.targetH = rect.h * CELL * 0.94;
    this.sel.area = rect.w * rect.h;

    if (snap || !this.sel.visible) {
      this.sel.cx = this.sel.targetCx;
      this.sel.cy = this.sel.targetCy;
      this.sel.w = this.sel.targetW;
      this.sel.h = this.sel.targetH;
      this.sel.badgeAge = 0;
    }
    this.sel.visible = true;
    this.sel.badgeVisible = true;
  }

  hideSelection(): void {
    this.sel.visible = false;
    this.sel.badgeVisible = false;
  }

  // ---------------------------------------------------------------- frame

  /** Returns true while anything on the board still needs to be redrawn. */
  update(nowMs: number, dtMs: number): boolean {
    this.now = nowMs;
    let busy = false;

    if (this.sel.visible) {
      const k = expSmooth(SELECTION_SHARPNESS, dtMs / 1000);
      this.sel.cx += (this.sel.targetCx - this.sel.cx) * k;
      this.sel.cy += (this.sel.targetCy - this.sel.cy) * k;
      this.sel.w += (this.sel.targetW - this.sel.w) * k;
      this.sel.h += (this.sel.targetH - this.sel.h) * k;
      this.sel.badgeAge += dtMs;
      busy = true;
    }

    if (this.shakeStart >= 0) {
      if (nowMs - this.shakeStart >= SHAKE_MS) this.shakeStart = -1;
      busy = true;
    }

    if (!busy && this.picture) {
      // Cheap scan: any block or clue still mid-animation keeps the loop awake.
      for (let i = 0; i < this.blockStart.length; i++) {
        const start = this.blockStart[i];
        if (start !== Number.NEGATIVE_INFINITY && nowMs < start + this.blockDur[i]) {
          busy = true;
          break;
        }
        const clueStart = this.clueFadeStart[i];
        if (clueStart !== Number.NEGATIVE_INFINITY && nowMs < clueStart + CLUE_FADE_MS) {
          busy = true;
          break;
        }
        if (this.flashUntil[i] > nowMs) {
          busy = true;
          break;
        }
      }
    }

    return busy;
  }

  /** Two-stage squash-and-bounce, matching UICell.PlayPopIn / PlayPopOut exactly. */
  private blockScale(idx: number): [number, number] {
    const start = this.blockStart[idx];
    if (start === Number.NEGATIVE_INFINITY) return [0, 0];
    const dir = this.blockDir[idx];
    const p = (this.now - start) / this.blockDur[idx];

    if (p <= 0) return dir === 0 ? [0, 0] : [1, 1];
    if (p >= 1) return dir === 0 ? [1, 1] : [0, 0];

    if (dir === 0) {
      if (p < 0.5) {
        const k = outQuad(p * 2);
        return [SQUEEZE_W * k, SQUEEZE_H * k];
      }
      const k = outBounce((p - 0.5) * 2);
      return [SQUEEZE_W + (1 - SQUEEZE_W) * k, SQUEEZE_H + (1 - SQUEEZE_H) * k];
    }

    if (p < 0.5) {
      const k = inBounce(p * 2);
      return [1 + (SQUEEZE_W - 1) * k, 1 + (SQUEEZE_H - 1) * k];
    }
    const k = inQuad((p - 0.5) * 2);
    return [SQUEEZE_W * (1 - k), SQUEEZE_H * (1 - k)];
  }

  draw(ctx: CanvasRenderingContext2D, cam: Camera, vp: Viewport): void {
    const picture = this.picture;
    if (!picture) return;

    const cellPx = cam.cellPx;
    const radius = cellPx * CORNER_RADIUS;
    const inset = cellPx * CELL_INSET;
    const size = cellPx - inset * 2;

    // Cull to the visible window. The board is intentionally NOT masked - parts of the
    // picture outside the play frame stay visible and simply get drawn under the HUD, so the
    // picture reads as one continuous thing rather than something trapped in a box.
    const x0 = Math.max(0, Math.floor(cam.screenToWorldX(-cellPx) / CELL));
    const x1 = Math.min(picture.w - 1, Math.ceil(cam.screenToWorldX(vp.w + cellPx) / CELL));
    const y0 = Math.max(0, Math.floor(cam.screenToWorldY(-cellPx) / CELL));
    const y1 = Math.min(picture.h - 1, Math.ceil(cam.screenToWorldY(vp.h + cellPx) / CELL));

    const fontPx = Math.round(cellPx * 0.46);
    if (fontPx !== this.lastFontPx) this.lastFontPx = fontPx;
    ctx.textAlign = 'center';
    ctx.textBaseline = 'middle';
    ctx.lineJoin = 'round';

    for (let y = y0; y <= y1; y++) {
      const sy = cam.worldToScreenY(y * CELL) + inset;
      for (let x = x0; x <= x1; x++) {
        const idx = y * picture.w + x;
        const pal = picture.pixels[idx];
        if (pal === 0) continue;

        const sx = cam.worldToScreenX(x * CELL) + inset;
        const mode = this.bgMode[idx];

        ctx.fillStyle =
          mode === BG_SOLVED ? this.cssMedium[pal] : mode === BG_ACTIVE ? COLORS.activeEmpty : COLORS.locked;
        rrect(ctx, sx, sy, size, size, radius);
        ctx.fill();

        if (mode === BG_ACTIVE) {
          ctx.strokeStyle = COLORS.cellBorder;
          ctx.lineWidth = Math.max(1, cellPx * 0.03);
          rrect(ctx, sx, sy, size, size, radius);
          ctx.stroke();
        }

        const [scaleX, scaleY] = this.blockScale(idx);
        if (scaleX > 0.004 && scaleY > 0.004) {
          const colorIdx = this.blockColor[idx];
          let face: string;
          let under: string;
          if (colorIdx === BLOCK_HOVER || colorIdx === BLOCK_NONE) {
            face = COLORS.hover;
            under = '#8F8F8F';
          } else if (this.flashUntil[idx] > this.now) {
            face = COLORS.hintFlash;
            under = '#E0D26B';
          } else {
            face = this.cssPalette[colorIdx];
            under = this.cssShade[colorIdx];
          }

          const bw = size * scaleX;
          const bh = size * scaleY;
          const bx = sx + (size - bw) / 2;
          const by = sy + (size - bh) / 2;
          const lift = cellPx * 0.045;

          ctx.fillStyle = under;
          rrect(ctx, bx, by + lift, bw, bh, radius);
          ctx.fill();
          ctx.fillStyle = face;
          rrect(ctx, bx, by, bw, bh, radius);
          ctx.fill();
        }

        const area = this.clueArea[idx];
        if (area > 0 && mode === BG_ACTIVE) {
          const fadeStart = this.clueFadeStart[idx];
          const alpha =
            fadeStart === Number.NEGATIVE_INFINITY ? 1 : clamp01((this.now - fadeStart) / CLUE_FADE_MS);
          if (alpha > 0.01) {
            ctx.globalAlpha = outQuad(alpha);
            ctx.fillStyle = COLORS.clue;
            ctx.font = `700 ${fontPx}px Baloo, ui-rounded, "Segoe UI Rounded", system-ui, sans-serif`;
            ctx.fillText(String(area), sx + size / 2, sy + size / 2 + fontPx * 0.06);
            ctx.globalAlpha = 1;
          }
        }
      }
    }

    this.drawSelection(ctx, cam, vp);
  }

  private drawSelection(ctx: CanvasRenderingContext2D, cam: Camera, vp: Viewport): void {
    const cellPx = cam.cellPx;
    const thickness = Math.max(2, cellPx * BORDER_THICKNESS);

    let cx: number;
    let cy: number;
    let w: number;
    let h: number;
    let area: number;
    let showBadge: boolean;

    if (this.shakeStart >= 0) {
      const t = clamp01((this.now - this.shakeStart) / SHAKE_MS);
      // sin(6*pi*t), damped - Board.cs's RejectFeedback, to the constant.
      const offset = Math.sin(t * Math.PI * 6) * CELL * SHAKE_AMPLITUDE * (1 - t);
      cx = this.shakeCx + offset;
      cy = this.shakeCy;
      w = this.shakeW;
      h = this.shakeH;
      area = 0;
      showBadge = false;
    } else if (this.sel.visible) {
      cx = this.sel.cx;
      cy = this.sel.cy;
      w = this.sel.w;
      h = this.sel.h;
      area = this.sel.area;
      showBadge = this.sel.badgeVisible;
    } else {
      return;
    }

    const left = cam.worldToScreenX(cx - w / 2);
    const right = cam.worldToScreenX(cx + w / 2);
    const top = cam.worldToScreenY(cy - h / 2);
    const bottom = cam.worldToScreenY(cy + h / 2);

    ctx.fillStyle = COLORS.selection;
    // Four bars, each overhanging by the thickness so the corners close cleanly.
    ctx.fillRect(left - thickness / 2, top - thickness / 2, right - left + thickness, thickness);
    ctx.fillRect(left - thickness / 2, bottom - thickness / 2, right - left + thickness, thickness);
    ctx.fillRect(left - thickness / 2, top - thickness / 2, thickness, bottom - top + thickness);
    ctx.fillRect(right - thickness / 2, top - thickness / 2, thickness, bottom - top + thickness);

    if (!showBadge || area <= 0) return;

    /**
     * The area badge: the single most important piece of UX help in the game. Without it the
     * player has to count cells by eye against the clue; with it, they just drag until the
     * number matches.
     */
    const r = Math.max(14, cellPx * 0.34);
    const punch = this.sel.badgeAge < 180 ? 1 + 0.18 * Math.sin((this.sel.badgeAge / 180) * Math.PI) : 1;
    let bx = (left + right) / 2;
    let by = top - cam.cellPx * BADGE_OFFSET;
    // Flip below the frame when there is no room above it (top rows, or a tall selection).
    if (by - r < vp.insetTop + 4) by = bottom + cam.cellPx * BADGE_OFFSET;
    bx = Math.min(Math.max(bx, r + 4), vp.w - r - 4);

    ctx.save();
    ctx.translate(bx, by);
    ctx.scale(punch, punch);
    ctx.beginPath();
    ctx.arc(0, 0, r, 0, Math.PI * 2);
    ctx.fillStyle = '#FFFFFF';
    ctx.fill();
    ctx.lineWidth = Math.max(2, r * 0.16);
    ctx.strokeStyle = COLORS.selection;
    ctx.stroke();
    ctx.fillStyle = COLORS.selection;
    ctx.font = `700 ${Math.round(r * 1.15)}px Baloo, ui-rounded, system-ui, sans-serif`;
    ctx.textAlign = 'center';
    ctx.textBaseline = 'middle';
    ctx.fillText(String(area), 0, r * 0.06);
    ctx.restore();
  }

  /** Screen position of a region's centre - used to anchor popups and sparkles. */
  regionScreenCentre(cam: Camera, rect: Rect): { x: number; y: number; radius: number } {
    return {
      x: cam.worldToScreenX((rect.x + rect.w / 2) * CELL),
      y: cam.worldToScreenY((rect.y + rect.h / 2) * CELL),
      radius: Math.max(rect.w, rect.h) * cam.cellPx * 0.5,
    };
  }
}

/**
 * The Overview button's icon: a live thumbnail of the picture, one canvas pixel per cell,
 * scaled up with image-rendering: pixelated.
 *
 * It is not decoration - it IS the progress bar, expressed in the product itself. Watching it
 * fill in is the strongest reason this genre gets a second session.
 */
export function renderMiniMap(canvas: HTMLCanvasElement, picture: Picture): void {
  if (canvas.width !== picture.w || canvas.height !== picture.h) {
    canvas.width = picture.w;
    canvas.height = picture.h;
  }
  const ctx = canvas.getContext('2d');
  if (!ctx) return;

  ctx.clearRect(0, 0, picture.w, picture.h);
  for (let y = 0; y < picture.h; y++) {
    for (let x = 0; x < picture.w; x++) {
      const idx = y * picture.w + x;
      const pal = picture.pixels[idx];
      if (pal === 0) continue;
      // Unsolved has to be visible enough that the icon reads as a silhouette filling in -
      // too faint and it looks like an empty button rather than a progress display.
      ctx.fillStyle = picture.solved[idx] === 1 ? picture.palette[pal] : 'rgba(90,70,54,0.22)';
      ctx.fillRect(x, y, 1, 1);
    }
  }
}
