/**
 * CLEAN-ROOM ARTWORK for the web prototype (content_set: "safe").
 *
 * Hand-authored here, from scratch, for this repo. Nothing in this file is derived from the
 * reference app or from the four Unity demo templates - those are cross-stitch charts pulled
 * from outside sources and contain third-party copyrighted characters, so they are dev/QA
 * only (content_set: "internal") and must never reach a build that buys traffic.
 *
 * Authoring rules that keep the puzzles good (plan B.12):
 *  - every colour blob is at least 2 cells thick in both directions -> the region tiler
 *    never has to emit a 1-cell region, which is the single worst thing a Shikaku board can
 *    contain (a move that needs no thought, only a tap).
 *  - chunky, squared-off shapes, restrained palette. Each shade counts as a separate colour
 *    for the "one region never spans two colours" rule, so extra shades = crumbs.
 *  - a silhouette that is guessable early, because curiosity about what the picture will be
 *    is the strongest reason to keep playing.
 *
 * Rows read top-to-bottom (natural reading order). '.' is not part of the artwork.
 */

/** @typedef {{ id: string, name: string, palette: Record<string,string>, rows: string[] }} ArtSource */

/** @type {ArtSource[]} */
export const SAFE_ART = [
  {
    id: 'p01',
    name: 'Little Sprout',
    // Smallest picture, deliberately: the "is it legible" gate is measured on its first
    // sections, so they have to be tiny and unambiguous.
    palette: {
      R: '#E2574C', // cap
      W: '#F6D28A', // cap spots
      C: '#E7C89A', // stem
      G: '#7FB77E', // grass
    },
    rows: [
      '....RRRRRR....',
      '..RRRRRRRRRR..',
      '.RRRRRRRRRRRR.',
      'RRRRRRRRRRRRRR',
      'RRWWRRRRRRWWRR',
      'RRWWRRRRRRWWRR',
      'RRRRRRRRRRRRRR',
      '.RRRRRRRRRRRR.',
      '..CCCCCCCCCC..',
      '..CCCCCCCCCC..',
      '...CCCCCCCC...',
      '...CCCCCCCC...',
      '...CCCCCCCC...',
      '...CCCCCCCC...',
      '.GGGGGGGGGGGG.',
      '.GGGGGGGGGGGG.',
    ],
  },
  {
    id: 'p02',
    name: 'Balloon Ride',
    palette: {
      A: '#4A9DD9', // balloon, outer
      B: '#E8963C', // balloon, inner band
      E: '#6B5744', // rigging
      D: '#A9784E', // basket
    },
    rows: [
      '......AAAAAA......',
      '....AAAAAAAAAA....',
      '...AAAABBBBAAAA...',
      '..AAAABBBBBBAAAA..',
      '.AAAABBBBBBBBAAAA.',
      '.AAAABBBBBBBBAAAA.',
      'AAAABBBBBBBBBBAAAA',
      'AAAABBBBBBBBBBAAAA',
      'AAAABBBBBBBBBBAAAA',
      'AAAABBBBBBBBBBAAAA',
      '.AAAABBBBBBBBAAAA.',
      '.AAAABBBBBBBBAAAA.',
      '..AAAABBBBBBAAAA..',
      '...AAAABBBBAAAA...',
      '....AAAAAAAAAA....',
      '......AAAAAA......',
      '......EE..EE......',
      '......EE..EE......',
      '.....DDDDDDDD.....',
      '.....DDDDDDDD.....',
    ],
  },
  {
    id: 'p03',
    name: 'Sitting Cat',
    palette: {
      K: '#6E6A66', // fur
      Y: '#F2C14E', // eyes
      P: '#E89FA8', // muzzle
      W: '#E3C49B', // chest
    },
    rows: [
      '....KKKK....KKKK....',
      '....KKKK....KKKK....',
      '...KKKKKK..KKKKKK...',
      '...KKKKKKKKKKKKKK...',
      '..KKKKKKKKKKKKKKKK..',
      '..KKKKKKKKKKKKKKKK..',
      '..KKYYKKKKKKKKYYKK..',
      '..KKYYKKKKKKKKYYKK..',
      '..KKKKKKPPPPKKKKKK..',
      '..KKKKKKPPPPKKKKKK..',
      '...KKKKKKKKKKKKKK...',
      '....KKKKKKKKKKKK....',
      '.....KKKKKKKKKK.....',
      '....KKKKKKKKKKKK....',
      '...KKKKWWWWWWKKKK...',
      '..KKKKWWWWWWWWKKKK..',
      '..KKKKWWWWWWWWKKKK..',
      '..KKKKWWWWWWWWKKKK..',
      '..KKKKWWWWWWWWKKKK..',
      '..KKKKKKWWWWKKKKKK..',
      '..KKKKKKKKKKKKKKKK..',
      '..KKKKKKKKKKKKKKKK..',
    ],
  },
];
