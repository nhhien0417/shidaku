/// <reference types="vite/client" />

/** Short git sha, injected by vite.config.ts. Attached to every analytics event. */
declare const __BUILD_VER__: string;

/**
 * "safe" = clean-room artwork, publishable.
 * "internal" = the four Unity demo templates, which contain third-party copyrighted
 * characters. Dev/QA only; check-budget.mjs fails a production build that declares it.
 */
declare const __CONTENT_SET__: 'safe' | 'internal';

interface ImportMetaEnv {
  readonly VITE_POSTHOG_KEY?: string;
  readonly VITE_POSTHOG_HOST?: string;
  readonly VITE_CLARITY_ID?: string;
  readonly VITE_META_PIXEL_ID?: string;
}

interface ImportMeta {
  readonly env: ImportMetaEnv;
}
