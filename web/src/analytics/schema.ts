/**
 * THE EVENT CONTRACT. One source of truth, enforced by scripts/check-events.mjs in CI.
 *
 * Two rules that matter more than anything else in this file:
 *
 * 1. NAMES AND PARAMS ARE FROZEN once a test has run against them. The whole method depends
 *    on comparing a new idea against the history of previous tests; renaming an event throws
 *    that history away. Add new events, never repurpose old ones.
 *
 * 2. "LEVEL" IN THE FUNNEL MEANS SECTION, NOT PICTURE. funnel_level counts sections completed
 *    cumulatively across every picture. A picture takes 10-30 minutes, so a picture-based
 *    funnel would read 100 / 4 / 1 / 0 / 0 on day-0 web traffic and diagnose nothing. A
 *    section is 1-3 minutes - the length of a "level" in any casual game - and it has its own
 *    completion gate and its own reward, which is what a funnel step needs to be.
 *
 * The denominator for every in-game rate is sessions that reached first_input. Never
 * page_open (that includes people who never saw a frame), and never paid clicks (30-50% of
 * those never become a session at all).
 */

export const EVENT_SPEC = {
  // ---- pre-game funnel. Without these four, a loading failure reads as a bad idea and a
  // perfectly good mechanic gets killed.
  page_open: { once: true, required: [] },
  assets_loaded: { once: true, required: ['load_ms'] },
  first_frame: { once: true, required: ['ttff_ms'] },
  first_input: { once: true, required: ['t_from_first_frame_ms'] },

  // ---- tutorial. Deliberately NOT part of funnel_level: almost everyone clears it, so
  // folding it in would inflate the legibility gate's 70% threshold into meaninglessness.
  tutorial_step_complete: { once: false, required: ['step', 'attempts', 'duration_ms'] },
  tutorial_complete: { once: true, required: ['total_duration_ms', 'total_attempts'] },

  /**
   * The first correct rectangle the player placed THEMSELVES. Separates someone playing from
   * someone poking at the screen to work out what this thing is - two behaviours that look
   * identical in a completion funnel.
   */
  first_meaningful_action: { once: true, required: ['t_from_first_input_ms'] },

  picture_start: { once: false, required: ['picture_id', 'total_sections', 'total_regions'] },
  picture_complete: { once: false, required: ['picture_id', 'duration_ms', 'rejects_total', 'hints_total'] },
  section_start: { once: false, required: ['funnel_level', 'picture_id', 'section_index', 'w', 'h', 'region_count'] },
  section_complete: { once: false, required: ['funnel_level', 'duration_ms', 'rejects', 'hints', 'max_combo'] },

  region_solved: {
    once: false,
    required: ['funnel_level', 'source', 'area', 'w', 'h', 't_since_section_start_ms', 'combo_after'],
  },
  region_rejected: {
    once: false,
    required: ['funnel_level', 'area_drawn', 'w', 'h', 'nearest_clue_area', 't_since_section_start_ms'],
  },

  /**
   * Shidaku has no lose state, so the method's strongest day-0 signal - "retried after a
   * loss" - has no direct equivalent. These two stand in for it: recovering from a refusal,
   * and choosing to carry on after a reward.
   */
  reject_recovered: { once: false, required: ['funnel_level', 't_ms', 'rejects_in_row'] },
  self_continue: { once: false, required: ['after', 'funnel_level'] },

  combo_tier_up: { once: false, required: ['tier', 'combo'] },
  hint_used: { once: false, required: ['funnel_level', 'region_area', 'regions_left', 't_since_section_start_ms'] },
  hint_blocked: { once: false, required: ['funnel_level', 'remaining_ms'] },
  overview_open: { once: false, required: ['funnel_level', 'progress_pct'] },
  overview_close: { once: false, required: ['funnel_level', 'progress_pct', 'open_duration_ms'] },
  mute_toggle: { once: false, required: ['muted'] },

  // ---- intent. The primary cross-idea ranking metric: the player has actually played the
  // mechanic before deciding, which is nothing like tapping an ad they have never tried.
  fake_install_shown: { once: false, required: ['placement', 'funnel_level'] },
  fake_install_click: { once: false, required: ['placement', 'funnel_level', 't_shown_to_click_ms'] },

  content_exhausted: { once: true, required: ['total_duration_ms', 'sections_solved'] },
  audio_unavailable: { once: true, required: [] },
  error: { once: false, required: ['message', 'where'] },

  session_end: {
    once: true,
    required: [
      'duration_ms',
      'last_funnel_level',
      'regions_solved',
      'rejects_total',
      'hints_total',
      'hint_heavy',
      'max_combo',
      'self_continue_count',
      'overview_opens',
    ],
  },
} as const;

export type EventName = keyof typeof EVENT_SPEC;

export type ParamValue = string | number | boolean | null;
export type EventParams = Record<string, ParamValue>;

/**
 * The only four events Facebook is told about.
 *
 * game_loaded and first_input measure traffic quality; level_1_complete lets the campaign
 * move off Landing Page Views onto a shallow conversion; level_3_complete is the real target
 * - optimising for it makes Facebook go and find people who actually play, which is the point
 * at which the sample stops being noise.
 */
export const PIXEL_MAP: Partial<Record<EventName, string>> = {
  assets_loaded: 'game_loaded',
  first_input: 'first_input',
};

/** section_complete maps to a Pixel event only at these funnel levels. */
export const PIXEL_SECTION_LEVELS: Record<number, string> = {
  1: 'level_1_complete',
  3: 'level_3_complete',
};
