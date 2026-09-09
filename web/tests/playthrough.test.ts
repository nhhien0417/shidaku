import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { describe, expect, it } from 'vitest';
import { buildPicture } from '../src/core/loader';
import { Game, HINT_COOLDOWN_MS } from '../src/core/state';
import type { PictureJson } from '../src/core/types';

const PUZZLES = resolve(__dirname, '../public/puzzles');

function readPicture(id: string): PictureJson {
  return JSON.parse(readFileSync(resolve(PUZZLES, `${id}.json`), 'utf8')) as PictureJson;
}

function catalog(): { pictures: string[]; content_set: string } {
  return JSON.parse(readFileSync(resolve(PUZZLES, 'catalog.json'), 'utf8'));
}

/** A clock the test drives by hand, so nothing here depends on wall time. */
function fakeClock() {
  const state = { t: 0 };
  return { now: () => state.t, advance: (ms: number) => (state.t += ms) };
}

/**
 * Whether the puzzles currently on disk are the publishable set.
 *
 * The correctness checks below apply to any content - if a section cannot be completed, the
 * player is stuck whatever the artwork's provenance. The QUALITY rules (no 1-cell regions,
 * at least 3 regions per section, 8 cells wide) only apply to the clean-room set that is
 * actually allowed to ship.
 *
 * The Unity demo's own puzzles do not satisfy them: its generator emits 1-cell regions and
 * sections up to 10 cells wide. They load and play correctly through this pipeline, which is
 * what the internal set is for - checking the pipeline against real Unity-authored puzzles.
 */
const isPublishable = catalog().content_set === 'safe';
const describeQuality = isPublishable ? describe : describe.skip;

describe('authored content', () => {
  it('loads every catalogued picture with no fatal errors', () => {
    for (const id of catalog().pictures) {
      const { picture } = buildPicture(readPicture(id));
      expect(picture.sections.length).toBeGreaterThan(0);
      expect(picture.totalRegions).toBeGreaterThan(0);
    }
  });

  it('is free of loader warnings when it is the publishable set', () => {
    if (!isPublishable) return;
    for (const id of catalog().pictures) {
      expect(buildPicture(readPicture(id)).warnings, `${id} warnings`).toEqual([]);
    }
  });

  it('ships at least 7 sections so the funnel has 7 measurable levels', () => {
    const total = catalog()
      .pictures.map((id) => buildPicture(readPicture(id)).picture.sections.length)
      .reduce((a, b) => a + b, 0);
    expect(total).toBeGreaterThanOrEqual(7);
  });

});

describeQuality('publishable content quality', () => {
  it('never contains a 1-cell region, and never a region over 15 cells', () => {
    for (const id of catalog().pictures) {
      const { picture } = buildPicture(readPicture(id));
      for (const section of picture.sections) {
        for (const region of section.regions) {
          expect(region.area, `${id} region at ${region.rect.x},${region.rect.y}`).toBeGreaterThan(1);
          expect(region.area).toBeLessThanOrEqual(15);
        }
      }
    }
  });

  it('gives every section at least 3 regions, since a section is a funnel step', () => {
    for (const id of catalog().pictures) {
      const { picture } = buildPicture(readPicture(id));
      for (const section of picture.sections) {
        expect(section.regions.length, `${id}/s${section.index}`).toBeGreaterThanOrEqual(3);
      }
    }
  });

  it('keeps every section within 9 cells wide, so cells stay >= 40 CSS px on a phone', () => {
    for (const id of catalog().pictures) {
      const { picture } = buildPicture(readPicture(id));
      for (const section of picture.sections) {
        expect(section.bounds.w, `${id}/s${section.index}`).toBeLessThanOrEqual(9);
      }
    }
  });

  it('keeps every region monochrome - a solved rectangle is always one solid colour', () => {
    for (const id of catalog().pictures) {
      const { picture } = buildPicture(readPicture(id));
      for (const section of picture.sections) {
        for (const region of section.regions) {
          const first = picture.pixels[region.rect.y * picture.w + region.rect.x];
          for (let y = region.rect.y; y < region.rect.y + region.rect.h; y++) {
            for (let x = region.rect.x; x < region.rect.x + region.rect.w; x++) {
              expect(picture.pixels[y * picture.w + x]).toBe(first);
            }
          }
        }
      }
    }
  });
});

