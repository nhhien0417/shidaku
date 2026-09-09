/**
 * The game model: everything that is true about the board, and nothing about how it looks.
 *
 * Runs headless - the whole picture can be played to 100% in a unit test with no canvas, no
 * DOM and no timers. Rendering, audio and analytics are all listeners.
 *
 * TUTORIAL AND REAL PLAY SHARE THIS ONE CODE PATH. A tutorial board is just a Picture with a
 * single region, carrying isTutorial. That flag does exactly two things: it keeps the board
 * out of the funnel counter, and it lets the analytics layer emit tutorial_step_complete
 * instead of section_complete. Everything else - drag, match, reject, wave, sound - is
 * literally the same code, which is the only way the tutorial can honestly teach the game.
 */

import { Emitter } from '../util/emitter';
import { comboTierFor, progressFraction } from './progress';
import { matchRegion, nearestClueArea, pickHintRegion } from './rules';
import type { BoardQuery, Picture, Rect, Region, Section, SolveSource } from './types';

export const HINT_COOLDOWN_MS = 5000;
/** How long after a rejection a correct placement still counts as "recovered" (spec A.4). */
export const REJECT_RECOVERY_WINDOW_MS = 15000;

export type Phase =
  | 'boot'
  | 'play'
  | 'overview'
  | 'sectionClear'
  | 'pictureClear'
  | 'contentEnd';

export interface SectionStats {
  rejects: number;
  hints: number;
  maxCombo: number;
  startedAt: number;
}

export interface GameEvents {
  pictureStart: { picture: Picture; pictureIndex: number; isTutorial: boolean };
  sectionStart: { section: Section; funnelLevel: number; isTutorial: boolean };
  regionSolved: {
    region: Region;
    source: SolveSource;
    funnelLevel: number;
    comboAfter: number;
    msSinceSectionStart: number;
    isTutorial: boolean;
  };
  regionRejected: {
    rect: Rect;
    funnelLevel: number;
    nearestClueArea: number | null;
    msSinceSectionStart: number;
    isTutorial: boolean;
  };
  rejectRecovered: { funnelLevel: number; ms: number; rejectsInRow: number };
  comboTier: { tier: number; combo: number };
  progress: { fraction: number };
  sectionComplete: {
    section: Section;
    funnelLevel: number;
    durationMs: number;
    stats: SectionStats;
    isTutorial: boolean;
    /** 1-based index of this section within the current picture. */
    stepInPicture: number;
  };
  pictureComplete: {
    picture: Picture;
    pictureIndex: number;
    durationMs: number;
    rejectsTotal: number;
    hintsTotal: number;
    isTutorial: boolean;
  };
  hintBlocked: { remainingMs: number; funnelLevel: number };
  contentEnd: { sectionsSolved: number };
}

export type AttemptResult =
  | { ok: true; region: Region; sectionComplete: boolean }
  /** A genuine wrong answer: shake it, count it, reset the combo. */
  | { ok: false; ignored: false; nearestClueArea: number | null }
  /**
   * Not judged at all - input was locked (a celebration is mid-flight) or there is no
   * section. Distinct from a wrong answer on purpose: it must not count as a mistake, and
   * the caller still has to put any risen hover blocks back down, because neither the accept
   * nor the reject animation is going to run.
   */
  | { ok: false; ignored: true };

export type HintResult =
  | { ok: true; region: Region; sectionComplete: boolean }
  | { ok: false; blockedMs: number };

export class Game extends Emitter<GameEvents> {
  phase: Phase = 'boot';
  /** Set while a wave/celebration is playing, or the overview is open. */
  inputLocked = false;

  picture: Picture | null = null;
  pictureIndex = -1;
  isTutorial = false;
  sectionIndex = 0;

  combo = 0;
  comboTier = 0;
  sessionMaxCombo = 0;

  /** Sections completed in REAL play, across every picture. This is the funnel's "level". */
  sectionsCompleted = 0;
  regionsSolvedTotal = 0;
  rejectsTotal = 0;
  hintsTotal = 0;
  overviewOpens = 0;
  selfContinueCount = 0;

