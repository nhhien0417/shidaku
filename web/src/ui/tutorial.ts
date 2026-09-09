/**
 * The three-step tutorial, and the ghost hand that demonstrates a drag when the player
 * stalls.
 *
 * This is where the "is the idea legible" gate is won or lost, so it is worth being precise
 * about why each step exists (all three come from the reference app's own onboarding):
 *
 *   1. horizontal drag over 3 cells - teaches the only verb in the game.
 *   2. vertical drag over 3 cells, WITH THE CLUE ON THE MIDDLE CELL. This is the important
 *      one. Newcomers to Shikaku almost universally assume the drag must start on the
 *      numbered cell; putting the number in the middle makes that impossible, so the lesson
 *      is taught by the board rather than by a sentence. The note says it out loud too.
 *   3. a 2x2 square, no direction given - generalises the verb to any rectangle.
 *
 * There is no skip button. Skipping would mix players who understood the game with players
 * who never saw the explanation into the same funnel, and the 70% legibility threshold would
 * stop meaning anything.
 *
 * Each board is an ordinary Picture with a single region, played through the same Game, the
 * same drag code and the same accept/reject feedback as the real thing - the only honest way
 * for a tutorial to teach a mechanic.
 */

import type { PictureJson } from '../core/types';
import type { Camera } from '../render/camera';
import { clamp01, outQuad, smoothStep } from '../util/tween';

export interface TutorialStep {
  json: PictureJson;
  text: string;
  note: string;
  /** Cells the ghost hand drags between, in the picture's own canvas coordinates. */
  ghostFrom: { x: number; y: number };
  ghostTo: { x: number; y: number };
}

/** Note: coordinates in PictureJson are Unity-style (y up); loader.ts flips them once. */
export const TUTORIAL_STEPS: TutorialStep[] = [
  {
    json: {
      id: 'tut1',
      name: 'Tutorial 1',
      w: 3,
      h: 1,
      palette: ['#00000000', '#E2574C'],
      pixels: 'BBB',
      sections: [{ regions: [{ r: [0, 0, 3, 1], c: [1, 0] }] }],
    },
    text: 'Draw a horizontal rectangle that covers 3 cells.',
    note: '',
    ghostFrom: { x: 0, y: 0 },
    ghostTo: { x: 2, y: 0 },
  },
  {
    json: {
      id: 'tut2',
      name: 'Tutorial 2',
      w: 1,
      h: 3,
      palette: ['#00000000', '#4A9DD9'],
      pixels: 'BBB',
      // Clue on the MIDDLE cell: the drag cannot start on the number.
      sections: [{ regions: [{ r: [0, 0, 1, 3], c: [0, 1] }] }],
    },
    text: 'Draw a vertical rectangle that covers 3 cells.',
    note: "(You don't have to start from the cell with the number.)",
    ghostFrom: { x: 0, y: 0 },
    ghostTo: { x: 0, y: 2 },
  },
  {
    json: {
      id: 'tut3',
      name: 'Tutorial 3',
      w: 2,
      h: 2,
      palette: ['#00000000', '#F2C14E'],
      pixels: 'BBBB',
      sections: [{ regions: [{ r: [0, 0, 2, 2], c: [0, 0] }] }],
    },
    text: 'Draw a rectangle that covers the area.',
    note: '',
    ghostFrom: { x: 0, y: 0 },
    ghostTo: { x: 1, y: 1 },
  },
];

const IDLE_BEFORE_GHOST_MS = 2500;
const GHOST_CYCLE_MS = 2200;

/**
 * A finger that shows the drag after 2.5s of inactivity, looping until the player does it
 * themselves. Costs nothing and removes the single most common way a first session dies:
 * not knowing that dragging is the interaction at all.
 */
export class GhostHand {
  private idleMs = 0;
  private cycleMs = 0;
  private active = false;
  private step: TutorialStep | null = null;

  setStep(step: TutorialStep | null): void {
    this.step = step;
    this.idleMs = 0;
    this.cycleMs = 0;
    this.active = false;
  }

  /** Any real input resets the timer and hides the hand. */
  noteActivity(): void {
    this.idleMs = 0;
    this.cycleMs = 0;
    this.active = false;
  }

  update(dtMs: number): boolean {
    if (!this.step) return false;
    if (!this.active) {
      this.idleMs += dtMs;
      if (this.idleMs >= IDLE_BEFORE_GHOST_MS) {
        this.active = true;
        this.cycleMs = 0;
      }
      return this.active;
    }
    this.cycleMs = (this.cycleMs + dtMs) % GHOST_CYCLE_MS;
    return true;
  }

  draw(ctx: CanvasRenderingContext2D, cam: Camera, pictureHeight: number): void {
    if (!this.active || !this.step) return;

    // The step's ghost coordinates are authored in Unity space; flip them the same way the
    // loader flips the board so the hand and the board agree.
    const from = { x: this.step.ghostFrom.x, y: pictureHeight - 1 - this.step.ghostFrom.y };
    const to = { x: this.step.ghostTo.x, y: pictureHeight - 1 - this.step.ghostTo.y };

    const phase = this.cycleMs / GHOST_CYCLE_MS;
    // 0.00-0.15 appear, 0.15-0.62 drag, 0.62-0.80 hold, 0.80-1.00 fade
    let travel = 0;
    let alpha = 1;
    if (phase < 0.15) {
      alpha = outQuad(phase / 0.15);
    } else if (phase < 0.62) {
      travel = smoothStep((phase - 0.15) / 0.47);
    } else if (phase < 0.8) {
      travel = 1;
    } else {
      travel = 1;
      alpha = 1 - clamp01((phase - 0.8) / 0.2);
    }

    const cx = cam.cellCenterX(from.x + (to.x - from.x) * travel);
    const cy = cam.cellCenterY(from.y + (to.y - from.y) * travel);
    const r = Math.max(12, cam.cellPx * 0.26);

    ctx.save();
    ctx.globalAlpha = alpha * 0.5;
    ctx.fillStyle = '#5A4636';
    ctx.beginPath();
    ctx.arc(cx, cy, r, 0, Math.PI * 2);
    ctx.fill();
    ctx.globalAlpha = alpha * 0.85;
    ctx.strokeStyle = '#FFFDF8';
    ctx.lineWidth = Math.max(2, r * 0.18);
    ctx.beginPath();
    ctx.arc(cx, cy, r, 0, Math.PI * 2);
    ctx.stroke();
    ctx.restore();
  }
}
