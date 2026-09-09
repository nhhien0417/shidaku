import { COMBO_TIERS } from '../render/palette';
import type { Picture } from './types';

/**
 * Progress is counted in REGIONS solved across the whole picture, not sections completed.
 *
 * This is a small difference with a large consequence for how the game feels: the number
 * ticks up after EVERY move instead of standing still and then jumping each time a section
 * finishes. Every bit of effort is acknowledged immediately.
 */
export function progressFraction(picture: Picture): number {
  return picture.totalRegions > 0 ? picture.solvedRegions / picture.totalRegions : 0;
}

export function progressPercent(picture: Picture): number {
  return Math.round(progressFraction(picture) * 100);
}

/** Combo tier index: 0 = below the first threshold, 1..5 = Nice!..Masterful! */
export function comboTierFor(combo: number): number {
  let tier = 0;
  for (let i = 0; i < COMBO_TIERS.length; i++) {
    if (combo >= COMBO_TIERS[i].at) tier = i + 1;
  }
  return tier;
}

export function comboTierInfo(tier: number): { label: string; color: string } | null {
  if (tier <= 0 || tier > COMBO_TIERS.length) return null;
  const t = COMBO_TIERS[tier - 1];
  return { label: t.label, color: t.color };
}
