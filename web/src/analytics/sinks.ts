/**
 * The three sinks, each loaded lazily and each individually optional.
 *
 * PostHog answers "how many" and is the tool the Go/Kill decision reads.
 * Clarity answers "why" - session recordings for diagnosis only, never for deciding.
 * Meta Pixel exists so the campaign can optimise toward people who actually play.
 * Firebase is deliberately absent: 24h-late reporting and GA4 sampling are useless for a
 * three-day test, and its 500-event-name cap cannot be undone.
 *
 * Keys come from env vars at build time. A missing key disables that sink silently - the
 * game must be playable and testable locally with no accounts configured at all.
 */

import type { EventParams } from './schema';

type Fbq = ((...args: unknown[]) => void) & { queue?: unknown[]; loaded?: boolean; version?: string };

interface PostHogLike {
  init: (key: string, config: Record<string, unknown>) => void;
  capture: (event: string, props?: Record<string, unknown>, options?: Record<string, unknown>) => void;
  register: (props: Record<string, unknown>) => void;
  __loaded?: boolean;
}

declare global {
  interface Window {
    posthog?: PostHogLike;
    clarity?: (...args: unknown[]) => void;
    fbq?: Fbq;
    _fbq?: Fbq;
  }
}

function loadScript(src: string, onError?: () => void): void {
  try {
    const el = document.createElement('script');
    el.async = true;
    el.src = src;
    el.onerror = () => onError?.();
    document.head.appendChild(el);
  } catch {
    onError?.();
  }
}

// ---------------------------------------------------------------- PostHog

let posthogReady = false;

export function initPostHog(key: string, host: string, superProps: EventParams): boolean {
  if (!key) return false;
  try {
    // Minimal stub of posthog-js's own snippet: queue calls until array.js swaps it out.
    const queue: Array<[string, unknown[]]> = [];
    const stub = {
      init: (...args: unknown[]) => queue.push(['init', args]),
      capture: (...args: unknown[]) => queue.push(['capture', args]),
      register: (...args: unknown[]) => queue.push(['register', args]),
      _queue: queue,
    } as unknown as PostHogLike;
    if (!window.posthog) window.posthog = stub;

    loadScript(`${host.replace(/\/$/, '')}/static/array.js`);

    const boot = (): void => {
      const ph = window.posthog;
      if (!ph || typeof ph.init !== 'function' || !ph.__loaded) {
        window.setTimeout(boot, 120);
        return;
      }
      ph.init(key, {
        api_host: host,
        // The prototype measures behaviour, not identity. No autocapture, no session
        // recording (Clarity does that), no cross-session people profiles.
        autocapture: false,
        capture_pageview: false,
        capture_pageleave: false,
        disable_session_recording: true,
        persistence: 'memory',
      });
      ph.register(superProps as Record<string, unknown>);
      posthogReady = true;
      for (const [method, args] of queue) {
        if (method === 'capture') (ph.capture as (...a: unknown[]) => void)(...args);
      }
      queue.length = 0;
    };
    boot();
    return true;
  } catch {
    return false;
  }
}

export function postHogCapture(event: string, props: EventParams, beacon = false): void {
  try {
    const ph = window.posthog;
    if (!ph) return;
    if (beacon && posthogReady) {
      // fetch() is cancelled on unload; sendBeacon is the only transport that survives it.
      ph.capture(event, props as Record<string, unknown>, { transport: 'sendBeacon' });
      return;
    }
    ph.capture(event, props as Record<string, unknown>);
  } catch {
    /* ignore */
  }
}

// ---------------------------------------------------------------- Clarity

export function initClarity(projectId: string): boolean {
  if (!projectId) return false;
  try {
    window.clarity =
      window.clarity ??
      ((...args: unknown[]) => {
        const c = window.clarity as unknown as { q?: unknown[] };
        (c.q ??= []).push(args);
      });
    loadScript(`https://www.clarity.ms/tag/${projectId}`);
    return true;
  } catch {
    return false;
  }
}

/** Clarity gets tags, not events - they are what make a recording findable later. */
export function claritySet(key: string, value: string): void {
  try {
    window.clarity?.('set', key, value);
  } catch {
    /* ignore */
  }
}

// ---------------------------------------------------------------- Meta Pixel

export function initPixel(pixelId: string): boolean {
  if (!pixelId) return false;
  try {
    if (!window.fbq) {
      const fbq: Fbq = ((...args: unknown[]) => {
        (fbq.queue ??= []).push(args);
      }) as Fbq;
      fbq.queue = [];
      fbq.version = '2.0';
      window.fbq = fbq;
      window._fbq = fbq;
      loadScript('https://connect.facebook.net/en_US/fbevents.js');
    }
    window.fbq?.('init', pixelId);
    return true;
  } catch {
    return false;
  }
}

export function pixelTrack(event: string, props?: EventParams): void {
  try {
    // trackCustom, not track: these are our own event names, not Meta's standard ones.
    window.fbq?.('trackCustom', event, props);
  } catch {
    /* ignore */
  }
}
