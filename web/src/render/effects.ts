/**
 * Everything transient that is drawn on top of the board: sparkles, the combo popup, the
 * mascot's entrance, and the end-of-picture confetti.
 *
 * All of it lives on the same canvas as the board (no DOM churn mid-drag) and all of it is
 * pure decoration - if this module did nothing, the game would still be playable. It is here
 * because the reward beat is precisely what the test is trying to measure, and an unrewarded
 * correct answer reads as a non-event.
 */

import { clamp01, inQuad, outBack, outCubic, outQuad } from '../util/tween';
import { drawMascot, MASCOT_ROWS } from './mascot';

interface Particle {
  x: number;
  y: number;
  vx: number;
  vy: number;
  size: number;
  life: number;
  maxLife: number;
  color: string;
  spin: number;
  angle: number;
  gravity: number;
  square: boolean;
}

interface Popup {
  x: number;
  y: number;
  text: string;
  sub: string;
  color: string;
  age: number;
  life: number;
}

interface MascotShow {
  x: number;
  y: number;
  px: number;
  text: string;
  age: number;
  inMs: number;
  holdMs: number;
  outMs: number;
}

const COMBO_POPUP_MS = 900;
const SPARKLE_MS = 520;

export class Effects {
  private particles: Particle[] = [];
  private popups: Popup[] = [];
  private mascot: MascotShow | null = null;
  private rng = () => Math.random();

  get busy(): boolean {
    return this.particles.length > 0 || this.popups.length > 0 || this.mascot !== null;
  }

  clear(): void {
    this.particles.length = 0;
    this.popups.length = 0;
    this.mascot = null;
  }

  /**
   * A small ring of light around a region that just landed.
   *
   * `spread` is how far out the ring reaches (region-sized), but the particle SIZE is tied to
   * the cell, not the region - scaling it off the region made a big region throw dinner-plate
   * blobs across the board and buried the very thing it was meant to celebrate.
   */
  sparkleBurst(x: number, y: number, spread: number, cellPx: number, color: string): void {
    const count = 10;
    const size = Math.max(1.5, cellPx * 0.055);
    for (let i = 0; i < count; i++) {
      const angle = (i / count) * Math.PI * 2 + this.rng() * 0.4;
      const speed = spread * (0.9 + this.rng() * 0.7);
      this.particles.push({
        x: x + Math.cos(angle) * spread * 0.4,
        y: y + Math.sin(angle) * spread * 0.4,
        vx: (Math.cos(angle) * speed) / 1000,
        vy: (Math.sin(angle) * speed) / 1000,
        size: size * (0.7 + this.rng() * 0.6),
        life: SPARKLE_MS,
        maxLife: SPARKLE_MS,
        color: i % 3 === 0 ? color : '#FFFFFF',
        spin: 0,
        angle: 0,
        gravity: 0,
        square: false,
      });
    }
  }

  /**
   * The only saturated colour allowed on screen besides the picture itself - and it is gone
   * in 0.9 seconds, which is what keeps that rule honest.
   */
  comboPopup(x: number, y: number, label: string, combo: number, color: string): void {
    this.popups.push({
      x,
      y,
      text: label,
      sub: combo >= 2 ? `x${combo}` : '',
      color,
      age: 0,
      life: COMBO_POPUP_MS,
    });
  }

  mascotEnter(x: number, y: number, px: number, text: string): void {
    this.mascot = { x, y, px, text, age: 0, inMs: 280, holdMs: 420, outMs: 240 };
  }

  confettiBurst(vw: number, vh: number): void {
    const colors = ['#E2574C', '#F2C14E', '#4A9DD9', '#7FB77E', '#B565C4', '#E8963C'];
    for (let i = 0; i < 90; i++) {
      const life = 1800 + this.rng() * 900;
      this.particles.push({
        x: this.rng() * vw,
        y: -20 - this.rng() * vh * 0.35,
        vx: (this.rng() - 0.5) * 0.09,
        vy: 0.16 + this.rng() * 0.14,
        size: 5 + this.rng() * 6,
        life,
        maxLife: life,
        color: colors[Math.floor(this.rng() * colors.length)],
        spin: (this.rng() - 0.5) * 0.012,
        angle: this.rng() * Math.PI,
        gravity: 0.00016,
        square: true,
      });
    }
  }

