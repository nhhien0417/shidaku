/**
 * The conductor: wires model, board, camera, sound, HUD and analytics together, and owns the
 * one thing the model deliberately does not - timing.
 *
 * Sequencing lives here because the model must stay headless and testable, and because the
 * celebration beats are a design decision, not a rule of the game.
 */

import { analytics, analyticsConfigFromEnv } from './analytics';
import { mountDebugOverlay } from './analytics/debug';
import { loadCatalog, loadPicture, PuzzleFatalError, buildPicture } from './core/loader';
import { progressPercent } from './core/progress';
import { clampDragTo, makeRect, rowMajorCells } from './core/rules';
import { Game } from './core/state';
import type { CellPos, Picture, Rect } from './core/types';
import { attachPointer } from './input/pointer';
import { Sfx } from './audio/sfx';
import { BoardView, POP_MS } from './render/board';
import { Camera, CELL, OVERVIEW_TWEEN_MS, SECTION_TWEEN_MS } from './render/camera';
import { Effects } from './render/effects';
import { comboTierInfo } from './core/progress';
import { SECTION_PRAISE } from './render/mascot';
import { Renderer } from './render/renderer';
import { Hud } from './ui/hud';
import { GhostHand, TUTORIAL_STEPS } from './ui/tutorial';
import {
  connectionType,
  deviceTier,
  isInAppBrowser,
  newSessionId,
  queryFlag,
  readSafeArea,
  vibrate,
} from './util/device';

/** Beat between the last block landing and the camera moving on (Board.cs: 0.2s + mascot). */
const SECTION_CLEAR_HOLD_MS = 900;
const TUTORIAL_STEP_GAP_MS = 520;
/** How long the player must stay after a reward for it to count as choosing to continue. */
const SELF_CONTINUE_MS = 10000;
const INSTALL_BANNER_MS = 6000;

interface QueueEntry {
  id: string;
  isTutorial: boolean;
  tutorialIndex: number;
}

export class App {
  private readonly renderer: Renderer;
  private readonly camera = new Camera();
  private readonly board = new BoardView();
  private readonly effects = new Effects();
  private readonly hud = new Hud();
  private readonly sfx = new Sfx();
  private readonly game: Game;
  private readonly ghost = new GhostHand();

  private queue: QueueEntry[] = [];
  private queueIndex = -1;
  private pictures = new Map<string, Picture>();
  private realPictureNumber = 0;

  private dragging = false;
  private dragStart: CellPos = { x: 0, y: 0 };
  private dragCurrent: CellPos = { x: 0, y: 0 };

  private pendingTier: { tier: number; combo: number } | null = null;
  private overviewOpenedAt = 0;
  private installShownAt = 0;
  private installBannerTimer = 0;
  private installBannerUsed = false;
  private installBannerPending = false;
  private sessionEnded = false;
  private firstInputTracked = false;
  private ttffMs = 0;
  private readonly debug: boolean;

  constructor() {
    this.debug = queryFlag('debug') === '1';
    const canvas = document.getElementById('board') as HTMLCanvasElement;
    this.renderer = new Renderer(canvas, readSafeArea());
    this.game = new Game();

    this.renderer.onFrame = (now, dt) => this.frame(now, dt);
    this.renderer.onDraw = (ctx, vp) => this.draw(ctx, vp);
    this.renderer.onResize = () => {
      this.camera.reanchor(this.renderer.vp);
      this.reframe(true);
    };

    this.wireGame();
    this.wireHud();
    this.wireInput();
    this.wireSessionEnd();
  }

  // ---------------------------------------------------------------- boot

