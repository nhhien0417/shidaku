/**
 * Entry point. The order of the first few lines is not stylistic - it is the measurement.
 *
 * page_open is the denominator of the technical gate (first_input / page_open >= 60%), and
 * that gate is what separates "the idea is bad" from "the page did not load". So it fires
 * before any await, before any import that could fail, before the canvas exists. If it were
 * fired later, every crash and every impatient exit would silently vanish from the funnel and
 * the loading failure would be misread as a rejected mechanic.
 */

import './font.css';
import './style.css';
import { analytics } from './analytics';

analytics.track('page_open');

import { App } from './app';

// Nothing user-facing depends on this handler, but a silent exception in an in-app WebView is
// how you lose a whole test to "the numbers looked bad".
window.addEventListener('error', (e) => {
  analytics.track('error', {
    message: String(e.message).slice(0, 200),
    where: `${e.filename ?? '?'}:${e.lineno ?? 0}`,
  });
});
window.addEventListener('unhandledrejection', (e) => {
  analytics.track('error', {
    message: String((e as PromiseRejectionEvent).reason).slice(0, 200),
    where: 'unhandledrejection',
  });
});

const app = new App();
void app.boot();

/**
 * QA handle, only when ?debug=1. Deliberately available in the production build too: the
 * definition of done requires verifying the REAL build inside the Facebook in-app browser,
 * and there is no other way to inspect state there. Read-only in practice, and gated behind
 * a flag no player will type. (?level=N, which could actually skew the funnel, is stripped
 * from production builds instead - see app.ts.)
 */
if (new URLSearchParams(window.location.search).get('debug') === '1') {
  const w = window as unknown as { __shidaku?: App; __events?: unknown };
  w.__shidaku = app;
  // The full ordered event log. The overlay only shows the tail; verifying "no duplicates,
  // nothing missing, correct order" on a real handset needs all of it.
  w.__events = analytics.log;
}
