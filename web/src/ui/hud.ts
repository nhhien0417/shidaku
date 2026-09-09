/**
 * The DOM half of the interface: labels, buttons, overlays.
 *
 * DOM rather than canvas for these, because safe-area insets, real tap targets and
 * accessibility all come free here and all have to be reinvented on a canvas. The board is
 * the opposite case, which is why it is canvas-only.
 */

import { renderMiniMap } from '../render/board';
import type { Picture } from '../core/types';

function need<T extends HTMLElement>(id: string): T {
  const el = document.getElementById(id);
  if (!el) throw new Error(`missing element #${id}`);
  return el as T;
}

export class Hud {
  readonly levelLabel = need<HTMLDivElement>('level-label');
  readonly progress = need<HTMLDivElement>('progress');
  readonly tapHint = need<HTMLDivElement>('tap-hint');
  readonly overviewBtn = need<HTMLButtonElement>('overview-btn');
  readonly hintBtn = need<HTMLButtonElement>('hint-btn');
  readonly hintLabel = need<HTMLSpanElement>('hint-label');
  readonly muteBtn = need<HTMLButtonElement>('mute-btn');
  readonly minimap = need<HTMLCanvasElement>('minimap');

  readonly tutorialBanner = need<HTMLElement>('tutorial-banner');
  readonly tutorialText = need<HTMLParagraphElement>('tutorial-text');
  readonly tutorialNote = need<HTMLParagraphElement>('tutorial-note');
  readonly tutorialDots = Array.from(need<HTMLDivElement>('tutorial-dots').querySelectorAll('i'));

  readonly installBanner = need<HTMLElement>('install-banner');
  readonly installCta = need<HTMLAnchorElement>('install-banner-cta');
  readonly installClose = need<HTMLButtonElement>('install-banner-close');

  readonly winOverlay = need<HTMLDivElement>('win-overlay');
  readonly winTitle = need<HTMLHeadingElement>('win-title');
  readonly winSub = need<HTMLParagraphElement>('win-sub');
  readonly nextBtn = need<HTMLButtonElement>('next-btn');
  readonly winInstall = need<HTMLAnchorElement>('win-install');

  readonly endOverlay = need<HTMLDivElement>('end-overlay');
  readonly endInstall = need<HTMLAnchorElement>('end-install');
  readonly bootError = need<HTMLDivElement>('boot-error');

  private progressTimer = 0;
  private progressPinned = false;

  setLevelLabel(text: string): void {
    this.levelLabel.textContent = text;
  }

  /**
   * Pops the progress readout in, then fades it back out - unless Overview is open, which
   * pins it. Matches the demo's 0.25s in / 1.4s hold / 0.35s out.
   */
  flashProgress(percent: number): void {
    this.progress.textContent = `Progress: ${percent}%`;
    if (this.progressPinned) return;
    this.progress.classList.add('show');
    window.clearTimeout(this.progressTimer);
    this.progressTimer = window.setTimeout(() => {
      if (!this.progressPinned) this.progress.classList.remove('show');
    }, 1400);
  }

  pinProgress(percent: number, pinned: boolean): void {
    this.progressPinned = pinned;
    this.progress.textContent = `Progress: ${percent}%`;
    if (pinned) {
      window.clearTimeout(this.progressTimer);
      this.progress.classList.add('show');
    } else {
      this.progress.classList.remove('show');
    }
  }

  showTapHint(show: boolean): void {
    this.tapHint.classList.toggle('show', show);
  }

  /** Countdown on the hint button itself, so the wait is visible rather than mysterious. */
  setHintCooldown(remainingMs: number): void {
    if (remainingMs > 50) {
      this.hintLabel.textContent = `${Math.ceil(remainingMs / 1000)}s`;
      this.hintBtn.disabled = true;
    } else {
      this.hintLabel.textContent = 'Hint';
      this.hintBtn.disabled = false;
    }
  }

  setMuted(muted: boolean): void {
    const wave = document.getElementById('mute-wave');
    const cross = document.getElementById('mute-cross');
    if (wave) wave.toggleAttribute('hidden', muted);
    if (cross) cross.toggleAttribute('hidden', !muted);
    this.muteBtn.setAttribute('aria-pressed', String(muted));
  }

  updateMiniMap(picture: Picture): void {
    renderMiniMap(this.minimap, picture);
  }

  setTutorialStep(step: number, text: string, note: string): void {
    this.tutorialBanner.hidden = false;
    this.tutorialText.textContent = text;
    this.tutorialNote.textContent = note;
    this.tutorialDots.forEach((dot, i) => dot.classList.toggle('on', i <= step - 1));
  }

  hideTutorial(): void {
    this.tutorialBanner.hidden = true;
  }

  setControlsVisible(visible: boolean): void {
    this.overviewBtn.style.visibility = visible ? 'visible' : 'hidden';
    this.hintBtn.style.visibility = visible ? 'visible' : 'hidden';
  }

  showInstallBanner(show: boolean): void {
    if (show) {
      this.installBanner.hidden = false;
      // Two frames: the element has to be laid out before the transition can run.
      requestAnimationFrame(() => requestAnimationFrame(() => this.installBanner.classList.add('show')));
    } else {
      this.installBanner.classList.remove('show');
      window.setTimeout(() => {
        this.installBanner.hidden = true;
      }, 300);
    }
  }

  showWin(title: string, sub: string): void {
    this.winTitle.textContent = title;
    this.winSub.textContent = sub;
    this.winOverlay.hidden = false;
  }

  hideWin(): void {
    this.winOverlay.hidden = true;
  }

  showEnd(): void {
    this.endOverlay.hidden = false;
  }

  showBootError(message: string): void {
    const text = document.getElementById('boot-error-text');
    if (text) text.textContent = message;
    this.bootError.hidden = false;
  }
}