  private sectionStats: SectionStats = { rejects: 0, hints: 0, maxCombo: 0, startedAt: 0 };
  private pictureStartedAt = 0;
  private pictureRejects = 0;
  private pictureHints = 0;
  private lastRejectAt: number | null = null;
  private rejectsInRow = 0;
  private hintReadyAt = 0;
  private readonly now: () => number;

  /** The read-only view handed to rules.ts. Built in the ctor so it can close over `this`. */
  readonly query: BoardQuery;

  constructor(now: () => number = () => performance.now()) {
    super();
    this.now = now;

    const game = this;
    this.query = {
      get w() {
        return game.picture?.w ?? 0;
      },
      get h() {
        return game.picture?.h ?? 0;
      },
      sectionAt: (x, y) => {
        const p = game.picture;
        if (!p || x < 0 || y < 0 || x >= p.w || y >= p.h) return -1;
        return p.sectionOf[y * p.w + x];
      },
      isSolved: (x, y) => {
        const p = game.picture;
        if (!p || x < 0 || y < 0 || x >= p.w || y >= p.h) return false;
        return p.solved[y * p.w + x] === 1;
      },
    };
  }

  get section(): Section | null {
    return this.picture?.sections[this.sectionIndex] ?? null;
  }

  /** The funnel bucket the player is in right now. Tutorial boards never advance it. */
  get funnelLevel(): number {
    return this.isTutorial ? 0 : this.sectionsCompleted + 1;
  }

  get progress(): number {
    return this.picture ? progressFraction(this.picture) : 0;
  }

  hintRemainingMs(): number {
    return Math.max(0, this.hintReadyAt - this.now());
  }

  // ---------------------------------------------------------------- lifecycle

  beginPicture(picture: Picture, pictureIndex: number, isTutorial: boolean): void {
    this.picture = picture;
    this.pictureIndex = pictureIndex;
    this.isTutorial = isTutorial;
    this.sectionIndex = 0;
    this.pictureStartedAt = this.now();
    this.pictureRejects = 0;
    this.pictureHints = 0;
    this.phase = 'play';
    this.inputLocked = false;
    this.emit('pictureStart', { picture, pictureIndex, isTutorial });
    this.startSection();
  }

  private startSection(): void {
    const section = this.section;
    if (!section) return;
    this.sectionStats = { rejects: 0, hints: 0, maxCombo: 0, startedAt: this.now() };
    this.emit('sectionStart', { section, funnelLevel: this.funnelLevel, isTutorial: this.isTutorial });
  }

  /**
   * Called by the app once the section-clear celebration has finished playing. Advances to
   * the next section, or reports the picture done. Split out on purpose: the model must not
   * own animation timing, and the celebration must not be skippable by a fast solve.
   */
  advanceSection(): void {
    const picture = this.picture;
    if (!picture) return;

    if (this.sectionIndex < picture.sections.length - 1) {
      this.sectionIndex++;
      this.phase = 'play';
      this.inputLocked = false;
      this.startSection();
      return;
    }

    this.phase = 'pictureClear';
    this.inputLocked = true;
    this.emit('pictureComplete', {
      picture,
      pictureIndex: this.pictureIndex,
      durationMs: this.now() - this.pictureStartedAt,
      rejectsTotal: this.pictureRejects,
      hintsTotal: this.pictureHints,
      isTutorial: this.isTutorial,
    });
  }

  endContent(): void {
    this.phase = 'contentEnd';
    this.inputLocked = true;
    this.emit('contentEnd', { sectionsSolved: this.sectionsCompleted });
  }

  // ---------------------------------------------------------------- moves