  async boot(): Promise<void> {
    const loadStart = performance.now();
    try {
      const catalog = await loadCatalog();

      if (catalog.content_set === 'internal') {
        // Loud on purpose. The internal set is the four Unity demo templates, which contain
        // third-party copyrighted characters; it exists for dev/QA only and must never be
        // the thing an ad points at.
        console.warn('[shidaku] content_set=internal - DEV/QA ONLY, not publishable');
      }

      this.queue = [
        ...TUTORIAL_STEPS.map((_, i) => ({ id: `tut${i + 1}`, isTutorial: true, tutorialIndex: i })),
        ...catalog.pictures.map((id) => ({ id, isTutorial: false, tutorialIndex: -1 })),
      ];

      for (const [i, step] of TUTORIAL_STEPS.entries()) {
        const { picture } = buildPicture(step.json);
        this.pictures.set(`tut${i + 1}`, picture);
      }

      // Only the first real picture is fetched up front; the rest load during play, so the
      // first-load request count stays at four.
      const firstReal = catalog.pictures[0];
      if (!firstReal) throw new PuzzleFatalError('catalog has no pictures');
      await this.ensurePicture(firstReal);

      analytics.track('assets_loaded', { load_ms: Math.round(performance.now() - loadStart) });

      this.hud.setMuted(this.sfx.muted);
      this.renderer.start();

      /**
       * Sinks start here, not inside the frame loop.
       *
       * They must not load before this point (they would compete with time-to-interactive,
       * which is the whole reason page_open is queued). But they must not wait on
       * requestAnimationFrame either: a tab that starts backgrounded gets its rAF throttled,
       * and hanging SDK startup off the first frame meant a session could reach first_input
       * with nothing initialised. In a visible tab these two moments are ~16ms apart.
       */
      analytics.start(analyticsConfigFromEnv(this.superProps(), this.debug));
      if (!this.sfx.available) analytics.track('audio_unavailable', {});

      let startIndex = 0;
      if (import.meta.env.DEV) {
        // Dev-only jump: ?level=N starts at the Nth section, tutorial skipped. Guarded by
        // import.meta.env.DEV so esbuild strips it from the production bundle entirely - a
        // player who found this flag would silently poison the funnel.
        const wanted = Number(queryFlag('level') ?? '0');
        if (wanted > 0) startIndex = TUTORIAL_STEPS.length;
      }
      this.startEntry(startIndex);

      if (this.debug) {
        mountDebugOverlay(() => ({
          fps: this.renderer.fps,
          ttff: Math.round(this.ttffMs),
          level: this.game.funnelLevel,
          phase: this.game.phase,
          combo: this.game.combo,
          set: catalog.content_set,
          build: __BUILD_VER__,
        }));
      }
    } catch (err) {
      const message = err instanceof Error ? err.message : String(err);
      analytics.track('error', { message, where: 'boot' });
      this.hud.showBootError('Could not load the puzzles. Please try again.');
      console.error(err);
    }
  }

  private async ensurePicture(id: string): Promise<Picture> {
    const cached = this.pictures.get(id);
    if (cached) return cached;
    const { picture, warnings } = await loadPicture(id);
    for (const warning of warnings) console.warn(`[content] ${warning}`);
    this.pictures.set(id, picture);
    return picture;
  }

  private startEntry(index: number): void {
    const entry = this.queue[index];
    if (!entry) {
      this.endContent();
      return;
    }
    this.queueIndex = index;

    const picture = this.pictures.get(entry.id);
    if (!picture) {
      // Not yet resolved (a later picture); fetch, then re-enter.
      void this.ensurePicture(entry.id).then(() => this.startEntry(index));
      return;
    }

    this.hud.hideWin();
    this.effects.clear();
    this.board.setPicture(picture);

    if (entry.isTutorial) {
      const step = TUTORIAL_STEPS[entry.tutorialIndex];
      this.hud.setTutorialStep(entry.tutorialIndex + 1, step.text, step.note);
      this.hud.setControlsVisible(false);
      this.hud.setLevelLabel('How to play');
      this.ghost.setStep(step);
    } else {
      this.realPictureNumber++;
      this.hud.hideTutorial();
      this.hud.setControlsVisible(true);
      this.hud.setLevelLabel(`Picture ${this.realPictureNumber}`);
      this.ghost.setStep(null);
      this.hud.updateMiniMap(picture);
      // Prefetch the next picture so a picture change never shows a loading gap.
      const next = this.queue[index + 1];
      if (next && !next.isTutorial) void this.ensurePicture(next.id).catch(() => {});
    }

    this.game.beginPicture(picture, index, entry.isTutorial);
    this.board.refreshActivation(0, { fadeClues: false });
    this.reframe(true);
    this.renderer.markDirty();
  }

