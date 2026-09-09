/**
 * Easing. Every value here is lifted from the demo's DOTween usage (UICell.cs / Board.cs) so
 * the web build feels like the same game, not a lookalike.
 *
 * Rule from the spec, enforced by review: there is no linear motion anywhere in this game.
 * If you reach for a plain `t`, you want outQuad at minimum.
 */

export const clamp01 = (t: number): number => (t < 0 ? 0 : t > 1 ? 1 : t);

export const lerp = (a: number, b: number, t: number): number => a + (b - a) * t;

export const outQuad = (t: number): number => {
  const k = clamp01(t);
  return 1 - (1 - k) * (1 - k);
};

export const inQuad = (t: number): number => {
  const k = clamp01(t);
  return k * k;
};

export const outCubic = (t: number): number => {
  const k = clamp01(t);
  return 1 - Math.pow(1 - k, 3);
};

/** Unity's Mathf.SmoothStep(0,1,t) - soft at both ends. Camera moves use this. */
export const smoothStep = (t: number): number => {
  const k = clamp01(t);
  return k * k * (3 - 2 * k);
};

/** Overshoots past 1 then settles. Combo popups scale in with this. */
export const outBack = (t: number, s = 1.7): number => {
  const k = clamp01(t) - 1;
  return k * k * ((s + 1) * k + s) + 1;
};

/** Robert Penner's easeOutBounce, matching DOTween's Ease.OutBounce. */
export function outBounce(t: number): number {
  let k = clamp01(t);
  const n1 = 7.5625;
  const d1 = 2.75;
  if (k < 1 / d1) return n1 * k * k;
  if (k < 2 / d1) return n1 * (k -= 1.5 / d1) * k + 0.75;
  if (k < 2.5 / d1) return n1 * (k -= 2.25 / d1) * k + 0.9375;
  return n1 * (k -= 2.625 / d1) * k + 0.984375;
}

/** DOTween's Ease.InBounce - the mirror, used for the collapse half of a pop-out. */
export const inBounce = (t: number): number => 1 - outBounce(1 - clamp01(t));

/**
 * Frame-rate independent exponential smoothing: the selection frame chasing the finger.
 * Board.cs uses `1 - exp(-22 * dt)` with sharpness 22; same here so the drag has the same
 * inertia (the spec explicitly wants the frame to trail the finger, not snap per cell).
 */
export const expSmooth = (sharpness: number, dt: number): number => 1 - Math.exp(-sharpness * dt);