  /**
   * The whole game, in one method. An exact match against the one authored answer, or an
   * immediate rejection - even for a rectangle that classic Shikaku would happily accept.
   */
  attemptRect(rect: Rect): AttemptResult {
    const section = this.section;
    if (!section || this.inputLocked) return { ok: false, ignored: true };

    const match = matchRegion(section, rect);
    const t = this.now();
    const msSinceSectionStart = t - this.sectionStats.startedAt;

    if (!match) {
      this.combo = 0;
      this.comboTier = 0;
      this.rejectsTotal++;
      this.pictureRejects++;
      this.sectionStats.rejects++;
      this.rejectsInRow++;
      this.lastRejectAt = t;
      const near = nearestClueArea(section, rect);
      this.emit('regionRejected', {
        rect,
        funnelLevel: this.funnelLevel,
        nearestClueArea: near,
        msSinceSectionStart,
        isTutorial: this.isTutorial,
      });
      return { ok: false, ignored: false, nearestClueArea: near };
    }

    // Recovery after a rejection - Shidaku's stand-in for "retried after a loss", since the
    // game has no loss state at all (plan A.4).
    if (this.lastRejectAt !== null && t - this.lastRejectAt <= REJECT_RECOVERY_WINDOW_MS) {
      this.emit('rejectRecovered', {
        funnelLevel: this.funnelLevel,
        ms: t - this.lastRejectAt,
        rejectsInRow: this.rejectsInRow,
      });
    }
    this.lastRejectAt = null;
    this.rejectsInRow = 0;

    this.combo++;
    if (this.combo > this.sessionMaxCombo) this.sessionMaxCombo = this.combo;
    if (this.combo > this.sectionStats.maxCombo) this.sectionStats.maxCombo = this.combo;
    const tier = comboTierFor(this.combo);
    if (tier > this.comboTier) {
      this.comboTier = tier;
      this.emit('comboTier', { tier, combo: this.combo });
    }

    return this.commitRegion(match, 'drag', msSinceSectionStart);
  }

  /** Solves the largest remaining region of the current section. Never touches combo. */
  useHint(): HintResult {
    const section = this.section;
    if (!section || this.inputLocked) return { ok: false, blockedMs: 0 };

    const remaining = this.hintRemainingMs();
    if (remaining > 0) {
      this.emit('hintBlocked', { remainingMs: remaining, funnelLevel: this.funnelLevel });
      return { ok: false, blockedMs: remaining };
    }

    const region = pickHintRegion(section);
    if (!region) return { ok: false, blockedMs: 0 };

    this.hintReadyAt = this.now() + HINT_COOLDOWN_MS;
    this.hintsTotal++;
    this.pictureHints++;
    this.sectionStats.hints++;
    return this.commitRegion(region, 'hint', this.now() - this.sectionStats.startedAt);
  }

  private commitRegion(
    region: Region,
    source: SolveSource,
    msSinceSectionStart: number,
  ): { ok: true; region: Region; sectionComplete: boolean } {
    const picture = this.picture!;
    const section = this.section!;

    region.solved = true;
    region.solvedBy = source;
    section.unsolvedCount--;
    picture.solvedRegions++;
    this.regionsSolvedTotal++;

    for (let y = region.rect.y; y < region.rect.y + region.rect.h; y++) {
      for (let x = region.rect.x; x < region.rect.x + region.rect.w; x++) {
        picture.solved[y * picture.w + x] = 1;
      }
    }

    this.emit('regionSolved', {
      region,
      source,
      funnelLevel: this.funnelLevel,
      comboAfter: this.combo,
      msSinceSectionStart,
      isTutorial: this.isTutorial,
    });
    this.emit('progress', { fraction: this.progress });

    const done = section.unsolvedCount === 0;
    if (done) {
      this.phase = 'sectionClear';
      this.inputLocked = true;
      const funnelLevel = this.funnelLevel;
      if (!this.isTutorial) this.sectionsCompleted++;
      this.emit('sectionComplete', {
        section,
        funnelLevel,
        durationMs: this.now() - this.sectionStats.startedAt,
        stats: this.sectionStats,
        isTutorial: this.isTutorial,
        stepInPicture: this.sectionIndex + 1,
      });
    }

    return { ok: true, region, sectionComplete: done };
  }

  // ---------------------------------------------------------------- overview

  enterOverview(): void {
    if (this.phase !== 'play') return;
    this.phase = 'overview';
    this.inputLocked = true;
    this.overviewOpens++;
  }

  exitOverview(): void {
    if (this.phase !== 'overview') return;
    this.phase = 'play';
    this.inputLocked = false;
  }

  noteSelfContinue(): void {
    this.selfContinueCount++;
  }
}