  private endContent(): void {
    this.hideInstallBanner();
    this.game.endContent();
    this.hud.hideWin();
    this.hud.showEnd();
    this.hud.setControlsVisible(false);
    this.trackInstallShown('content_end');
    analytics.track('content_exhausted', {
      total_duration_ms: Math.round(performance.now()),
      sections_solved: this.game.sectionsCompleted,
    });
  }

  // ---------------------------------------------------------------- model -> world

  private wireGame(): void {
    this.game.on('pictureStart', ({ picture, isTutorial }) => {
      if (isTutorial) return; // tutorial boards are steps, not pictures
      analytics.track('picture_start', {
        picture_id: picture.id,
        total_sections: picture.sections.length,
        total_regions: picture.totalRegions,
      });
    });

    this.game.on('sectionStart', ({ section, funnelLevel, isTutorial }) => {
      this.board.refreshActivation(this.game.sectionIndex, { fadeClues: this.game.sectionIndex > 0 });
      this.reframe(false);
      if (this.installBannerPending) {
        this.installBannerPending = false;
        this.showInstallBanner();
      }
      analytics.track('section_start', {
        funnel_level: funnelLevel,
        picture_id: this.game.picture?.id ?? '',
        section_index: section.index,
        w: section.bounds.w,
        h: section.bounds.h,
        region_count: section.regions.length,
        is_tutorial: isTutorial,
      });
    });

    this.game.on('regionSolved', (e) => {
      const picture = this.game.picture;
      if (!picture) return;

      this.board.popRegion(e.region, e.source === 'hint');
      const cells = rowMajorCells(e.region.rect);
      const step = Math.min(POP_MS * 0.5, 1100 / Math.max(1, cells.length));
      cells.forEach((_, i) => this.sfx.pop(i, i * step));
      vibrate(8);

      const centre = this.board.regionScreenCentre(this.camera, e.region.rect);
      const paletteIdx = picture.pixels[e.region.rect.y * picture.w + e.region.rect.x];
      this.effects.sparkleBurst(centre.x, centre.y, centre.radius, this.camera.cellPx, picture.palette[paletteIdx]);

      if (this.pendingTier) {
        const info = comboTierInfo(this.pendingTier.tier);
        if (info) {
          this.effects.comboPopup(centre.x, centre.y, info.label, this.pendingTier.combo, info.color);
          this.sfx.comboTier(this.pendingTier.tier);
        }
        this.pendingTier = null;
      }

      this.hud.flashProgress(progressPercent(picture));
      if (!e.isTutorial) this.hud.updateMiniMap(picture);

      analytics.track('region_solved', {
        funnel_level: e.funnelLevel,
        source: e.source,
        area: e.region.area,
        w: e.region.rect.w,
        h: e.region.rect.h,
        t_since_section_start_ms: Math.round(e.msSinceSectionStart),
        combo_after: e.comboAfter,
        is_tutorial: e.isTutorial,
      });

      /**
       * First purposeful action. A tutorial drag counts: the metric exists to separate
       * someone playing from someone tapping around to work out what the screen is, and
       * completing tutorial step 1 is exactly that signal. Hints never count - the system
       * did the thinking.
       */
      if (e.source === 'drag') {
        analytics.track('first_meaningful_action', {
          t_from_first_input_ms: Math.round(performance.now() - this.firstInputAt),
        });
      }

      this.renderer.markDirty();
    });

    this.game.on('regionRejected', (e) => {
      this.board.rejectRegion(e.rect);
      this.sfx.reject();
      vibrate(30);
      analytics.track('region_rejected', {
        funnel_level: e.funnelLevel,
        area_drawn: e.rect.w * e.rect.h,
        w: e.rect.w,
        h: e.rect.h,
        nearest_clue_area: e.nearestClueArea,
        t_since_section_start_ms: Math.round(e.msSinceSectionStart),
        is_tutorial: e.isTutorial,
      });
      this.renderer.markDirty();
    });

    this.game.on('rejectRecovered', (e) => {
      analytics.track('reject_recovered', {
        funnel_level: e.funnelLevel,
        t_ms: Math.round(e.ms),
        rejects_in_row: e.rejectsInRow,
      });
    });

    this.game.on('comboTier', (e) => {
      this.pendingTier = e;
      analytics.track('combo_tier_up', { tier: e.tier, combo: e.combo });
    });

    this.game.on('hintBlocked', (e) => {
      analytics.track('hint_blocked', {
        funnel_level: e.funnelLevel,
        remaining_ms: Math.round(e.remainingMs),
      });
    });

    this.game.on('sectionComplete', (e) => {
      this.sfx.sectionClear();
      vibrate(15);

      const centre = this.board.regionScreenCentre(this.camera, e.section.bounds);
      this.effects.sparkleBurst(centre.x, centre.y, centre.radius * 0.8, this.camera.cellPx, '#FFFFFF');
      if (!e.isTutorial) {
        const praise = SECTION_PRAISE[e.funnelLevel % SECTION_PRAISE.length];
        const px = Math.max(3, Math.min(6, this.renderer.vp.w / 70));
        // Sits above the progress readout, which flashes at this exact moment for the same
        // move - overlapping the two made both unreadable.
        this.effects.mascotEnter(
          this.renderer.vp.w / 2,
          this.renderer.vp.h - this.renderer.vp.insetBottom - 58,
          px,
          praise,
        );
      }

      if (e.isTutorial) {
        analytics.track('tutorial_step_complete', {
          step: this.queue[this.queueIndex]?.tutorialIndex + 1,
          attempts: e.stats.rejects + e.section.regions.length,
          duration_ms: Math.round(e.durationMs),
        });
      } else {
        analytics.track('section_complete', {
          funnel_level: e.funnelLevel,
          duration_ms: Math.round(e.durationMs),
          rejects: e.stats.rejects,
          hints: e.stats.hints,
          max_combo: e.stats.maxCombo,
        });
        this.scheduleSelfContinue('section', e.funnelLevel);
        // Queued, not shown now: the section-clear beat already puts the mascot, a line of
        // praise and the progress readout on screen. A fourth thing arriving on top of a
        // celebration is unreadable, and an intent probe nobody could read is worse than no
        // probe at all. It goes up when calm play resumes.
        if (e.funnelLevel === 3) this.installBannerPending = true;
      }

      const hold = e.isTutorial ? TUTORIAL_STEP_GAP_MS : SECTION_CLEAR_HOLD_MS;
      window.setTimeout(() => this.game.advanceSection(), hold);
    });

    this.game.on('pictureComplete', (e) => {
      if (e.isTutorial) {
        window.setTimeout(() => {
          if (this.queueIndex === this.queue.length - 1) this.endContent();
          else if (this.queue[this.queueIndex + 1]?.isTutorial === false) {
            analytics.track('tutorial_complete', {
              total_duration_ms: Math.round(performance.now()),
              total_attempts: this.game.rejectsTotal + this.game.regionsSolvedTotal,
            });
            this.startEntry(this.queueIndex + 1);
          } else {
            this.startEntry(this.queueIndex + 1);
          }
        }, TUTORIAL_STEP_GAP_MS);
        return;
      }

      analytics.track('picture_complete', {
        picture_id: e.picture.id,
        duration_ms: Math.round(e.durationMs),
        rejects_total: e.rejectsTotal,
        hints_total: e.hintsTotal,
      });

      this.hideInstallBanner();
      // Pull back so the finished picture is the whole screen, then celebrate over it.
      this.camera.frameTo(pictureBounds(e.picture), this.renderer.vp, { durationMs: 600 });
      this.hud.setControlsVisible(false);
      this.effects.confettiBurst(this.renderer.vp.w, this.renderer.vp.h);
      this.sfx.pictureClear();
      const px = Math.max(4, Math.min(9, this.renderer.vp.w / 44));
      this.effects.mascotEnter(this.renderer.vp.w / 2, this.renderer.vp.h * 0.52, px, '');
      this.hud.showWin('Brilliant!', "A true artist's touch.");
      this.trackInstallShown('picture_complete');
      this.scheduleSelfContinue('picture', this.game.sectionsCompleted);
      this.renderer.markDirty();
    });

    this.game.on('progress', () => this.renderer.markDirty());
  }

