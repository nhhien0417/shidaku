/**
 * All sound, synthesised at runtime. Zero bytes of audio asset.
 *
 * The spec calls sound the cheapest-to-add, highest-impact thing missing from the Unity demo:
 * "a small click as each block rises would double the satisfaction of the most important
 * moment in the game". But the load budget is 3 seconds on 4G, which rules out audio files.
 * WebAudio oscillators cost about 2KB of code and nothing to download.
 *
 * The one real idea in here: the pop pitch RISES with the cell's position in the wave, so a
 * six-cell region plays a little ascending arpeggio. That is what makes a big region feel
 * better than six small ones - the same reason the wave is staggered visually.
 */

import { safeGet, safeSet } from '../util/device';

type Ctor = typeof AudioContext;

const MUTE_KEY = 'shidaku_muted';
/** Major pentatonic, so any subset of the ladder sounds consonant in any order. */
const LADDER = [0, 2, 4, 7, 9];
const C5 = 523.25;

export class Sfx {
  private ctx: AudioContext | null = null;
  private master: GainNode | null = null;
  private unavailable = false;
  private lastHoverAt = 0;
  muted: boolean;

  constructor() {
    this.muted = safeGet(MUTE_KEY) === '1';
  }

  get available(): boolean {
    return !this.unavailable;
  }

  /**
   * MUST be called from inside a real user-gesture handler. iOS and Android both refuse to
   * start an AudioContext otherwise, and a refused context stays silent forever.
   */
  unlock(): void {
    if (this.ctx || this.unavailable) return;
    try {
      const Ctx: Ctor | undefined =
        window.AudioContext ?? (window as unknown as { webkitAudioContext?: Ctor }).webkitAudioContext;
      if (!Ctx) {
        this.unavailable = true;
        return;
      }
      this.ctx = new Ctx();
      this.master = this.ctx.createGain();
      this.master.gain.value = 0.85;
      this.master.connect(this.ctx.destination);
      void this.ctx.resume();
    } catch {
      this.unavailable = true;
    }
  }

  toggleMute(): boolean {
    this.muted = !this.muted;
    safeSet(MUTE_KEY, this.muted ? '1' : '0');
    return this.muted;
  }

  private tone(opts: {
    freq: number;
    type?: OscillatorType;
    durMs: number;
    gain?: number;
    delayMs?: number;
    glideTo?: number;
  }): void {
    if (this.muted || !this.ctx || !this.master) return;
    try {
      const ctx = this.ctx;
      const t0 = ctx.currentTime + (opts.delayMs ?? 0) / 1000;
      const dur = opts.durMs / 1000;
      const osc = ctx.createOscillator();
      const gain = ctx.createGain();
      osc.type = opts.type ?? 'sine';
      osc.frequency.setValueAtTime(opts.freq, t0);
      if (opts.glideTo) osc.frequency.exponentialRampToValueAtTime(Math.max(1, opts.glideTo), t0 + dur);

      const peak = opts.gain ?? 0.14;
      gain.gain.setValueAtTime(0.0001, t0);
      gain.gain.exponentialRampToValueAtTime(peak, t0 + Math.min(0.012, dur * 0.3));
      gain.gain.exponentialRampToValueAtTime(0.0001, t0 + dur);

      osc.connect(gain);
      gain.connect(this.master);
      osc.start(t0);
      osc.stop(t0 + dur + 0.02);
    } catch {
      /* a dropped sound is never worth an exception */
    }
  }

  private noise(opts: { durMs: number; gain?: number; freq?: number }): void {
    if (this.muted || !this.ctx || !this.master) return;
    try {
      const ctx = this.ctx;
      const frames = Math.max(1, Math.floor((ctx.sampleRate * opts.durMs) / 1000));
      const buffer = ctx.createBuffer(1, frames, ctx.sampleRate);
      const data = buffer.getChannelData(0);
      for (let i = 0; i < frames; i++) {
        data[i] = (Math.random() * 2 - 1) * (1 - i / frames);
      }
      const src = ctx.createBufferSource();
      src.buffer = buffer;
      const filter = ctx.createBiquadFilter();
      filter.type = 'bandpass';
      filter.frequency.value = opts.freq ?? 1400;
      const gain = ctx.createGain();
      gain.gain.value = opts.gain ?? 0.08;
      src.connect(filter);
      filter.connect(gain);
      gain.connect(this.master);
      src.start();
    } catch {
      /* ignore */
    }
  }

  private ladderFreq(index: number): number {
    const step = LADDER[index % LADDER.length];
    const octave = Math.min(2, Math.floor(index / LADDER.length));
    return C5 * Math.pow(2, (step + octave * 12) / 12);
  }

  /** One block landing. `index` is its position in the wave, `delayMs` matches the visual. */
  pop(index: number, delayMs: number): void {
    this.tone({ freq: this.ladderFreq(index), type: 'sine', durMs: 90, gain: 0.13, delayMs });
    this.tone({
      freq: this.ladderFreq(index) * 2,
      type: 'triangle',
      durMs: 55,
      gain: 0.04,
      delayMs,
    });
  }

  /** A cell lighting up under the finger. Throttled: a fast drag would machine-gun it. */
  hover(nowMs: number): void {
    if (nowMs - this.lastHoverAt < 32) return;
    this.lastHoverAt = nowMs;
    this.tone({ freq: 880, type: 'sine', durMs: 22, gain: 0.03 });
  }

  /** Two notes falling: clearly "not that", and over in a tenth of a second. */
  reject(): void {
    this.tone({ freq: 210, type: 'triangle', durMs: 70, gain: 0.1 });
    this.tone({ freq: 150, type: 'triangle', durMs: 110, gain: 0.09, delayMs: 70 });
  }

  comboTier(tier: number): void {
    const root = C5 * Math.pow(2, (tier * 2) / 12);
    [0, 4, 7].forEach((interval, i) => {
      this.tone({
        freq: root * Math.pow(2, interval / 12),
        type: 'sine',
        durMs: 220,
        gain: 0.07,
        delayMs: i * 45,
      });
    });
  }

  sectionClear(): void {
    [0, 4, 7, 12].forEach((interval, i) => {
      this.tone({
        freq: C5 * Math.pow(2, interval / 12),
        type: 'sine',
        durMs: 200,
        gain: 0.1,
        delayMs: i * 80,
      });
    });
  }

  pictureClear(): void {
    [0, 4, 7, 12, 16, 19].forEach((interval, i) => {
      this.tone({
        freq: C5 * Math.pow(2, interval / 12),
        type: 'sine',
        durMs: 320,
        gain: 0.1,
        delayMs: i * 95,
      });
      this.tone({
        freq: C5 * Math.pow(2, interval / 12) * 1.005,
        type: 'triangle',
        durMs: 320,
        gain: 0.035,
        delayMs: i * 95,
      });
    });
  }

  click(): void {
    this.noise({ durMs: 30, gain: 0.07, freq: 1100 });
  }
}
