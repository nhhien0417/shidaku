using System.Collections.Generic;
using UnityEngine;

namespace Shidaku.Editor
{
    /// <summary>
    /// "Auto-Cluster Sections" algorithm used by PuzzleDesignerWindow: splits a picture's playable
    /// cells into RECTANGULAR Sections via a simple two-tier grid cut - deliberately NOT organic/
    /// Voronoi (an earlier version of this class was). Reverse-engineered from a real reference
    /// Shikaku picture-puzzle game via frame-by-frame video analysis: several Sections' exact
    /// width/height were confirmed by matching the sum of their region-clue numbers to a cell count
    /// (e.g. 6+8+14+10+6+8+4+7 = 63 = a 9x7 Section; 6+12+9 = 27 = a 9x3 Section), and every single
    /// Section observed this way was at most ~9 cells wide while height varied freely (3 to 11+
    /// rows) - i.e. width is a hard cap, height is the flexible dimension used to balance content.
    ///
    ///   Tier 1: slice the picture into vertical column-strips, left to right.
    ///   Tier 2: within each strip, slice top to bottom into horizontal row-bands.
    ///
    /// Both tiers use the same <see cref="SplitLengthIntoSegments"/> helper: a randomized cut
    /// bounded to [Min, Max] per axis. Earlier greedy "always take Max, remainder gets whatever's
    /// left" cutting produced ugly slivers whenever the total didn't divide evenly (e.g. a 22-wide
    /// picture with Max=9 greedily cut 9+9+4 - a 4-wide sliver Section). The bounded-random splitter
    /// looks ahead instead: it never leaves a piece under Min unless the axis itself is shorter than
    /// Min, so every Section reads as a deliberate size, not a leftover - while the randomization
    /// (rather than always-equal division) keeps the grid from looking like a mechanically uniform
    /// checkerboard.
    ///
    /// This alone can strand a single cell on the "wrong" side of a grid cut relative to the rest
    /// of its same-color blob (a real example was found on video: an isolated 1-cell fragment sitting
    /// mid-row, separated from the rest of its color by a Section boundary). Left alone,
    /// PixelShikakuGenerator would have no choice but to emit a forced 1-cell region for that lone
    /// cell - it can't see the matching cells living in the neighboring Section. So a final pass,
    /// <see cref="FixStrayColorFragments"/>, reassigns any such stray single cell to whichever
    /// neighboring Section holds the rest of that same-color blob.
    ///
    /// That reassignment can occasionally grow the receiving Section's bounding box by exactly 1
    /// row/column past MaxSectionWidth/Height (confirmed by testing: only ever by 1, never more,
    /// since each fix moves a single cell). This is accepted on purpose - eliminating a guaranteed
    /// 1-cell region is a strictly worse outcome than a Section being very slightly oversized, and
    /// the "no 1-cell regions" rule has been this project's hard, repeatedly-reaffirmed priority
    /// throughout - so correctness wins this rare tie-break over the (video-estimated, not exact)
    /// size cap.
    /// </summary>
    public static class SectionClusterer
    {
        private const int MinSectionWidth = 5;
        private const int MaxSectionWidth = 9;
        private const int MinSectionHeight = 5;
        private const int MaxSectionHeight = 12;

        /// <summary>
        /// Returns a flat cell-&gt;section-index map (size w*h; -1 for non-playable cells; every
        /// playable cell assigned to exactly one 0-based section index, dense with no gaps).
        /// `sectionCount` is currently unused - Section size is driven entirely by the Min/Max
        /// bounds above (a target cell count would fight the "no ugly slivers" goal), kept only for
        /// call-site compatibility. `rng` drives the randomized-but-bounded segment split.
        /// </summary>
        public static int[] AutoAssignSections(int w, int h, Color32[] pixels, int sectionCount, System.Random rng)
        {
            var playable = new bool[w * h];
            int totalPlayable = 0;
            int minX = w, minY = h, maxX = -1, maxY = -1;
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int idx = y * w + x;
                    playable[idx] = pixels[idx].a != 0;
                    if (!playable[idx]) continue;
                    totalPlayable++;
                    if (x < minX) minX = x;
                    if (x > maxX) maxX = x;
                    if (y < minY) minY = y;
                    if (y > maxY) maxY = y;
                }
            }