  private firstInputAt = 0;

  // ---------------------------------------------------------------- HUD

  private wireHud(): void {
    this.hud.overviewBtn.addEventListener('click', () => {
      this.sfx.unlock();
      this.sfx.click();
      const picture = this.game.picture;
      if (!picture) return;

      if (this.game.phase === 'overview') {
        this.exitOverview();
        return;
      }
      if (this.game.phase !== 'play') return;

      this.hideInstallBanner();
      this.game.enterOverview();
      this.overviewOpenedAt = performance.now();
      this.camera.frameTo(pictureBounds(picture), this.renderer.vp, { durationMs: OVERVIEW_TWEEN_MS });
      this.hud.pinProgress(progressPercent(picture), true);
      this.hud.showTapHint(true);
      this.board.hideSelection();
      analytics.track('overview_open', {
        funnel_level: this.game.funnelLevel,
        progress_pct: progressPercent(picture),
      });
      this.renderer.markDirty();
    });

    this.hud.hintBtn.addEventListener('click', () => {
      this.sfx.unlock();
      this.sfx.click();
      this.ghost.noteActivity();
      const result = this.game.useHint();
      if (result.ok) {
        analytics.track('hint_used', {
          funnel_level: this.game.funnelLevel,
          region_area: result.region.area,
          regions_left: this.game.section?.unsolvedCount ?? 0,
          t_since_section_start_ms: 0,
        });
      }
    });

    this.hud.muteBtn.addEventListener('click', () => {
      this.sfx.unlock();
      const muted = this.sfx.toggleMute();
      this.hud.setMuted(muted);
      if (!muted) this.sfx.click();
      analytics.track('mute_toggle', { muted });
    });

    this.hud.nextBtn.addEventListener('click', () => {
      this.sfx.unlock();
      this.sfx.click();
      this.hud.hideWin();
      if (this.queueIndex >= this.queue.length - 1) this.endContent();
      else this.startEntry(this.queueIndex + 1);
    });

    this.hud.installClose.addEventListener('click', () => this.hideInstallBanner());

    const trackInstallClick = (placement: string) => () => {
      analytics.track('fake_install_click', {
        placement,
        funnel_level: this.game.funnelLevel,
        t_shown_to_click_ms: Math.round(performance.now() - this.installShownAt),
      });
    };
    this.hud.installCta.addEventListener('click', trackInstallClick('after_section_3'));
    this.hud.winInstall.addEventListener('click', trackInstallClick('picture_complete'));
    this.hud.endInstall.addEventListener('click', trackInstallClick('content_end'));

    // Carry the session id across to the landing page so intent can be joined back to the
    // session that produced it. No personal data, just the random per-session id.
    for (const link of [this.hud.installCta, this.hud.winInstall, this.hud.endInstall]) {
      const url = new URL(link.getAttribute('href') ?? 'soon/', window.location.href);
      url.searchParams.set('sid', newSessionId());
      const idea = queryFlag('idea_id');
      if (idea) url.searchParams.set('idea_id', idea);
      link.setAttribute('href', url.pathname + url.search);
    }
  }

