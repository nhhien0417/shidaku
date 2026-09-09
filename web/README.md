# Shidaku — web prototype

A playable web build of Shidaku (Pixel Fill Shikaku), built to be run through
[the idea-validation process](../docs/Quy-Trinh-Kiem-Chung-Y-Tuong-Game.md).

**This is a measuring instrument, not a port of the Unity demo.** It exists to answer one
question — *is this mechanic actually interesting to a stranger on day 0* — and, if not, to say
**which way it failed**: people did not understand it, or they understood it and did not think
it was worth continuing. Every decision in here is downstream of that. Anything that muddies
the measurement is out, even when it would make a nicer game.

Full plan and rationale: [`docs/Plan-Web-Prototype-Shidaku.md`](../docs/Plan-Web-Prototype-Shidaku.md).

---

## Quick start

```bash
npm install
npm run content:safe   # generate the publishable puzzles
npm run font:subset    # optional; src/font.css is committed
npm run dev            # http://localhost:5173
```

```bash
npm run build          # typecheck + event contract + vite build + budget gate
npm test               # 49 unit tests
npm run preview        # serve dist/ at :4173
```

### Useful URLs

| URL | What it does |
|---|---|
| `/?debug=1` | Event log overlay, fps, funnel level. Also exposes `window.__shidaku` and `window.__events` for QA on a real handset. |
| `/?nofx=1` | Reserved for disabling animation during logic QA. |
| `/?level=N` | Skips the tutorial. **Dev builds only** — stripped from production so a player cannot poison the funnel with it. |
| `/soon/` | The fake-install landing page. |
| `/privacy/` | Privacy notice. |
| `/?idea_id=shidaku&v=a&utm_source=fb&...` | The shape of the real ad link. `idea_id` is the key that joins a test to its row in the test-history sheet. |

---

## The one decision worth knowing before reading the code

**"Level" in the funnel means SECTION, not picture.**

A picture takes 10–30 minutes, so a picture-based funnel on day-0 web traffic reads
`100 / 4 / 1 / 0 / 0` and diagnoses nothing. A section is 1–3 minutes — the length of a level
in any casual game — and it has its own completion gate and its own reward, which is what a
funnel step has to be.

So `funnel_level` counts **sections completed, cumulatively across every picture**. The tutorial
is excluded (almost everyone clears it, and folding it in would inflate the 70% legibility
threshold into meaninglessness).

Second thing: Shidaku has **no lose state**, so the process's strongest day-0 signal — "retried
after a loss" — has no direct equivalent. Two stand-ins are measured instead:
`reject_recovered` (placed a correct rectangle within 15s of being refused) and `self_continue`
(still playing 10s after a reward).

---

## Architecture

```
src/
  core/       the game, headless. rules.ts is pure and fully tested.
  render/     one canvas. No DOM per cell.
  input/      PointerEvent -> cell, and the mobile-browser defaults that would break a drag.
  audio/      WebAudio synthesis. Zero bytes of audio asset.
  ui/         the DOM half: labels, buttons, overlays, tutorial.
  analytics/  schema.ts is the contract; index.ts is the only way to fire an event.
scripts/      content pipeline, font subsetting, and the two CI gates.
```

- **`core/rules.ts`** is the only part that is not allowed to be wrong. Pure functions, no
  imports beyond types, 24 unit tests. Everything else can misbehave and the measurement still
  works; if this is wrong, every number is meaningless.
- **`core/state.ts`** runs headless — a whole picture is played to 100% in a unit test with no
  canvas, no DOM and no timers. It owns no animation timing; `app.ts` does.
- **Tutorial and real play share one code path.** A tutorial board is an ordinary `Picture`
  with a single region carrying `isTutorial`. Same drag, same match, same wave, same sound —
  the only honest way for a tutorial to teach a mechanic.

### Why canvas and no dependencies

The runtime that matters is **Facebook's in-app WebView**, not Chrome. Unity WebGL is ruled out
outright (the process document is explicit: it swings between a 15-second load and a hard crash
there). Phaser would spend two thirds of the load budget on an engine this game does not use.
300–700 animating DOM nodes is how you end up measuring the renderer instead of the mechanic.

Result: **37 KB gzipped, 4 requests.**

| | Actual | Budget |
|---|---|---|
| `index.html` (inlined CSS + base64 subset font) | 17.4 KB | 26 KB |
| JS | 19.2 KB | 60 KB |
| First content (catalog + picture 1) | 0.6 KB | 10 KB |
| **Total first load** | **37.2 KB** | 110 KB |
| First-load requests | 4 | 4 |

`npm run check:budget` fails the build if that creeps. Analytics SDKs are excluded on purpose:
they load after boot, which is why `page_open` is queued rather than sent immediately.

---

## Content

Two sets, one catalog switch.

```bash
npm run content:safe      # clean-room artwork -> content_set "safe"   (default, publishable)
npm run content:internal   # the four Unity demo puzzles -> "internal" (DEV/QA ONLY)
```

⚠️ **The four Unity demo templates are cross-stitch charts from outside sources and contain
third-party copyrighted characters.** They are useful for checking this pipeline against real
Unity-authored puzzles, and they must never be what a paid ad points at.
`scripts/check-budget.mjs` fails any production build that declares `content_set: "internal"`
unless `ALLOW_INTERNAL_CONTENT=1` is set explicitly.