            var rects = new List<RectInt>();
            if (totalPlayable > 0)
            {
                var stripWidths = SplitLengthIntoSegments(maxX - minX + 1, MinSectionWidth, MaxSectionWidth, rng);
                int stripX = minX;
                foreach (int stripWidth in stripWidths)
                {
                    BuildRowBandsForStrip(stripX, stripWidth, minY, maxY, w, playable, rng, rects);
                    stripX += stripWidth;
                }
            }

            var owner = new int[w * h];
            for (int i = 0; i < owner.Length; i++) owner[i] = -1;
            for (int i = 0; i < rects.Count; i++)
            {
                var r = rects[i];
                for (int y = r.y; y < r.y + r.height; y++)
                    for (int x = r.x; x < r.x + r.width; x++)
                    {
                        int idx = y * w + x;
                        if (playable[idx]) owner[idx] = i;
                    }
            }

            FixStrayColorFragments(owner, pixels, playable, w, h);
            MergeUndersizedSections(owner, w, h);

            return RemapDense(owner, playable, w, h);
        }

        /// <summary>
        /// Safety net for a side effect of FixStrayColorFragments: donating a cell away can shrink
        /// the donor's bounding box a lot if that cell was sparsely holding up one edge (common near
        /// a rounded/silhouette-heavy Section, which naturally has few playable cells at its rim) -
        /// exactly the "few columns/rows, looks tiny" case Min/MaxSectionWidth/Height exist to avoid.
        /// Any Section whose bounding box ends up under Min on either axis is folded wholesale into
        /// whichever neighbor shares the most border - it stops being a perfect rectangle at that
        /// point, but this only triggers for a rare, already-degenerate leftover, and a slightly
        /// irregular Section reads far better than a visibly tiny sliver one.
        /// </summary>
        private static void MergeUndersizedSections(int[] owner, int w, int h)
        {
            bool changed = true;
            while (changed)
            {
                changed = false;

                var cellsByOwner = new Dictionary<int, List<int>>();
                for (int i = 0; i < owner.Length; i++)
                {
                    if (owner[i] < 0) continue;
                    if (!cellsByOwner.TryGetValue(owner[i], out var list)) cellsByOwner[owner[i]] = list = new List<int>();
                    list.Add(i);
                }
                if (cellsByOwner.Count <= 1) return;

                foreach (var kv in cellsByOwner)
                {
                    int ownerId = kv.Key;
                    var cells = kv.Value;

                    int minX = w, minY = h, maxX = -1, maxY = -1;
                    foreach (var idx in cells)
                    {
                        int x = idx % w, y = idx / w;
                        if (x < minX) minX = x;
                        if (x > maxX) maxX = x;
                        if (y < minY) minY = y;
                        if (y > maxY) maxY = y;
                    }
                    int bw = maxX - minX + 1, bh = maxY - minY + 1;
                    if (bw >= MinSectionWidth && bh >= MinSectionHeight) continue;

                    var borderTally = new Dictionary<int, int>();
                    foreach (var idx in cells)
                    {
                        int x = idx % w, y = idx / w;
                        TallyNeighbor(x - 1, y);
                        TallyNeighbor(x + 1, y);
                        TallyNeighbor(x, y - 1);
                        TallyNeighbor(x, y + 1);

                        void TallyNeighbor(int nx, int ny)
                        {
                            if (nx < 0 || nx >= w || ny < 0 || ny >= h) return;
                            int nIdx = ny * w + nx;
                            int nOwner = owner[nIdx];
                            if (nOwner == -1 || nOwner == ownerId) return;
                            borderTally.TryGetValue(nOwner, out var c);
                            borderTally[nOwner] = c + 1;
                        }
                    }
                    if (borderTally.Count == 0) continue; // no bordering Section (shouldn't happen) - nothing to fold into

                    int bestNeighbor = -1, bestCount = -1;
                    foreach (var t in borderTally)
                        if (t.Value > bestCount) { bestCount = t.Value; bestNeighbor = t.Key; }

                    foreach (var idx in cells) owner[idx] = bestNeighbor;
                    changed = true;
                    break; // ownership changed - restart the scan
                }
            }
        }

        /// <summary>
        /// Randomized bounded segment split: divides `totalLength` into pieces each within
        /// [minLen, maxLen], with enough look-ahead that a piece is never left under minLen just
        /// because it happened to be what remained (the previous cut is shrunk instead, so the
        /// leftover is always either 0 or >= minLen). Falls back to a single undersized segment
        /// only when totalLength itself is already <= maxLen (nothing better is possible).
        /// </summary>
        private static List<int> SplitLengthIntoSegments(int totalLength, int minLen, int maxLen, System.Random rng)
        {
            var segments = new List<int>();
            if (totalLength <= maxLen)
            {
                segments.Add(totalLength);
                return segments;
            }

            int remaining = totalLength;
            while (remaining > maxLen)
            {
                int cut = minLen + rng.Next(maxLen - minLen + 1);
                int leftover = remaining - cut;
                if (leftover > 0 && leftover < minLen) cut = remaining - minLen; // keep the leftover valid for the next piece
                cut = Mathf.Clamp(cut, 1, maxLen);
                segments.Add(cut);
                remaining -= cut;
            }
            if (remaining > 0) segments.Add(remaining);
            return segments;
        }

        /// <summary>
        /// Tier 2: splits one vertical strip into horizontal row-bands via
        /// <see cref="SplitLengthIntoSegments"/> over the strip's OWN playable row range (not the
        /// whole picture's - a narrow strip near a rounded silhouette edge may only have playable
        /// cells over part of the picture's full height). A band that still ends up with zero
        /// playable cells (an internal silhouette gap) is dropped.
        /// </summary>
        private static void BuildRowBandsForStrip(int stripX, int stripWidth, int minY, int maxY, int fullWidth, bool[] playable, System.Random rng, List<RectInt> rects)
        {
            int stripMinY = -1, stripMaxY = -1;
            for (int y = minY; y <= maxY; y++)
            {
                bool anyPlayable = false;
                for (int x = stripX; x < stripX + stripWidth; x++)
                    if (playable[y * fullWidth + x]) { anyPlayable = true; break; }
                if (!anyPlayable) continue;
                if (stripMinY < 0) stripMinY = y;
                stripMaxY = y;
            }
            if (stripMinY < 0) return; // this strip has no playable cells at all

            var bandHeights = SplitLengthIntoSegments(stripMaxY - stripMinY + 1, MinSectionHeight, MaxSectionHeight, rng);
            int bandY = stripMinY;
            foreach (int bandHeight in bandHeights)
            {
                var rect = new RectInt(stripX, bandY, stripWidth, bandHeight);
                if (CountPlayableInRect(rect, fullWidth, playable) > 0) rects.Add(rect);
                bandY += bandHeight;
            }
        }

        private static int CountPlayableInRect(RectInt r, int fullWidth, bool[] playable)
        {
            int count = 0;
            for (int y = r.y; y < r.y + r.height; y++)
                for (int x = r.x; x < r.x + r.width; x++)
                    if (playable[y * fullWidth + x]) count++;
            return count;
        }

        /// <summary>
        /// Flood-fills the WHOLE picture by exact color (ignoring Section boundaries entirely - the
        /// same blob-detection PixelShikakuGenerator.BuildColorBlobs uses, just at picture scope
        /// instead of one Section's local array). For every such blob that a grid cut happened to
        /// split into pieces, any piece that is exactly 1 cell gets folded into whichever
        /// neighboring Section holds the largest adjacent piece of that same blob. A blob that is
        /// genuinely only 1 cell in the whole picture (not a fragmentation artifact - a real,
        /// unavoidable single-pixel color feature) is left untouched.
        /// </summary>
        private static void FixStrayColorFragments(int[] owner, Color32[] pixels, bool[] playable, int w, int h)
        {
            int n = w * h;
            var visited = new bool[n];
            var stack = new Stack<int>();

            for (int start = 0; start < n; start++)
            {
                if (visited[start] || !playable[start]) continue;

                var blobColor = pixels[start];
                var blobCells = new List<int>();
                stack.Push(start);
                visited[start] = true;
                while (stack.Count > 0)
                {
                    int idx = stack.Pop();
                    blobCells.Add(idx);
                    int x = idx % w, y = idx / w;
                    TryVisit(x - 1, y);
                    TryVisit(x + 1, y);
                    TryVisit(x, y - 1);
                    TryVisit(x, y + 1);

                    void TryVisit(int nx, int ny)
                    {
                        if (nx < 0 || nx >= w || ny < 0 || ny >= h) return;
                        int nIdx = ny * w + nx;
                        if (visited[nIdx] || !playable[nIdx]) return;
                        if (!pixels[nIdx].Equals(blobColor)) return;
                        visited[nIdx] = true;
                        stack.Push(nIdx);
                    }
                }

                if (blobCells.Count <= 1) continue; // genuine 1-pixel color feature, not a fragmentation artifact

                var byOwner = new Dictionary<int, List<int>>();
                foreach (var idx in blobCells)
                {
                    int o = owner[idx];
                    if (!byOwner.TryGetValue(o, out var list)) byOwner[o] = list = new List<int>();
                    list.Add(idx);
                }
                if (byOwner.Count <= 1) continue; // whole blob already in a single Section

                foreach (var kv in byOwner)
                {
                    if (kv.Value.Count != 1) continue;

                    int strayIdx = kv.Value[0];
                    int strayOwner = kv.Key;
                    int sx = strayIdx % w, sy = strayIdx / w;

                    var tally = new Dictionary<int, int>();
                    TallyNeighbor(sx - 1, sy);
                    TallyNeighbor(sx + 1, sy);
                    TallyNeighbor(sx, sy - 1);
                    TallyNeighbor(sx, sy + 1);

                    void TallyNeighbor(int nx, int ny)
                    {
                        if (nx < 0 || nx >= w || ny < 0 || ny >= h) return;
                        int nIdx = ny * w + nx;
                        if (!playable[nIdx] || !pixels[nIdx].Equals(blobColor)) return;
                        int nOwner = owner[nIdx];
                        if (nOwner == strayOwner) return;
                        tally.TryGetValue(nOwner, out var c);
                        tally[nOwner] = c + 1;
                    }

                    int bestOwner = -1, bestCount = -1;
                    foreach (var t in tally)
                        if (t.Value > bestCount) { bestCount = t.Value; bestOwner = t.Key; }

                    if (bestOwner >= 0) owner[strayIdx] = bestOwner;
                }
            }
        }

        private static int[] RemapDense(int[] owner, bool[] playable, int w, int h)
        {
            var remap = new Dictionary<int, int>();
            var result = new int[w * h];
            for (int i = 0; i < result.Length; i++) result[i] = -1;
            for (int i = 0; i < owner.Length; i++)
            {
                if (!playable[i]) continue;
                int raw = owner[i];
                if (!remap.TryGetValue(raw, out int dense))
                {
                    dense = remap.Count;
                    remap[raw] = dense;
                }
                result[i] = dense;
            }
            return result;
        }
    }
}