  private showInstallBanner(): void {
    if (this.installBannerUsed) return;
    this.installBannerUsed = true;
    this.hud.showInstallBanner(true);
    this.trackInstallShown('after_section_3');
    this.installBannerTimer = window.setTimeout(() => this.hideInstallBanner(), INSTALL_BANNER_MS);
  }

  /**
   * Two intent placements must never be on screen together. If the after-section-3 banner is
   * still up when the picture-complete card appears, both fire fake_install_shown for the same
   * moment and a click cannot be attributed to either - which breaks the one metric used to
   * rank ideas against each other.
   */
  private hideInstallBanner(): void {
    window.clearTimeout(this.installBannerTimer);
    this.hud.showInstallBanner(false);
  }

  /** Only counts as shown once it is actually on screen, not merely present in the DOM. */
  private trackInstallShown(placement: string): void {
    this.installShownAt = performance.now();
    analytics.track('fake_install_shown', { placement, funnel_level: this.game.funnelLevel });
  }

  private exitOverview(): void {
    const picture = this.game.picture;
    if (!picture || this.game.phase !== 'overview') return;
    this.game.exitOverview();
    this.hud.pinProgress(progressPercent(picture), false);
    this.hud.showTapHint(false);
    this.reframe(false);
    analytics.track('overview_close', {
      funnel_level: this.game.funnelLevel,
      progress_pct: progressPercent(picture),
      open_duration_ms: Math.round(performance.now() - this.overviewOpenedAt),
    });
    this.renderer.markDirty();
  }

