/**
 * The ?debug=1 overlay. This is the primary tool for verifying the event stream by hand
 * before any money is spent on traffic - if an event is missing or doubled, it shows up here
 * in seconds instead of in a corrupted funnel three days later.
 *
 * Never enabled without the query flag, and the flag does nothing but display.
 */

import { analytics, type RecordedEvent } from './index';

const MAX_ROWS = 26;

export function mountDebugOverlay(getStatus: () => Record<string, string | number | boolean>): void {
  const el = document.getElementById('debug');
  if (!el) return;
  el.hidden = false;

  const rows: string[] = [];
  const push = (e: RecordedEvent): void => {
    const params = Object.entries(e.params)
      .map(([k, v]) => `${k}=${v}`)
      .join(' ');
    rows.push(`${(e.at / 1000).toFixed(2)}s ${e.name}${params ? ' | ' + params : ''}`);
    if (rows.length > MAX_ROWS) rows.shift();
  };

  for (const e of analytics.log) push(e);
  analytics.onEvent((e) => push(e));

  const render = (): void => {
    const status = getStatus();
    const head = Object.entries(status)
      .map(([k, v]) => `${k}:${v}`)
      .join('  ');
    const sinks = analytics.sinkStatus;
    el.textContent =
      `${head}\nsinks posthog:${sinks.posthog} clarity:${sinks.clarity} pixel:${sinks.pixel}\n` +
      `events ${analytics.log.length}\n` +
      rows.join('\n');
    window.setTimeout(render, 250);
  };
  render();
}
