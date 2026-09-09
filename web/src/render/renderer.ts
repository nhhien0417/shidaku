/**
 * Canvas ownership: sizing, device pixel ratio, and the frame loop.
 *
 * The loop always runs, but a frame is only PAINTED when something asked for it. Idle time
 * therefore costs one empty callback rather than a full repaint of a few hundred rounded
 * rectangles - which matters on the low-end Android the ad traffic will mostly arrive on.
 */

import type { Viewport } from './camera';
import { COLORS } from './palette';
import type { SafeArea } from '../util/device';

/**
 * Cheap phones frequently report devicePixelRatio 3. Rendering 3x costs 9x the fill rate for
 * a difference nobody can see on a 5" screen, and fill rate is exactly what runs out first.
 */
const MAX_DPR = 2;

/** Room for the level label at the top, and the two buttons plus progress at the bottom. */
const HUD_TOP = 52;
const HUD_BOTTOM = 128;

export class Renderer {
  readonly canvas: HTMLCanvasElement;
  readonly ctx: CanvasRenderingContext2D;
  vp: Viewport = { w: 0, h: 0, insetTop: HUD_TOP, insetBottom: HUD_BOTTOM };
  dpr = 1;
  fps = 0;

  onFrame: (nowMs: number, dtMs: number) => boolean = () => false;
  onDraw: (ctx: CanvasRenderingContext2D, vp: Viewport) => void = () => {};
  onResize: (vp: Viewport) => void = () => {};

  private safeArea: SafeArea;
  private dirty = true;
  private lastNow = 0;
  private fpsAccum = 0;
  private fpsFrames = 0;
  private running = false;

  constructor(canvas: HTMLCanvasElement, safeArea: SafeArea) {
    this.canvas = canvas;
    this.safeArea = safeArea;
    const ctx = canvas.getContext('2d', { alpha: false });
    if (!ctx) throw new Error('canvas 2d context unavailable');
    this.ctx = ctx;
    this.resize();
  }

  markDirty(): void {
    this.dirty = true;
  }

  resize(): void {
    const cssW = Math.max(1, window.innerWidth);
    const cssH = Math.max(1, window.innerHeight);
    this.dpr = Math.min(window.devicePixelRatio || 1, MAX_DPR);

    this.canvas.width = Math.round(cssW * this.dpr);
    this.canvas.height = Math.round(cssH * this.dpr);
    this.canvas.style.width = `${cssW}px`;
    this.canvas.style.height = `${cssH}px`;
    this.ctx.setTransform(this.dpr, 0, 0, this.dpr, 0, 0);

    this.vp = {
      w: cssW,
      h: cssH,
      insetTop: HUD_TOP + this.safeArea.top,
      insetBottom: HUD_BOTTOM + this.safeArea.bottom,
    };
    this.dirty = true;
    this.onResize(this.vp);
  }

  start(): void {
    if (this.running) return;
    this.running = true;
    const tick = (now: number): void => {
      if (!this.running) return;
      // Clamp dt: a backgrounded tab returns with a huge delta that would teleport tweens.
      const dt = this.lastNow === 0 ? 16.7 : Math.min(64, now - this.lastNow);
      this.lastNow = now;

      this.fpsAccum += dt;
      this.fpsFrames++;
      if (this.fpsAccum >= 500) {
        this.fps = Math.round((this.fpsFrames * 1000) / this.fpsAccum);
        this.fpsAccum = 0;
        this.fpsFrames = 0;
      }

      const busy = this.onFrame(now, dt);
      if (busy || this.dirty) {
        this.dirty = false;
        this.ctx.fillStyle = COLORS.bg;
        this.ctx.fillRect(0, 0, this.vp.w, this.vp.h);
        this.onDraw(this.ctx, this.vp);
      }

      requestAnimationFrame(tick);
    };
    requestAnimationFrame(tick);
  }

  stop(): void {
    this.running = false;
  }
}