  private scheduleSelfContinue(after: 'section' | 'picture', funnelLevel: number): void {
    window.setTimeout(() => {
      // Still here 10 seconds after the reward = chose to carry on. Shidaku has no lose
      // state, so this and reject_recovered are what stand in for "retried after a loss".
      if (document.visibilityState !== 'visible' || this.sessionEnded) return;
      this.game.noteSelfContinue();
      analytics.track('self_continue', { after, funnel_level: funnelLevel });
    }, SELF_CONTINUE_MS);
  }

  // ---------------------------------------------------------------- input

  private cellAt(sx: number, sy: number): CellPos {
    return {
      x: Math.floor(this.camera.screenToWorldX(sx) / CELL),
      y: Math.floor(this.camera.screenToWorldY(sy) / CELL),
    };
  }

  private wireInput(): void {
    attachPointer(this.renderer.canvas, {
      onDown: (x, y) => {
        // The very first gesture is the only moment an AudioContext may be created.
        this.sfx.unlock();
        this.noteFirstInput();
        this.ghost.noteActivity();

        if (this.game.phase === 'overview') return false;
        if (this.game.inputLocked || this.game.phase !== 'play') return false;

        const cell = this.cellAt(x, y);
        const section = this.game.sectionIndex;
        if (this.game.query.sectionAt(cell.x, cell.y) !== section) return false;
        if (this.game.query.isSolved(cell.x, cell.y)) return false;

        this.dragging = true;
        this.dragStart = cell;
        this.dragCurrent = cell;
        this.board.showSelection(makeRect(cell, cell), true);
        this.board.setHover([cell]);
        this.sfx.hover(performance.now());
        this.renderer.markDirty();
        return true;
      },

      onMove: (x, y) => {
        if (!this.dragging) return;
        const raw = this.cellAt(x, y);
        const clamped = clampDragTo(this.game.query, this.game.sectionIndex, this.dragStart, raw);
        if (clamped.x !== this.dragCurrent.x || clamped.y !== this.dragCurrent.y) {
          this.dragCurrent = clamped;
          this.sfx.hover(performance.now());
        }
        const rect = makeRect(this.dragStart, this.dragCurrent);
        this.board.showSelection(rect, false);
        this.board.setHover(rowMajorCells(rect));
        this.renderer.markDirty();
      },

      onUp: () => this.finishDrag(),
      onCancel: () => this.finishDrag(),

      onTap: () => {
        // Tap anywhere leaves Overview - the only way back, and the pacing beat the spec
        // asks for between stretches of play.
        if (this.game.phase === 'overview') this.exitOverview();
      },
    });
  }

  private finishDrag(): void {
    if (!this.dragging) return;
    this.dragging = false;
    this.board.hideSelection();
    const rect = makeRect(this.dragStart, this.dragCurrent);
    const result = this.game.attemptRect(rect);
    // On accept popRegion takes the cells over; on reject rejectRegion waves them back down.
    // If the attempt was ignored, neither runs and the risen grey blocks would stay risen.
    if (!result.ok && result.ignored) this.board.setHover([]);
    this.renderer.markDirty();
  }

  private noteFirstInput(): void {
    if (this.firstInputTracked) return;
    this.firstInputTracked = true;
    this.firstInputAt = performance.now();
    analytics.track('first_input', {
      // -1 means "no frame had been drawn yet", which only happens in a tab that started
      // backgrounded with rAF throttled. Reporting a sentinel is honest; reporting a
      // negative or page-load-relative number would quietly corrupt the technical gate.
      t_from_first_frame_ms: this.ttffMs > 0 ? Math.round(this.firstInputAt - this.ttffMs) : -1,
    });
  }

  // ---------------------------------------------------------------- session end

