using System.Collections.Generic;
using UnityEngine;

namespace Shidaku
{
    /// <summary>
    /// Real (non-trivial) Shikaku puzzle generator for one Section of a real picture.
    ///   1. Groups PLAYABLE cells into same-exact-color connected blobs (a Region may never span
    ///      two different source-art colors),
    ///   2. Splits each blob into 1..N rectangles sized 1-maxArea, weighted toward a mix of one/few
    ///      large pieces plus several small ones for visual variety — but within one blob, minimizing
    ///      the count of 1-cell regions always wins over variety (e.g. splitting a 7-cell strip as
    ///      5+2 rather than 6+1), with largest-remaining-region as the tie-break after that,
    ///      enforced both by clamping the cut-selection away from 1-cell slices up front and by a
    ///      final merge-away-singletons safety net,
    ///   3. Places the clue on a random cell within its region (still seed-deterministic),
    ///   4. HARD-validates the result covers every playable cell of the section exactly once
    ///      before returning — a previous version silently shipped sections with leftover
    ///      uncovered cells (unsolvable section), so this check is not optional.
    /// Ambiguity across the whole section's clue set is NOT a hard gate: the baked layout is the
    /// sole accepted answer regardless (runtime match is exact-rectangle comparison, not a live
    /// constraint solver), so ShikakuSolutionCounter is only a diagnostic warning here.
    ///
    /// The recursive split model (step 2) was reverse-engineered from a real reference Shikaku
    /// picture-puzzle game by frame-by-frame analysis of two gameplay recordings (every clue number
    /// + the underlying solved color layout was read back and cross-checked by hand against several
    /// single-color blobs). Confirmed properties that shaped the constants below - see each
    /// method's own doc for the specific formula:
    ///   - No "avoid duplicate area" rule of any kind - not even for the blob's own largest value
    ///     (a real blob was observed splitting into 6+6+4, i.e. a tied maximum).
    ///   - Splitting is a purely LOCAL, one-node-at-a-time recursive decision, never a global
    ///     "plan the whole blob's partition ahead of time" search - e.g. a 27-cell blob became
    ///     6+12+9 (not the more "consolidated" 15+12) because the very first cut only carved off a
    ///     modest 6, leaving 21 (already over the cap) to be forced-split afterwards; nothing ever
    ///     evaluates the 2-piece alternative.
    ///   - A rect small enough (<=6) is effectively always left whole; above that, "keep whole" is
    ///     still the common outcome even at 8-14, and hitting the cap (15) unsplit was rare - see
    ///     <see cref="SizeVarietyChance"/>.
    ///   - A voluntary split picks between two genuinely different cut styles - a lopsided
    ///     small-piece-off-the-side cut, and a near-50/50 cut that can legitimately tie two regions
    ///     at the same (possibly maximum) size - see <see cref="PickVarietyCut"/>.
    ///   - A non-rectangular (notched/staircase-shaped) blob's cuts are dictated by its own real
    ///     silhouette first (matches the picture's actual diagonal/step edges observed on-screen),
    ///     and only once a resulting piece is a clean rectangle does the above size-based logic
    ///     apply - see the `!allIn` branch of <see cref="SplitBlob"/>.
    /// </summary>
    public static class PixelShikakuGenerator
    {
        /// <summary>
        /// Generates a region set for one section. colors: flat Width*Height array local to this
        /// section; alpha==0 cells are not playable (holes / cells owned by a different section).
        /// Returns null (caller must treat the whole level as failed) if the generated regions do
        /// not exactly cover every playable cell — this should never happen by construction, but
        /// is verified rather than assumed.
        /// </summary>
        public static List<PuzzleRegion> GenerateSection(int width, int height, Color32[] colors, int maxArea, int seed)
        {
            var rng = new System.Random(seed);
            var rects = new List<RectInt>();

            foreach (var blobMask in BuildColorBlobs(width, height, colors))
            {
                var bounds = BoundingBox(blobMask, width, height);
                int blobArea = CountTrue(blobMask);
                rects.AddRange(GenerateBestBlobSplit(bounds, blobMask, width, blobArea, maxArea, rng));
            }

            bool[] playableMask = BuildAlphaMask(colors);
            if (!ValidateFullCoverage(rects, playableMask, width, height))
            {
                Debug.LogError("PixelShikakuGenerator: generated regions do NOT exactly cover this section's playable cells - rejecting.");
                return null;
            }

            var clues = new List<(Vector2Int cell, int area)>(rects.Count);
            var clueCells = new HashSet<Vector2Int>();
            foreach (var r in rects)
            {
                var clueCell = PickClueCell(r, rng);
                clues.Add((clueCell, r.width * r.height));
                clueCells.Add(clueCell);
            }

            // Diagnostic only (see class doc): a section's answer is fixed at bake time either way.
            int solutionCount = new ShikakuSolutionCounter(width, height, playableMask, clues, clueCells).CountUpTo(2);
            if (solutionCount != 1)
                Debug.LogWarning($"PixelShikakuGenerator: section has {(solutionCount == 0 ? "0 (unexpected)" : ">1")} " +
                                  "global tilings for its own clue set — baked layout is still the only accepted answer.");

            var result = new List<PuzzleRegion>(rects.Count);
            for (int i = 0; i < rects.Count; i++)
            {
                result.Add(new PuzzleRegion
                {
                    Rect = rects[i],
                    ClueCell = clues[i].cell,
                });
            }
            return result;
        }

