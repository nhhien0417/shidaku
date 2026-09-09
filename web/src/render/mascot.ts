/**
 * The mascot, drawn from a pixel map in code - no image request, no asset budget, and
 * unambiguously our own drawing. (The reference game's capybara is theirs; this is a
 * generic round creature authored here.)
 *
 * It exists because "a section finishes and the camera just slides" is, per the spec, far
 * too quiet for the amount of work the player just did. One bouncing character and a line
 * of praise is the cheapest fix available for the biggest gap in the demo.
 */

/**
 * Sixteen by sixteen, drawn round on purpose: the first pass had a crenellated top and a row
 * of separate toes, which read as a blocky monster rather than something friendly. The head
 * now domes in three steps, the ears taper into it, and the body narrows to two paws.
 */
const MAP = [
  '...KK......KK...',
  '..KKKK....KKKK..',
  '..KKKKKKKKKKKK..',
  '.KKKKKKKKKKKKKK.',
  'KKKKKKKKKKKKKKKK',
  'KKKWWKKKKKKWWKKK',
  'KKKWWKKKKKKWWKKK',
  'KKKKKKKKKKKKKKKK',
  'KKKKKKPPPPKKKKKK',
  'KKKKKKPPPPKKKKKK',
  '.KKKKKKKKKKKKKK.',
  '..KKKKKKKKKKKK..',
  '...KKKKKKKKKK...',
  '...KKKKKKKKKK...',
  '..KKK......KKK..',
  '..KKK......KKK..',
];

const PALETTE: Record<string, string> = {
  K: '#C29A76', // warm fur
  W: '#FFF9F0', // eyes
  P: '#E89FA8', // muzzle
};

export const MASCOT_COLS = MAP[0].length;
export const MASCOT_ROWS = MAP.length;

/**
 * Draws the mascot with its bottom-centre at (x, y). `px` is the size of one mascot pixel.
 * Cheap: at most 256 fillRects, and it is on screen for about a second at a time.
 */
export function drawMascot(
  ctx: CanvasRenderingContext2D,
  x: number,
  y: number,
  px: number,
  alpha = 1,
): void {
  if (alpha <= 0) return;
  const prevAlpha = ctx.globalAlpha;
  ctx.globalAlpha = prevAlpha * alpha;

  const left = x - (MASCOT_COLS * px) / 2;
  const top = y - MASCOT_ROWS * px;
  let lastColor = '';

  for (let row = 0; row < MASCOT_ROWS; row++) {
    for (let col = 0; col < MASCOT_COLS; col++) {
      const ch = MAP[row][col];
      if (ch === '.') continue;
      const color = PALETTE[ch];
      if (color !== lastColor) {
        ctx.fillStyle = color;
        lastColor = color;
      }
      // +0.5 on the size closes the hairline seams DPR rounding leaves between fillRects.
      ctx.fillRect(left + col * px, top + row * px, px + 0.5, px + 0.5);
    }
  }

  ctx.globalAlpha = prevAlpha;
}

export const SECTION_PRAISE = [
  'Nice work!',
  'Looking good!',
  'Keep going!',
  'That one fit!',
  'Coming together!',
];