describe('headless playthrough', () => {
  it('solves a whole picture by drag alone and lands exactly on 100%', () => {
    const clock = fakeClock();
    const game = new Game(clock.now);
    const { picture } = buildPicture(readPicture('p01'));

    const solvedEvents: number[] = [];
    const sectionCompletes: number[] = [];
    let pictureComplete = 0;
    game.on('regionSolved', (e) => solvedEvents.push(e.funnelLevel));
    game.on('sectionComplete', (e) => sectionCompletes.push(e.funnelLevel));
    game.on('pictureComplete', () => pictureComplete++);

    game.beginPicture(picture, 0, false);

    for (let guard = 0; guard < 500; guard++) {
      const section = game.section;
      if (!section) break;
      const next = section.regions.find((r) => !r.solved);
      if (next) {
        clock.advance(1200);
        const result = game.attemptRect(next.rect);
        expect(result.ok).toBe(true);
        if (result.ok && result.sectionComplete) game.advanceSection();
        continue;
      }
      break;
    }

    expect(pictureComplete).toBe(1);
    expect(game.progress).toBe(1);
    expect(picture.solvedRegions).toBe(picture.totalRegions);
    expect(sectionCompletes.length).toBe(picture.sections.length);
    // funnel_level counts sections completed, cumulatively: 1, 2, 3, ...
    expect(sectionCompletes).toEqual(picture.sections.map((_, i) => i + 1));
    expect(solvedEvents.length).toBe(picture.totalRegions);
  });

  it('carries funnel_level across pictures instead of restarting per picture', () => {
    const clock = fakeClock();
    const game = new Game(clock.now);
    const first = buildPicture(readPicture('p01')).picture;
    const second = buildPicture(readPicture('p02')).picture;
    const seen: number[] = [];
    game.on('sectionComplete', (e) => seen.push(e.funnelLevel));

    for (const [index, picture] of [first, second].entries()) {
      game.beginPicture(picture, index, false);
      for (let guard = 0; guard < 800; guard++) {
        const section = game.section;
        if (!section) break;
        const next = section.regions.find((r) => !r.solved);
        if (!next) break;
        clock.advance(900);
        const result = game.attemptRect(next.rect);
        if (result.ok && result.sectionComplete) game.advanceSection();
      }
    }

    const expected = first.sections.length + second.sections.length;
    expect(seen.length).toBe(expected);
    expect(seen).toEqual(Array.from({ length: expected }, (_, i) => i + 1));
    expect(game.sectionsCompleted).toBe(expected);
  });

  it('leaves funnel_level alone for tutorial boards', () => {
    const clock = fakeClock();
    const game = new Game(clock.now);
    const { picture } = buildPicture(readPicture('p01'));
    const levels: number[] = [];
    game.on('sectionComplete', (e) => levels.push(e.funnelLevel));

    game.beginPicture(picture, 0, true);
    const section = game.section!;
    for (const region of section.regions) {
      clock.advance(500);
      game.attemptRect(region.rect);
    }

    expect(levels).toEqual([0]);
    expect(game.sectionsCompleted).toBe(0);
  });
});

describe('rejection, combo and recovery', () => {
  function setup() {
    const clock = fakeClock();
    const game = new Game(clock.now);
    const { picture } = buildPicture(readPicture('p01'));
    game.beginPicture(picture, 0, false);
    return { clock, game, picture };
  }

  it('rejects a wrong rect without changing the board', () => {
    const { game } = setup();
    const before = game.progress;
    const result = game.attemptRect({ x: 0, y: 0, w: 1, h: 1 });
    expect(result.ok).toBe(false);
    expect(game.progress).toBe(before);
    expect(game.rejectsTotal).toBe(1);
  });

  it('resets combo on a rejection and rebuilds it on the next correct move', () => {
    const { clock, game } = setup();
    const section = game.section!;
    // Needs a section with room for two solves plus a third move, otherwise the second solve
    // completes the section and locks input before the rejection can be tested.
    expect(section.regions.length).toBeGreaterThanOrEqual(3);
    clock.advance(100);
    game.attemptRect(section.regions[0].rect);
    clock.advance(100);
    game.attemptRect(section.regions[1].rect);
    expect(game.combo).toBe(2);

    clock.advance(100);
    game.attemptRect({ x: 99, y: 99, w: 1, h: 1 });
    expect(game.combo).toBe(0);

    clock.advance(100);
    game.attemptRect(section.regions[2].rect);
    expect(game.combo).toBe(1);
  });

  it('fires rejectRecovered only inside the 15s window', () => {
    const { clock, game } = setup();
    const section = game.section!;
    const recovered: number[] = [];
    game.on('rejectRecovered', (e) => recovered.push(e.ms));

    game.attemptRect({ x: 99, y: 99, w: 1, h: 1 });
    clock.advance(4000);
    game.attemptRect(section.regions[0].rect);
    expect(recovered).toEqual([4000]);

    game.attemptRect({ x: 99, y: 99, w: 1, h: 1 });
    clock.advance(16000);
    game.attemptRect(section.regions[1].rect);
    expect(recovered).toEqual([4000]); // too late, not counted
  });

  it('emits each combo tier once, in order', () => {
    const clock = fakeClock();
    const game = new Game(clock.now);
    const { picture } = buildPicture(readPicture('p03'));
    const tiers: number[] = [];
    game.on('comboTier', (e) => tiers.push(e.tier));

    game.beginPicture(picture, 0, false);
    for (let guard = 0; guard < 40; guard++) {
      const section = game.section;
      if (!section) break;
      const next = section.regions.find((r) => !r.solved);
      if (!next) break;
      clock.advance(700);
      const result = game.attemptRect(next.rect);
      if (result.ok && result.sectionComplete) game.advanceSection();
    }

    expect(tiers.slice(0, 3)).toEqual([1, 2, 3]);
    expect(new Set(tiers).size).toBe(tiers.length);
  });
});