        /// <summary>
        /// Rebuilds a coverage grid from the generated rects and compares it, cell-by-cell,
        /// against the section's real playable mask: every playable cell must be covered by
        /// EXACTLY one rect (no gaps, no overlaps), and no rect may cover a non-playable cell.
        /// </summary>
        private static bool ValidateFullCoverage(List<RectInt> rects, bool[] playableMask, int width, int height)
        {
            var covered = new bool[playableMask.Length];
            foreach (var r in rects)
            {
                for (int y = r.y; y < r.y + r.height; y++)
                {
                    for (int x = r.x; x < r.x + r.width; x++)
                    {
                        int idx = y * width + x;
                        if (!playableMask[idx]) return false; // region covers a hole
                        if (covered[idx]) return false;        // two regions overlap
                        covered[idx] = true;
                    }
                }
            }
            for (int i = 0; i < playableMask.Length; i++)
                if (playableMask[i] && !covered[i]) return false; // playable cell left uncovered
            return true;
        }

        // ---------------------------------------------------------------- color blobs

        /// <summary>4-connected flood fill grouping playable cells that share the exact same color.</summary>
        private static List<bool[]> BuildColorBlobs(int width, int height, Color32[] colors)
        {
            var blobs = new List<bool[]>();
            var visited = new bool[colors.Length];
            var stack = new Stack<int>();

            for (int start = 0; start < colors.Length; start++)
            {
                if (visited[start] || colors[start].a == 0) continue;

                var blobColor = colors[start];
                var mask = new bool[colors.Length];
                stack.Push(start);
                visited[start] = true;

                while (stack.Count > 0)
                {
                    int idx = stack.Pop();
                    mask[idx] = true;
                    int x = idx % width, y = idx / width;

                    TryVisit(x - 1, y);
                    TryVisit(x + 1, y);
                    TryVisit(x, y - 1);
                    TryVisit(x, y + 1);

                    void TryVisit(int nx, int ny)
                    {
                        if (nx < 0 || nx >= width || ny < 0 || ny >= height) return;
                        int nIdx = ny * width + nx;
                        if (visited[nIdx]) return;
                        if (!colors[nIdx].Equals(blobColor)) return;
                        visited[nIdx] = true;
                        stack.Push(nIdx);
                    }
                }

                blobs.Add(mask);
            }
            return blobs;
        }

