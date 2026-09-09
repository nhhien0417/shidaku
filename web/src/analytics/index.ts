/**
 * The one function the game calls: track(name, params).
 *
 * Responsibilities, in order of importance:
 *  1. NEVER LOSE page_open. It is the denominator of the technical gate, it fires on the
 *     first line of main.ts, and at that moment no SDK has loaded. So everything queues.
 *  2. Never let analytics break the game. Every call is wrapped; a dead sink is invisible to
 *     the player.
 *  3. Never fire a once-only event twice. A duplicated first_input silently corrupts the
 *     gate that decides whether the idea is even allowed to be judged.
 *  4. Load the SDKs AFTER the first frame, so measurement never costs time-to-interactive.
 */

import { EVENT_SPEC, PIXEL_MAP, PIXEL_SECTION_LEVELS, type EventName, type EventParams } from './schema';
import { claritySet, initClarity, initPixel, initPostHog, pixelTrack, postHogCapture } from './sinks';

export interface AnalyticsConfig {
  posthogKey: string;
  posthogHost: string;
  clarityId: string;
  pixelId: string;
  superProps: EventParams;
  debug: boolean;
}

export interface RecordedEvent {
  name: EventName;
  params: EventParams;
  at: number;
}

class Analytics {
  /** Everything fired this session, in order. The ?debug=1 overlay reads this. */
  readonly log: RecordedEvent[] = [];

  private queue: Array<{ name: EventName; params: EventParams; beacon: boolean }> = [];
  private fired = new Set<EventName>();
  private started = false;
  private config: AnalyticsConfig | null = null;
  private sinks = { posthog: false, clarity: false, pixel: false };
  private listeners: Array<(e: RecordedEvent) => void> = [];

  onEvent(listener: (e: RecordedEvent) => void): void {
    this.listeners.push(listener);
  }

  /** Called after first_frame. Until then, track() only queues and logs. */
  start(config: AnalyticsConfig): void {
    if (this.started) return;
    this.started = true;
    this.config = config;

    this.sinks.posthog = initPostHog(config.posthogKey, config.posthogHost, config.superProps);
    this.sinks.clarity = initClarity(config.clarityId);
    this.sinks.pixel = initPixel(config.pixelId);

    if (this.sinks.clarity) {
      // Tags are how you find the recording that matches a number in PostHog later.
      claritySet('idea_id', String(config.superProps.idea_id ?? ''));
      claritySet('build_ver', String(config.superProps.build_ver ?? ''));
      claritySet('session_id', String(config.superProps.session_id ?? ''));
    }

    const pending = this.queue;
    this.queue = [];
    for (const item of pending) this.dispatch(item.name, item.params, item.beacon);

    if (config.debug) {
      console.info('[analytics] sinks', this.sinks, 'super', config.superProps);
    }
  }

  get sinkStatus(): { posthog: boolean; clarity: boolean; pixel: boolean } {
    return { ...this.sinks };
  }

  track(name: EventName, params: EventParams = {}, opts: { beacon?: boolean } = {}): void {
    try {
      const spec = EVENT_SPEC[name];
      if (!spec) {
        console.warn(`[analytics] unknown event "${name}"`);
        return;
      }
      if (spec.once) {
        if (this.fired.has(name)) return;
        this.fired.add(name);
      }

      const recorded: RecordedEvent = { name, params, at: performance.now() };
      this.log.push(recorded);
      for (const listener of this.listeners) {
        try {
          listener(recorded);
        } catch {
          /* ignore */
        }
      }

      if (this.config?.debug) {
        const missing = spec.required.filter((key) => !(key in params));
        if (missing.length > 0) console.warn(`[analytics] ${name} missing params: ${missing.join(', ')}`);
        console.debug(`[analytics] ${name}`, params);
      }

      if (!this.started) {
        this.queue.push({ name, params, beacon: opts.beacon ?? false });
        return;
      }
      this.dispatch(name, params, opts.beacon ?? false);
    } catch (err) {
      console.error('[analytics] track failed', err);
    }
  }

  private dispatch(name: EventName, params: EventParams, beacon: boolean): void {
    postHogCapture(name, params, beacon);

    const mapped = PIXEL_MAP[name];
    if (mapped) pixelTrack(mapped);

    // The two funnel depths the campaign optimises toward.
    if (name === 'section_complete') {
      const level = Number(params.funnel_level);
      const pixelEvent = PIXEL_SECTION_LEVELS[level];
      if (pixelEvent) pixelTrack(pixelEvent);
    }
  }
}

export const analytics = new Analytics();

/**
 * Reads the build-time env. Absent keys are normal in dev: the sink turns itself off and the
 * ?debug=1 overlay becomes the way to verify events.
 */
export function analyticsConfigFromEnv(superProps: EventParams, debug: boolean): AnalyticsConfig {
  const env = import.meta.env as Record<string, string | undefined>;
  return {
    posthogKey: env.VITE_POSTHOG_KEY ?? '',
    posthogHost: env.VITE_POSTHOG_HOST ?? 'https://us.i.posthog.com',
    clarityId: env.VITE_CLARITY_ID ?? '',
    pixelId: env.VITE_META_PIXEL_ID ?? '',
    superProps,
    debug,
  };
}
