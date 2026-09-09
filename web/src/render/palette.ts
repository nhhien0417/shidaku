/**
 * The whole colour system, in one place.
 *
 * THE RULE THAT IS NOT NEGOTIABLE: the only saturated colour allowed on screen is the
 * picture's own colour. That is why the page is warm cream and not white, the buttons are
 * white and not blue, and the cell borders are pale grey and not tinted. The single
 * exception is the combo popup, which exists for 0.9s.
 */

export const COLORS = {
  /** Warm paper cream. Never pure white - the eye has to be able to rest here. */
  bg: '#F7F4ED',
  /** Current section, unsolved: just dark enough to read as "you can play here". */
  activeEmpty: '#ECE5DF',
  /** Future section: almost dissolved into the background. Present, but not inviting. */
  locked: '#F1EDE9',
  /** Cell border on the active section only. Separates cells without drawing a heavy grid. */
  cellBorder: '#CBCBCB',
  /** Selection frame - the strongest contrast on screen while dragging. */
  selection: '#111111',
  /** Cells the finger has swept: grey blocks risen but not committed. */
  hover: '#AAAAAA',
  /** Clue numbers: warm dark brown, not pure black, so they sit in the cream instead of cutting it. */
  clue: '#5A4636',
  /** One beat of yellow before a hint reveals the real colour - "the machine did this, not you". */
  hintFlash: '#FFF28C',
} as const;

export const COMBO_TIERS: ReadonlyArray<{ at: number; label: string; color: string }> = [
  { at: 3, label: 'Nice!', color: '#7FB77E' },
  { at: 6, label: 'Great!', color: '#4A9DD9' },
  { at: 9, label: 'Amazing!', color: '#B565C4' },
  { at: 13, label: 'Unbelievable!', color: '#E8963C' },
  { at: 18, label: 'Masterful!', color: '#E2574C' },
];

export interface Rgb {
  r: number;
  g: number;
  b: number;
}

export function hexToRgb(hex: string): Rgb {
  const s = hex.charAt(0) === '#' ? hex.slice(1) : hex;
  const n = parseInt(s.length === 3 ? s.replace(/./g, (c) => c + c) : s.slice(0, 6), 16);
  return { r: (n >> 16) & 0xff, g: (n >> 8) & 0xff, b: n & 0xff };
}

export function rgbToCss({ r, g, b }: Rgb): string {
  return `rgb(${r},${g},${b})`;
}

function rgbToHsv({ r, g, b }: Rgb): [number, number, number] {
  const rn = r / 255;
  const gn = g / 255;
  const bn = b / 255;
  const max = Math.max(rn, gn, bn);
  const min = Math.min(rn, gn, bn);
  const d = max - min;
  let hue = 0;
  if (d !== 0) {
    if (max === rn) hue = ((gn - bn) / d) % 6;
    else if (max === gn) hue = (bn - rn) / d + 2;
    else hue = (rn - gn) / d + 4;
    hue /= 6;
    if (hue < 0) hue += 1;
  }
  return [hue, max === 0 ? 0 : d / max, max];
}

function hsvToRgb(h: number, s: number, v: number): Rgb {
  const i = Math.floor(h * 6);
  const f = h * 6 - i;
  const p = v * (1 - s);
  const q = v * (1 - f * s);
  const t = v * (1 - (1 - f) * s);
  let r = 0;
  let g = 0;
  let b = 0;
  switch (i % 6) {
    case 0: r = v; g = t; b = p; break;
    case 1: r = q; g = v; b = p; break;
    case 2: r = p; g = v; b = t; break;
    case 3: r = p; g = q; b = v; break;
    case 4: r = t; g = p; b = v; break;
    default: r = v; g = p; b = q; break;
  }
  return { r: Math.round(r * 255), g: Math.round(g * 255), b: Math.round(b * 255) };
}

const MEDIUM_SATURATION_MULTIPLIER = 0.48;
const MEDIUM_VALUE_LERP = 0.22;
/** Perceived-brightness gap the backing must keep from the block sitting on it. */
const MIN_TINT_CONTRAST = 0.13;
/** Above this brightness, lightening has nowhere left to go and must reverse. */
const PALE_THRESHOLD = 0.62;

function luminance({ r, g, b }: Rgb): number {
  return (0.2126 * r + 0.7152 * g + 0.0722 * b) / 255;
}

/**
 * The single most expensive detail in the whole visual language (spec 4.3): on accept, the
 * cell BACKGROUND does not jump to the real colour - it goes to a muted version of it, and
 * only then does the real-coloured block pop up on top. Without it the block rises invisibly
 * (same colour, same place) and the payoff moment - the entire point of the game - is lost.
 *
 * The base formula is Board.cs's, fitted to #B23085 -> #C37FA4.
 *
 * DELIBERATE DEVIATION: that formula only lightens, and it was fitted to one saturated
 * magenta. Applied to a pale colour it does nothing - mediumTint('#F3E2CC') is
 * indistinguishable from '#F3E2CC', so the mushroom stem and the cat's white chest solved
 * into invisible blocks. Verified by eye in the browser, not theorised.
 *
 * So the tint now moves AWAY from the block in whichever direction has room: light colours
 * get a darker backing, everything else keeps the demo's lightening. A pale block on a
 * slightly deeper shade of itself reads as lifted off the surface, which is the same thing
 * the effect was always after. The palette rule still holds - the backing is always the
 * picture's own hue, never a foreign colour.
 */
export function mediumTint(color: Rgb): Rgb {
  const [h, s, v] = rgbToHsv(color);
  const lum = luminance(color);

  // Start from the demo's own formula, so a colour it already handles well is untouched.
  const lit = hsvToRgb(h, s * MEDIUM_SATURATION_MULTIPLIER, v + (1 - v) * MEDIUM_VALUE_LERP);
  if (Math.abs(luminance(lit) - lum) >= MIN_TINT_CONTRAST) return lit;

  // Otherwise walk the value in whichever direction has headroom until the gap is real.
  // Stepping (rather than solving) because dropping saturation RAISES perceived brightness
  // for warm hues, so the two moves partly cancel and there is no clean closed form.
  const goLighter = lum <= PALE_THRESHOLD;
  let best = lit;
  const steps = 12;
  for (let step = 1; step <= steps; step++) {
    const t = step / steps;
    const nextV = goLighter ? v + (1 - v) * (MEDIUM_VALUE_LERP + (1 - MEDIUM_VALUE_LERP) * t) : v * (1 - t);
    const nextS = s * (goLighter ? MEDIUM_SATURATION_MULTIPLIER : 0.7);
    best = hsvToRgb(h, nextS, nextV);
    if (Math.abs(luminance(best) - lum) >= MIN_TINT_CONTRAST) return best;
  }
  return best;
}

/** Under-shadow of a risen block: the same hue, pulled darker. Gives the block its thickness. */
export function shade(color: Rgb, factor = 0.82): Rgb {
  return {
    r: Math.round(color.r * factor),
    g: Math.round(color.g * factor),
    b: Math.round(color.b * factor),
  };
}