describe('hint', () => {
  it('solves the largest remaining region, without touching combo', () => {
    const clock = fakeClock();
    const game = new Game(clock.now);
    const { picture } = buildPicture(readPicture('p02'));
    game.beginPicture(picture, 0, false);

    const largest = Math.max(...game.section!.regions.map((r) => r.area));
    const result = game.useHint();
    expect(result.ok).toBe(true);
    if (result.ok) {
      expect(result.region.area).toBe(largest);
      expect(result.region.solvedBy).toBe('hint');
    }
    expect(game.combo).toBe(0);
    expect(game.hintsTotal).toBe(1);
  });

  it('blocks a second hint for 5 seconds and reports the remaining time', () => {
    const clock = fakeClock();
    const game = new Game(clock.now);
    const { picture } = buildPicture(readPicture('p02'));
    game.beginPicture(picture, 0, false);

    game.useHint();
    clock.advance(2000);
    const blocked = game.useHint();
    expect(blocked.ok).toBe(false);
    if (!blocked.ok) expect(blocked.blockedMs).toBe(HINT_COOLDOWN_MS - 2000);

    clock.advance(3000);
    expect(game.useHint().ok).toBe(true);
  });

  it('can finish a whole section on hints alone', () => {
    const clock = fakeClock();
    const game = new Game(clock.now);
    const { picture } = buildPicture(readPicture('p01'));
    let completed = 0;
    game.on('sectionComplete', () => completed++);
    game.beginPicture(picture, 0, false);

    for (let guard = 0; guard < 40 && completed === 0; guard++) {
      clock.advance(HINT_COOLDOWN_MS);
      game.useHint();
    }
    expect(completed).toBe(1);
  });
});

describe('input locking', () => {
  it('ignores moves while a celebration is playing', () => {
    const clock = fakeClock();
    const game = new Game(clock.now);
    const { picture } = buildPicture(readPicture('p01'));
    game.beginPicture(picture, 0, false);

    const section = game.section!;
    for (const region of section.regions) {
      clock.advance(400);
      game.attemptRect(region.rect);
    }
    expect(game.phase).toBe('sectionClear');
    expect(game.inputLocked).toBe(true);

    const rejected = game.attemptRect({ x: 0, y: 0, w: 2, h: 1 });
    expect(rejected.ok).toBe(false);
    // Reported as "ignored", not as a wrong answer: it must not count as a mistake, and the
    // renderer needs to know that neither the accept nor the reject wave will run.
    if (!rejected.ok) expect(rejected.ignored).toBe(true);
    expect(game.rejectsTotal).toBe(0);

    const realMiss = new Game(clock.now);
    const fresh = buildPicture(readPicture('p01')).picture;
    realMiss.beginPicture(fresh, 0, false);
    const miss = realMiss.attemptRect({ x: 0, y: 0, w: 1, h: 1 });
    expect(miss.ok).toBe(false);
    if (!miss.ok) expect(miss.ignored).toBe(false);
    expect(realMiss.rejectsTotal).toBe(1);

    game.advanceSection();
    expect(game.phase).toBe('play');
    expect(game.inputLocked).toBe(false);
  });
});
