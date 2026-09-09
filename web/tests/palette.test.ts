import { describe, expect, it } from 'vitest';
import { hexToRgb, mediumTint, shade, type Rgb } from '../src/render/palette';

const lum = ({ r, g, b }: Rgb): number => (0.2126 * r + 0.7152 * g + 0.0722 * b) / 255;
const contrast = (hex: string): number => Math.abs(lum(mediumTint(hexToRgb(hex))) - lum(hexToRgb(hex)));

describe('mediumTint', () => {
  it('keeps the demo behaviour for the colour it was fitted to (#B23085 -> ~#C37FA4)', () => {
    const got = mediumTint(hexToRgb('#B23085'));
    const want = hexToRgb('#C37FA4');
    // Within a few units per channel of the reference tone in Board.cs.
    expect(Math.abs(got.r - want.r)).toBeLessThan(12);
    expect(Math.abs(got.g - want.g)).toBeLessThan(12);
    expect(Math.abs(got.b - want.b)).toBeLessThan(12);
  });

  it('lightens dark and mid colours', () => {
    expect(lum(mediumTint(hexToRgb('#B23085')))).toBeGreaterThan(lum(hexToRgb('#B23085')));
    expect(lum(mediumTint(hexToRgb('#6E6A66')))).toBeGreaterThan(lum(hexToRgb('#6E6A66')));
  });

  it('keeps lightening a near-neutral grey until the gap is real, rather than reversing', () => {
    // #6E6A66 is almost unsaturated, so one pass of the demo formula moves it barely at all.
    const grey = hexToRgb('#6E6A66');
    expect(lum(mediumTint(grey))).toBeGreaterThan(lum(grey));
    expect(contrast('#6E6A66')).toBeGreaterThan(0.1);
  });

  it('darkens pale colours instead, because lightening has nowhere to go', () => {
    // This is the bug the deviation exists for: mediumTint of a pale cream used to come back
    // as the same pale cream, so the solved block was invisible against its own backing.
    expect(lum(mediumTint(hexToRgb('#F3E2CC')))).toBeLessThan(lum(hexToRgb('#F3E2CC')));
    expect(lum(mediumTint(hexToRgb('#FFF6EC')))).toBeLessThan(lum(hexToRgb('#FFF6EC')));
  });

  it('always separates the backing from the block by a visible margin', () => {
    // Every colour used by the shipped artwork, plus white and black.
    const palette = [
      '#E2574C', '#FFF6EC', '#F3E2CC', '#7FB77E', // p01
      '#4A9DD9', '#E8963C', '#6B5744', '#A9784E', // p02
      '#6E6A66', '#F2C14E', '#E89FA8', // p03
      '#FFFFFF', '#000000', '#B23085',
    ];
    for (const hex of palette) {
      expect(contrast(hex), `${hex} tint contrast`).toBeGreaterThan(0.1);
    }
  });

  it('never returns a channel outside 0..255', () => {
    for (const hex of ['#FFFFFF', '#000000', '#FF0000', '#F3E2CC']) {
      const t = mediumTint(hexToRgb(hex));
      for (const channel of [t.r, t.g, t.b]) {
        expect(channel).toBeGreaterThanOrEqual(0);
        expect(channel).toBeLessThanOrEqual(255);
      }
    }
  });
});

describe('shade', () => {
  it('darkens for the block under-shadow that gives a block its thickness', () => {
    const base = hexToRgb('#4A9DD9');
    expect(lum(shade(base))).toBeLessThan(lum(base));
  });
});
