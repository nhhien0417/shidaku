/**
 * Device facts, and every browser API that might not be there.
 *
 * The primary runtime is Facebook's in-app WebView on both Android and iOS - not Chrome, not
 * Safari. Anything optional is wrapped so a missing or blocked API degrades instead of
 * throwing: a thrown exception during boot would cost the whole session, and the session is
 * the unit of measurement.
 */

export interface SafeArea {
  top: number;
  bottom: number;
}

/** localStorage throws outright in some privacy configurations, not just returns null. */
export function safeGet(key: string): string | null {
  try {
    return window.localStorage.getItem(key);
  } catch {
    return null;
  }
}

export function safeSet(key: string, value: string): void {
  try {
    window.localStorage.setItem(key, value);
  } catch {
    /* nothing we can do, and nothing that matters */
  }
}

export function sessionGet(key: string): string | null {
  try {
    return window.sessionStorage.getItem(key);
  } catch {
    return null;
  }
}

export function sessionSet(key: string, value: string): void {
  try {
    window.sessionStorage.setItem(key, value);
  } catch {
    /* ignore */
  }
}

export function readSafeArea(): SafeArea {
  try {
    const probe = document.createElement('div');
    probe.style.cssText =
      'position:fixed;left:0;top:0;visibility:hidden;pointer-events:none;' +
      'padding-top:env(safe-area-inset-top);padding-bottom:env(safe-area-inset-bottom);';
    document.body.appendChild(probe);
    const styles = getComputedStyle(probe);
    const top = parseFloat(styles.paddingTop) || 0;
    const bottom = parseFloat(styles.paddingBottom) || 0;
    probe.remove();
    return { top, bottom };
  } catch {
    return { top: 0, bottom: 0 };
  }
}

/** Rough three-way split, only ever used to segment the funnel by hardware. */
export function deviceTier(): 'low' | 'mid' | 'high' {
  const nav = navigator as Navigator & { deviceMemory?: number };
  const cores = nav.hardwareConcurrency ?? 2;
  const memory = nav.deviceMemory ?? 0;
  if (cores <= 4 || (memory > 0 && memory <= 2)) return 'low';
  if (cores <= 6 || (memory > 0 && memory <= 4)) return 'mid';
  return 'high';
}

export function connectionType(): string {
  const conn = (navigator as Navigator & { connection?: { effectiveType?: string } }).connection;
  return conn?.effectiveType ?? 'unknown';
}

/**
 * Facebook and Instagram in-app browsers, by user agent. Used only to segment the data -
 * never to change behaviour, because a behaviour split between environments would make the
 * funnel incomparable.
 */
export function isInAppBrowser(): boolean {
  const ua = navigator.userAgent || '';
  return /FBAN|FBAV|FB_IAB|Instagram|Line\/|MicroMessenger/i.test(ua);
}

export function vibrate(pattern: number | number[]): void {
  try {
    const nav = navigator as Navigator & { vibrate?: (p: number | number[]) => boolean };
    nav.vibrate?.(pattern);
  } catch {
    /* iOS Safari has no vibrate; that is fine */
  }
}

export function queryFlag(name: string): string | null {
  try {
    return new URLSearchParams(window.location.search).get(name);
  } catch {
    return null;
  }
}

export function newSessionId(): string {
  const existing = sessionGet('shidaku_sid');
  if (existing) return existing;
  let id: string;
  try {
    id = crypto.randomUUID();
  } catch {
    id = `s${Date.now().toString(36)}${Math.random().toString(36).slice(2, 10)}`;
  }
  sessionSet('shidaku_sid', id);
  return id;
}