  update(dtMs: number): boolean {
    let alive = false;

    for (let i = this.particles.length - 1; i >= 0; i--) {
      const p = this.particles[i];
      p.life -= dtMs;
      if (p.life <= 0) {
        this.particles.splice(i, 1);
        continue;
      }
      p.vy += p.gravity * dtMs;
      p.x += p.vx * dtMs;
      p.y += p.vy * dtMs;
      p.angle += p.spin * dtMs;
      alive = true;
    }

    for (let i = this.popups.length - 1; i >= 0; i--) {
      const popup = this.popups[i];
      popup.age += dtMs;
      if (popup.age >= popup.life) this.popups.splice(i, 1);
      else alive = true;
    }

    if (this.mascot) {
      this.mascot.age += dtMs;
      const total = this.mascot.inMs + this.mascot.holdMs + this.mascot.outMs;
      if (this.mascot.age >= total) this.mascot = null;
      else alive = true;
    }

    return alive;
  }

  draw(ctx: CanvasRenderingContext2D): void {
    // --- particles
    for (const p of this.particles) {
      const k = clamp01(p.life / p.maxLife);
      ctx.globalAlpha = p.square ? Math.min(1, k * 2.2) : outQuad(k);
      ctx.fillStyle = p.color;
      if (p.square) {
        ctx.save();
        ctx.translate(p.x, p.y);
        ctx.rotate(p.angle);
        ctx.fillRect(-p.size / 2, -p.size / 4, p.size, p.size / 2);
        ctx.restore();
      } else {
        const size = p.size * (0.4 + k * 0.8);
        ctx.beginPath();
        ctx.arc(p.x, p.y, size, 0, Math.PI * 2);
        ctx.fill();
      }
    }
    ctx.globalAlpha = 1;

    // --- mascot
    if (this.mascot) {
      const m = this.mascot;
      let offset = 0;
      let alpha = 1;
      if (m.age < m.inMs) {
        // Enters from below with an overshoot - the bounce is the point.
        offset = (1 - outBack(m.age / m.inMs)) * MASCOT_ROWS * m.px;
      } else if (m.age > m.inMs + m.holdMs) {
        const k = (m.age - m.inMs - m.holdMs) / m.outMs;
        offset = inQuad(k) * MASCOT_ROWS * m.px * 1.2;
        alpha = 1 - inQuad(k);
      }
      drawMascot(ctx, m.x, m.y + offset, m.px, alpha);

      if (m.text) {
        ctx.globalAlpha = alpha;
        ctx.textAlign = 'center';
        ctx.textBaseline = 'alphabetic';
        ctx.font = `700 ${Math.round(m.px * 2.6)}px Baloo, ui-rounded, system-ui, sans-serif`;
        ctx.fillStyle = '#5A4636';
        ctx.fillText(m.text, m.x, m.y + offset - MASCOT_ROWS * m.px - m.px * 1.4);
        ctx.globalAlpha = 1;
      }
    }

    // --- combo popups (drawn last: they are the loudest thing on screen)
    for (const popup of this.popups) {
      const k = popup.age / popup.life;
      const scale = popup.age < 150 ? outBack(popup.age / 150) : 1;
      const rise = outCubic(k) * 44;
      const alpha = k < 0.6 ? 1 : 1 - inQuad((k - 0.6) / 0.4);

      ctx.save();
      ctx.globalAlpha = alpha;
      ctx.translate(popup.x, popup.y - rise);
      ctx.scale(scale, scale);
      ctx.textAlign = 'center';
      ctx.textBaseline = 'middle';
      ctx.font = '700 26px Baloo, ui-rounded, system-ui, sans-serif';
      ctx.lineWidth = 6;
      ctx.strokeStyle = 'rgba(247,244,237,0.92)';
      const text = popup.sub ? `${popup.text} ${popup.sub}` : popup.text;
      ctx.strokeText(text, 0, 0);
      ctx.fillStyle = popup.color;
      ctx.fillText(text, 0, 0);
      ctx.restore();
    }
    ctx.globalAlpha = 1;
  }
}