The publishable set is three original pictures hand-authored in `scripts/art-safe.mjs`
(24 sections, ~144 regions, roughly 20–30 minutes of play). Content rules, all enforced by the
generator and by `loader.ts`:

- a region is **never 1 cell** (a move that costs a tap and teaches nothing) and never over 15;
- every section has **at least 3 regions** — a section is a funnel step, and a level solvable in
  two drags measures nothing;
- a region **never spans two colours**, which is why the picture's shape decides the puzzle's
  shape and not the other way round;
- sections are capped at **8 cells wide** so a cell still renders ≥ 40 CSS px on a 360px phone;
- no artwork colour sits within 0.1 luminance of the paper background, or a solved region reads
  as unsolved.

### Authoring in Unity (the primary path)

`Tools → Shidaku → Export Web JSON` (`Assets/_Game/Demo/Scripts/Editor/ShidakuWebExporter.cs`)
writes `web/public/puzzles/` straight from `PuzzleCatalog`. It re-runs Verify's three checks
(monochrome, coverage, exclusive ownership) and writes nothing if any fails.
`npm run content:internal` is a Node fallback that reads the `.asset` YAML directly, for working
on the web build without opening the editor.

---

## Analytics

Three sinks, keys from env vars (`VITE_POSTHOG_KEY`, `VITE_CLARITY_ID`, `VITE_META_PIXEL_ID`).
A missing key disables that sink silently, so the game is fully playable and testable with no
accounts configured.

| Sink | Role |
|---|---|
| **PostHog** | Quantitative. This is the tool the Go/Kill decision reads. |
| **Microsoft Clarity** | Qualitative — session recordings, for diagnosis only, never for deciding. |
| **Meta Pixel** | Exactly 4 events: `game_loaded`, `first_input`, `level_1_complete`, `level_3_complete`. |
| ~~Firebase~~ | Deliberately absent: 24h-late reporting and GA4 sampling are useless for a 3-day test. |

`src/analytics/schema.ts` is the single source of truth for 27 events, and
`npm run check:events` fails CI on an unknown event name, a missing required parameter, or a
declared event that nothing fires.

**Names and parameters are frozen once a test has run against them.** The whole method depends
on comparing a new idea to the history of previous tests; renaming an event throws that history
away. Add new events; never repurpose old ones.

The denominator for every in-game rate is **sessions that reached `first_input`** — never
`page_open` (that includes people who never saw a frame), never paid clicks (30–50% of those
never become a session at all).

---

## Deliberate deviations from the demo and the spec

Each of these was a decision, not an oversight.

| Change | Why |
|---|---|
| Accept-wave stagger compressed so a wave never exceeds ~1.1s | The demo's flat 175ms/cell makes a 15-cell region take ~3s with input locked. On a phone that reads as lag, not reward. |
| `mediumTint` moves away from the block in whichever direction has room | The demo's formula only lightens and was fitted to one saturated magenta. On a pale colour it returns the same colour, so the block rose invisibly — found by looking at the running game, not by theory. |
| Sections capped at 8 wide, not 9 | 360px / 1.14 / 9 = 35px per cell. The binding constraint is the thumb, not the grid. |
| Combo popups shown (demo counts them silently) | The micro-reward is the thing being measured; hiding it discards the signal. |
| Sound added, synthesised | The spec calls it the cheapest, highest-impact gap in the demo. The load budget rules out audio files. |
| Mascot + praise + sparkle on section clear | "The camera just slides" is far too quiet for the work the player just did. |
| Hint has a 5s cooldown | Matches the reference game and stops hint-spam from flattening the difficulty curve. |
| Install banner deferred to the next section start | Landing it on top of the section-clear celebration made the intent probe unreadable. |
| No progress saving | A returning player would be counted as a new one already at level 5, which corrupts the funnel. |

---

## Deploy

Static output; Cloudflare Pages. `public/_headers` sets the cache policy — hashed assets
immutable for a year, puzzles for an hour, `index.html` `no-cache` (a cached entry page would
pin returning visitors to an old build and mix two `build_ver` values into one funnel).

`build_ver` is the short git sha, injected at compile time and attached to every event.

**Do not deploy while a test is running.** If you must, bump `build_ver` and split the data by
it when analysing.

---

## Not done yet

- **Real-device QA**: opening the built page from inside the Facebook app on Android and iOS,
  and confirming `session_end` fires there. Nothing else substitutes for this, and it is the
  environment nearly all the traffic will arrive from.
- **Live sink verification**: playing one full session on a handset and ticking off every event
  in PostHog's live view. The `?debug=1` overlay and `window.__events` exist for exactly this.
- **PostHog dashboards**: the 4 funnels, 7 insights and 2 cohorts from the plan (E.5) must be
  built before any spend, not after.
- **`/soon` collector**: the email form acknowledges locally and logs to the console; point it
  at the agreed endpoint (Cloudflare Worker + KV, Formspree, or a Google Form) before a paid
  test. The intent *click* is already recorded by the game, which is the metric that ranks
  ideas — the address is a bonus.
- **Decisions still open** (plan H.3): test market (this decides whether a consent gate is
  needed, which itself changes `first_input / page_open`), domain, analytics accounts, and the
  Go/Kill thresholds for `reject_recovered` and `self_continue`.