        private static RectInt BoundingBox(bool[] mask, int width, int height)
        {
            int minX = width, minY = height, maxX = -1, maxY = -1;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (!mask[y * width + x]) continue;
                    if (x < minX) minX = x;
                    if (x > maxX) maxX = x;
                    if (y < minY) minY = y;
                    if (y > maxY) maxY = y;
                }
            }
            return new RectInt(minX, minY, maxX - minX + 1, maxY - minY + 1);
        }

        private static bool[] BuildAlphaMask(Color32[] colors)
        {
            var mask = new bool[colors.Length];
            for (int i = 0; i < colors.Length; i++) mask[i] = colors[i].a != 0;
            return mask;
        }

        /// <summary>Random cell within the rect (deterministic per the section's own seeded RNG).</summary>
        private static Vector2Int PickClueCell(RectInt r, System.Random rng)
        {
            int x = r.x + rng.Next(r.width);
            int y = r.y + rng.Next(r.height);
            return new Vector2Int(x, y);
        }

        private static int CountTrue(bool[] mask)
        {
            int count = 0;
            for (int i = 0; i < mask.Length; i++) if (mask[i]) count++;
            return count;
        }

        // ---------------------------------------------------------------- best-of-N split (one blob)

        /// <summary>How many times to retry a blob's split (with the shared RNG's next draws) when
        /// the result still has 1-cell regions after the merge safety net - a genuinely notch-shaped
        /// blob can occasionally force a 1-cell leaf that no pairwise merge can absorb (verified via
        /// a synthetic L-shaped test: ~0.7% of random splits), and a different cut sequence usually
        /// avoids it entirely. Cheap: only re-runs the split for the one blob still short of ideal.</summary>
        private const int MaxSplitAttempts = 16;

        /// <summary>
        /// Runs SplitBlob (+ the merge safety net) up to <see cref="MaxSplitAttempts"/> times and
        /// keeps the best result by the exact priority the puzzle's authoring rule specifies: fewest
        /// 1-cell regions first, then the largest single region as the tie-break. Stops early once
        /// a 0-singleton split is found. Each attempt consumes further draws from the same seeded
        /// rng, so the overall section generation stays fully seed-deterministic.
        /// </summary>
        private static List<RectInt> GenerateBestBlobSplit(RectInt bounds, bool[] blobMask, int fullWidth, int blobArea, int maxArea, System.Random rng)
        {
            List<RectInt> best = null;
            int bestSingletons = int.MaxValue;
            int bestMaxArea = -1;

            for (int attempt = 0; attempt < MaxSplitAttempts; attempt++)
            {
                var voluntaryBudget = new[] { blobArea <= 6 ? 0 : blobArea <= 10 ? 1 : 2 };
                var candidate = new List<RectInt>();
                SplitBlob(bounds, blobMask, fullWidth, candidate, rng, maxArea, voluntaryBudget);
                MergeAwaySingleCellRegions(candidate, blobMask, fullWidth, maxArea);

                int singletons = 0, maxRegionArea = 0;
                foreach (var r in candidate)
                {
                    int area = r.width * r.height;
                    if (area == 1) singletons++;
                    if (area > maxRegionArea) maxRegionArea = area;
                }

                bool better = best == null || singletons < bestSingletons ||
                              (singletons == bestSingletons && maxRegionArea > bestMaxArea);
                if (better)
                {
                    best = candidate;
                    bestSingletons = singletons;
                    bestMaxArea = maxRegionArea;
                }

                if (bestSingletons == 0) break;
            }

            return best;
        }

        // ---------------------------------------------------------------- singleton cleanup (one blob)

        /// <summary>
        /// Safety net for the #1 authoring rule: within one same-color blob, produce as few 1-cell
        /// regions as possible. The cut-selection heuristics below already steer away from ever
        /// creating a 1-cell piece, but a real (non-rectangular) blob shape can still force one via
        /// PickShapeCut. Whenever a 1-cell leaf remains, greedily fold it into whichever adjacent
        /// same-blob rect it can form a clean merged rectangle with — preferring the merge that
        /// yields the LARGEST resulting area (the tie-break rule) — as long as the merge stays
        /// within maxArea and the merged rectangle is still fully inside the blob (never merges
        /// across a real color/shape boundary). Repeats until no 1-cell region can be improved.
        /// </summary>
        private static void MergeAwaySingleCellRegions(List<RectInt> rects, bool[] blobMask, int fullWidth, int maxArea)
        {
            bool merged = true;
            while (merged)
            {
                merged = false;
                for (int i = 0; i < rects.Count; i++)
                {
                    if (rects[i].width * rects[i].height != 1) continue;

                    int bestJ = -1;
                    int bestArea = -1;
                    RectInt bestRect = default;
                    for (int j = 0; j < rects.Count; j++)
                    {
                        if (j == i) continue;
                        if (!TryMergeRects(rects[i], rects[j], out var candidate)) continue;
                        int area = candidate.width * candidate.height;
                        if (area > maxArea) continue;
                        if (!RectFullyInMask(candidate, blobMask, fullWidth)) continue;
                        if (area > bestArea) { bestArea = area; bestJ = j; bestRect = candidate; }
                    }

                    if (bestJ >= 0)
                    {
                        rects[bestJ] = bestRect;
                        rects.RemoveAt(i);
                        merged = true;
                        break; // list mutated - restart the scan
                    }
                }
            }
        }

        /// <summary>Two rects combine into one exact rectangle only if they share a full flush edge
        /// (same position+length on one axis, touching on the other) - anything else is rejected
        /// rather than approximated, so a merge can never silently swallow/skip cells.</summary>
        private static bool TryMergeRects(RectInt a, RectInt b, out RectInt merged)
        {
            merged = default;
            if (a.y == b.y && a.height == b.height)
            {
                if (a.x + a.width == b.x) { merged = new RectInt(a.x, a.y, a.width + b.width, a.height); return true; }
                if (b.x + b.width == a.x) { merged = new RectInt(b.x, a.y, a.width + b.width, a.height); return true; }
            }
            if (a.x == b.x && a.width == b.width)
            {
                if (a.y + a.height == b.y) { merged = new RectInt(a.x, a.y, a.width, a.height + b.height); return true; }
                if (b.y + b.height == a.y) { merged = new RectInt(a.x, b.y, a.width, a.height + b.height); return true; }
            }
            return false;
        }

        private static bool RectFullyInMask(RectInt r, bool[] mask, int fullWidth)
        {
            for (int y = r.y; y < r.y + r.height; y++)
                for (int x = r.x; x < r.x + r.width; x++)
                    if (!mask[y * fullWidth + x]) return false;
            return true;
        }

        // ---------------------------------------------------------------- mask-aware recursive split (one blob)

        /// <summary>
        /// voluntaryBudget[0]: how many more "purely for variety" splits this blob's whole subtree
        /// is still allowed to make (shared/decremented across the recursion, not re-rolled fresh
        /// per node) - keeps a big blob's split count predictable instead of every recursive call
        /// independently re-rolling a chance to fragment further.
        /// </summary>
        private static void SplitBlob(RectInt rect, bool[] blobMask, int fullWidth, List<RectInt> output, System.Random rng, int maxArea, int[] voluntaryBudget)
        {
            bool anyIn = false, allIn = true;
            for (int y = rect.y; y < rect.y + rect.height; y++)
            {
                for (int x = rect.x; x < rect.x + rect.width; x++)
                {
                    bool inBlob = blobMask[y * fullWidth + x];
                    anyIn |= inBlob;
                    allIn &= inBlob;
                }
            }

            if (!anyIn) return; // fully outside this blob (a "notch" in a non-rectangular blob shape) - discard

            bool canSplitW = rect.width >= 2;
            bool canSplitH = rect.height >= 2;
            int area = rect.width * rect.height;

            bool mustSplit = allIn && area > maxArea;
            if (allIn && !mustSplit)
            {
                bool wantsVariety = voluntaryBudget[0] > 0 && (canSplitW || canSplitH) && rng.NextDouble() < SizeVarietyChance(area, maxArea);
                if (!wantsVariety)
                {
                    output.Add(rect);
                    return;
                }
                voluntaryBudget[0]--;
            }

            if (!canSplitW && !canSplitH)
            {
                if (allIn) output.Add(rect); // 1x1 leaf - always accepted regardless of maxArea/variety
                return;
            }

            bool splitW;
            if (!allIn)
            {
                // This rect still contains a notch/hole (a non-rectangular blob shape). Which axis
                // to cut along first is a correctness/quality choice here, not an aesthetic one: a
                // notch (e.g. a single-cell "flag" sticking off a bigger rectangle) can only be
                // absorbed into a same-size-or-bigger neighbor from ONE of the two axis orders - the
                // other order stalls into forced 1-cell leaves. So (unlike the size-driven branches
                // below) don't hard-bias toward the longer axis; a 50/50 choice lets
                // GenerateBestBlobSplit's retries actually reach whichever axis order avoids it.
                // (Also matches the reference game: a real staircase-shaped blob's cuts followed
                // its own diagonal silhouette rather than any fixed axis preference - see class doc.)
                splitW = canSplitW && canSplitH ? rng.Next(2) == 0 : canSplitW;
            }
            else
            {
                splitW = canSplitW && canSplitH
                    ? rect.width > rect.height || (rect.width == rect.height && rng.Next(2) == 0)
                    : canSplitW;
            }

            int otherDim = splitW ? rect.height : rect.width;
            int dimSize = splitW ? rect.width : rect.height;
            int cut;
            if (!allIn)
            {
                // Cut along where the blob's own boundary actually steps, not by a size heuristic.
                cut = PickShapeCut(blobMask, fullWidth, rect, splitW, rng);
            }
            else if (mustSplit)
            {
                // Forced split (blob bigger than the area cap): greedily carve off one near-cap
                // "big" region so a large blob comes out as one big piece + a smaller remainder
                // chain, rather than two arbitrary mid-size halves that both fragment further.
                cut = PickForcedCut(dimSize, otherDim, maxArea, rng);
            }
            else
            {
                cut = PickVarietyCut(dimSize, otherDim, rng);
            }

            if (splitW)
            {
                SplitBlob(new RectInt(rect.x, rect.y, cut, rect.height), blobMask, fullWidth, output, rng, maxArea, voluntaryBudget);
                SplitBlob(new RectInt(rect.x + cut, rect.y, rect.width - cut, rect.height), blobMask, fullWidth, output, rng, maxArea, voluntaryBudget);
            }
            else
            {
                SplitBlob(new RectInt(rect.x, rect.y, rect.width, cut), blobMask, fullWidth, output, rng, maxArea, voluntaryBudget);
                SplitBlob(new RectInt(rect.x, rect.y + cut, rect.width, rect.height - cut), blobMask, fullWidth, output, rng, maxArea, voluntaryBudget);
            }
        }

        /// <summary>
        /// Chance of splitting an already-acceptable (allIn, area&lt;=maxArea) rect further anyway,
        /// for size variety. A small patch NEVER splits voluntarily - it should always come out as
        /// the single biggest region its own color allows (a 2x2 same-color block must stay one
        /// "4", never four "1"s just because a leftover voluntary-split budget from a bigger
        /// ancestor blob got lucky/unlucky on the RNG roll). Variety only kicks in for genuinely
        /// large patches, where breaking into a big chunk + a small remainder actually reads as
        /// intentional instead of confetti.
        ///
        /// Curve calibrated against the reference game (see class doc): "keep whole" was the
        /// observed majority outcome all the way up to area~14 (an 8-cell rect, for one, was seen
        /// staying a single "8" region far more often than not - by this curve, ~85% of the time),
        /// while hitting the cap (15) unsplit was seen only once across everything sampled - hence
        /// the curve keeps split-chance modest through the low-teens and only reaches its highest
        /// value (still well under 50%) right at the cap.
        /// </summary>
        private static float SizeVarietyChance(int area, int maxArea)
        {
            if (area <= 6) return 0f;
            float t = Mathf.Clamp01((area - 6f) / Mathf.Max(1f, maxArea - 6f));
            return Mathf.Lerp(0.08f, 0.4f, t);
        }

        /// <summary>
        /// Picks a cut position biased toward asymmetric splits (one big + one small side) about
        /// a quarter of the time, and a looser near-balanced split otherwise. Either way, the cut
        /// is kept away from producing a 1-cell side whenever the rect's own shape allows it: a
        /// 1-wide/1-tall strip (otherDim == 1) cutting at position 1 or dimSize-1 would yield a
        /// lone 1-cell region (e.g. splitting 7 as "6, 1") — instead the cut range is clamped to
        /// [2, dimSize-2] so the same strip comes out as e.g. "5, 2" instead, satisfying the rule
        /// "fewest 1-cell regions first, biggest remaining region second". A 2D rect (otherDim >= 2)
        /// can never produce a 1-cell piece from a single cut regardless of position, so it's left
        /// unclamped for full variety.
        ///
        /// Both branches are load-bearing, confirmed against the reference game (see class doc):
        /// the 25% "asymmetric" branch matches an observed lopsided split (a 27-cell strip's first
        /// cut carved off just 6, leaving 21 to keep recursing); the other 75% branch's frac range
        /// (0.35-0.65) matches an observed near-dead-center split (a 12-cell rect cut into 6+6,
        /// i.e. two tied regions - which is also that blob's own largest value, confirming the
        /// reference game has no "avoid tying the max" rule either).
        /// </summary>
        private static int PickVarietyCut(int dimSize, int otherDim, System.Random rng)
        {
            bool mustAvoidUnitSlice = otherDim == 1 && dimSize > 3;
            int minCut = mustAvoidUnitSlice ? 2 : 1;
            int maxCut = mustAvoidUnitSlice ? dimSize - 2 : dimSize - 1;

            if (rng.NextDouble() < 0.25)
            {
                int smallRange = Mathf.Max(1, Mathf.Min(3, maxCut - minCut + 1));
                int smallSide = minCut + rng.Next(smallRange);
                return rng.Next(2) == 0 ? smallSide : dimSize - smallSide;
            }
            else
            {
                float frac = 0.35f + (float)rng.NextDouble() * 0.3f; // 0.35 - 0.65
                return Mathf.Clamp(Mathf.RoundToInt(dimSize * frac), minCut, maxCut);
            }
        }

        /// <summary>
        /// Greedily carves off one side as close to (but not over) maxArea as this axis allows,
        /// so a forced split (blob bigger than the cap) yields one near-cap "big" region plus a
        /// smaller remainder to keep recursing on. Same 1-cell-avoidance clamp as PickVarietyCut.
        /// </summary>
        private static int PickForcedCut(int dimSize, int otherDim, int maxArea, System.Random rng)
        {
            bool mustAvoidUnitSlice = otherDim == 1 && dimSize > 3;
            int minCut = mustAvoidUnitSlice ? 2 : 1;
            int maxCut = mustAvoidUnitSlice ? dimSize - 2 : dimSize - 1;

            int ideal = Mathf.Clamp(maxArea / Mathf.Max(1, otherDim), minCut, maxCut);
            int jitter = rng.Next(-1, 2);
            int cut = Mathf.Clamp(ideal + jitter, minCut, maxCut);
            return rng.Next(2) == 0 ? cut : dimSize - cut;
        }

        /// <summary>
        /// Picks a cut position biased toward where this non-rectangular blob's own silhouette
        /// actually steps (a notch/corner), so carving out its holes takes few, clean cuts.
        /// </summary>
        private static int PickShapeCut(bool[] blobMask, int fullWidth, RectInt rect, bool vertical, System.Random rng)
        {
            int n = (vertical ? rect.width : rect.height) - 1;
            var weights = new float[n];
            float total = 0f;
            for (int i = 0; i < n; i++)
            {
                int cutPos = i + 1;
                int score = 0;
                if (vertical)
                {
                    int xa = rect.x + cutPos - 1, xb = rect.x + cutPos;
                    for (int y = rect.y; y < rect.y + rect.height; y++)
                        if (blobMask[y * fullWidth + xa] != blobMask[y * fullWidth + xb]) score++;
                }
                else
                {
                    int ya = rect.y + cutPos - 1, yb = rect.y + cutPos;
                    for (int x = rect.x; x < rect.x + rect.width; x++)
                        if (blobMask[ya * fullWidth + x] != blobMask[yb * fullWidth + x]) score++;
                }
                weights[i] = 1f + score * 4f;
                total += weights[i];
            }

            float pick = (float)(rng.NextDouble() * total);
            float acc = 0f;
            for (int i = 0; i < n; i++)
            {
                acc += weights[i];
                if (pick <= acc) return i + 1;
            }
            return n;
        }
    }
}