  private wireSessionEnd(): void {
    const end = (): void => {
      if (this.sessionEnded) return;
      this.sessionEnded = true;
      const hintHeavy =
        this.game.regionsSolvedTotal > 0 && this.game.hintsTotal / this.game.regionsSolvedTotal >= 0.5;
      analytics.track(
        'session_end',
        {
          duration_ms: Math.round(performance.now()),
          last_funnel_level: this.game.sectionsCompleted,
          regions_solved: this.game.regionsSolvedTotal,
          rejects_total: this.game.rejectsTotal,
          hints_total: this.game.hintsTotal,
          // Flagged, not filtered: a free hint solves a whole region, so a hint-heavy
          // session would otherwise inflate completion and hide "not worth continuing".
          // The funnel has to be reportable both with and without this cohort.
          hint_heavy: hintHeavy,
          max_combo: this.game.sessionMaxCombo,
          self_continue_count: this.game.selfContinueCount,
          overview_opens: this.game.overviewOpens,
        },
        { beacon: true },
      );
    };

    // Both, because neither alone is reliable across iOS and Android WebViews.
    document.addEventListener('visibilitychange', () => {
      if (document.visibilityState === 'hidden') end();
    });
    window.addEventListener('pagehide', end);
  }

  // ---------------------------------------------------------------- frame

  private reframe(instant: boolean): void {
    const section = this.game.section;
    const picture = this.game.picture;
    if (!picture) return;
    const bounds = this.game.phase === 'overview' || !section ? pictureBounds(picture) : section.bounds;
    this.camera.frameTo(bounds, this.renderer.vp, {
      instant,
      durationMs: this.game.phase === 'overview' ? OVERVIEW_TWEEN_MS : SECTION_TWEEN_MS,
    });
  }

  private frame(now: number, dt: number): boolean {
    if (this.ttffMs === 0) {
      this.ttffMs = now;
      analytics.track('first_frame', { ttff_ms: Math.round(performance.now()) });
    }

    let busy = false;
    if (this.camera.update(dt)) busy = true;
    if (this.board.update(now, dt)) busy = true;
    if (this.effects.update(dt)) busy = true;
    if (this.ghost.update(dt)) busy = true;

    this.hud.setHintCooldown(this.game.hintRemainingMs());
    if (this.game.hintRemainingMs() > 0) busy = true;

    return busy;
  }

  private draw(ctx: CanvasRenderingContext2D, vp: import('./render/camera').Viewport): void {
    this.board.draw(ctx, this.camera, vp);
    const picture = this.game.picture;
    if (picture && this.game.isTutorial) this.ghost.draw(ctx, this.camera, picture.h);
    this.effects.draw(ctx);
  }

  private superProps(): Record<string, string | number | boolean | null> {
    return {
      idea_id: queryFlag('idea_id') ?? 'shidaku',
      build_ver: __BUILD_VER__,
      content_set: __CONTENT_SET__,
      variant: queryFlag('v') ?? 'a',
      session_id: newSessionId(),
      device_tier: deviceTier(),
      conn: connectionType(),
      is_inapp_browser: isInAppBrowser(),
      dpr: this.renderer.dpr,
      vw: this.renderer.vp.w,
      vh: this.renderer.vp.h,
      utm_source: queryFlag('utm_source'),
      utm_campaign: queryFlag('utm_campaign'),
      utm_content: queryFlag('utm_content'),
    };
  }
}

/** Union of every section's bounds - the whole artwork, ignoring transparent margins. */
function pictureBounds(picture: Picture): Rect {
  let x0 = picture.w;
  let y0 = picture.h;
  let x1 = -1;
  let y1 = -1;
  for (const section of picture.sections) {
    const b = section.bounds;
    if (b.x < x0) x0 = b.x;
    if (b.y < y0) y0 = b.y;
    if (b.x + b.w - 1 > x1) x1 = b.x + b.w - 1;
    if (b.y + b.h - 1 > y1) y1 = b.y + b.h - 1;
  }
  if (x1 < x0) return { x: 0, y: 0, w: picture.w, h: picture.h };
  return { x: x0, y: y0, w: x1 - x0 + 1, h: y1 - y0 + 1 };
}
