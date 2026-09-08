#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Shidaku.Editor
{
    /// <summary>
    /// Grid-aware downscaler for importing REFERENCE-CHART-style pixel art (a picture drawn at a
    /// large real resolution with thin grid lines baked in between logical cells - e.g. a
    /// cross-stitch/perler-bead pattern chart) into a proper 1-cell-per-pixel Template.
    ///
    /// Ported from smart-queens' PixelArtImageProcessor (see
    /// D:\hienn\Projects\smart-queens\Assets\_Game\Scripts\Design\LevelDesign\ArtDesign) - the
    /// edge-detection/grid-band logic there is completely generic (not Queens-specific), so it's
    /// reused near-verbatim. What changed: no EditorWindow/menu of its own and no "save a
    /// _TruePixel PNG to disk" step - this returns the downscaled Color32[] directly in memory so
    /// TemplateDesignerWindow's Import flow can feed it straight into the paint canvas.
    ///
    /// This is for pictures that are NOT already pixel-perfect (unlike Assets/_Game/Demo/SourceArt,
    /// which is already 1 image pixel = 1 game cell and should use the plain 1:1 import instead).
    /// </summary>
    public static class ArtImageProcessor
    {
        private struct EdgeCluster
        {
            public int Start, End, Peak;
            public EdgeCluster(int start, int end, int peak) { Start = start; End = end; Peak = peak; }
        }

        private struct GridBand
        {
            public int Start, End;
            public GridBand(int start, int end) { Start = start; End = end; }
        }

        /// <summary>A detected cell's full pixel extent along one axis (not just its center) - needed
        /// so sampling can read a whole interior block instead of one single point.</summary>
        private struct SampleRange
        {
            public int Start, End;
            public SampleRange(int start, int end) { Start = start; End = end; }
            public int Center => (Start + End) / 2;
        }

        /// <summary>
        /// Detects the picture's logical cell grid (via color-change edge density along each axis)
        /// and samples one color per detected cell. Returns false if no grid could be detected (e.g.
        /// the image is already tiny/pixel-perfect - use the plain 1:1 import for those instead).
        ///
        /// Two de-noising passes keep cells that are meant to be the exact same flat color from
        /// drifting into slightly-different Color32 values (which breaks the "1 region = 1 color"
        /// exact-match invariant everywhere downstream - blob flood-fill, region validation, etc.):
        ///   1. Each cell is sampled as the per-channel MEDIAN over a shrunk interior block (trimmed
        ///      away from the grid-line/anti-aliasing edges), not a single source.GetPixel point -
        ///      a lone noisy/compressed pixel can no longer decide the whole cell's color.
        ///   2. After downscaling, colorSnapTolerance clusters any still-near-duplicate colors
        ///      (residual noise, or the source simply having off-by-one-bit shading) down to a
        ///      single canonical Color32 per visual cluster - the same squared-distance test
        ///      RemoveBackgroundFromEdges already uses for its own flood fill.
        /// </summary>
        public static bool TryDetectAndDownscale(Texture2D source, out Color32[] pixels, out int width, out int height,
            bool removeBackground = true, float backgroundTolerance = 0.08f, float colorSnapTolerance = 0.05f)
        {
            pixels = null;
            width = height = 0;
            if (source == null) return false;

            int sourceWidth = source.width, sourceHeight = source.height;
            Color[] sourcePixels = source.GetPixels();

            float[] grays = new float[sourcePixels.Length];
            for (int i = 0; i < sourcePixels.Length; i++) grays[i] = sourcePixels[i].grayscale;

            float[] verticalDiffs = new float[Mathf.Max(0, sourceWidth - 1)];
            for (int x = 1; x < sourceWidth; x++)
            {
                float diffSum = 0f;
                for (int y = 0; y < sourceHeight; y++)
                    diffSum += Mathf.Abs(grays[y * sourceWidth + x] - grays[y * sourceWidth + x - 1]);
                verticalDiffs[x - 1] = diffSum;
            }

            float[] horizontalDiffs = new float[Mathf.Max(0, sourceHeight - 1)];
            for (int y = 1; y < sourceHeight; y++)
            {
                float diffSum = 0f;
                for (int x = 0; x < sourceWidth; x++)
                    diffSum += Mathf.Abs(grays[y * sourceWidth + x] - grays[(y - 1) * sourceWidth + x]);
                horizontalDiffs[y - 1] = diffSum;
            }

            const int minCellSize = 4;
            const float thresholdRatio = 0.3f;
            List<GridBand> verticalGridBands = DetectGridBands(verticalDiffs, sourceWidth, minCellSize, thresholdRatio);
            List<GridBand> horizontalGridBands = DetectGridBands(horizontalDiffs, sourceHeight, minCellSize, thresholdRatio);
            int widthScale = GetRepresentativeCellSize(verticalGridBands, minCellSize);
            int heightScale = GetRepresentativeCellSize(horizontalGridBands, minCellSize);
            List<SampleRange> xSamples = BuildSampleRanges(sourceWidth, verticalGridBands, widthScale, minCellSize);
            List<SampleRange> ySamples = BuildSampleRanges(sourceHeight, horizontalGridBands, heightScale, minCellSize);

            if (xSamples.Count == 0 || ySamples.Count == 0)
            {
                Debug.LogWarning($"ArtImageProcessor: could not detect a grid for '{source.name}' (widthScale={widthScale}, heightScale={heightScale}). Use plain 1:1 import if this picture is already pixel-perfect.");
                return false;
            }

            int newWidth = xSamples.Count, newHeight = ySamples.Count;
            var result = new Color32[newWidth * newHeight];
            for (int y = 0; y < newHeight; y++)
            {
                var yRange = ySamples[y];
                for (int x = 0; x < newWidth; x++)
                {
                    var xRange = xSamples[x];
                    result[y * newWidth + x] = SampleRobustCellColor(source, xRange, yRange);
                }
            }

            int removedBackgroundPixels = 0;
            if (removeBackground)
                removedBackgroundPixels = RemoveBackgroundFromEdges(result, newWidth, newHeight, backgroundTolerance);

            int paletteSize = SnapColorsToPalette(result, colorSnapTolerance);

            pixels = result;
            width = newWidth;
            height = newHeight;
            Debug.Log($"ArtImageProcessor: downscaled '{source.name}' {sourceWidth}x{sourceHeight} -> {newWidth}x{newHeight}. " +
                      $"Grid bands={verticalGridBands.Count}x{horizontalGridBands.Count}, cell~{widthScale}x{heightScale}, " +
                      $"removedBackgroundPixels={removedBackgroundPixels}, palette={paletteSize} colors.");
            return true;
        }

        /// <summary>
        /// Reads the cell's block of source pixels (shrunk ~20% inward on each side so grid-line
        /// bleed/anti-aliasing at the very edge is excluded) and returns the per-channel MEDIAN -
        /// robust against a minority of noisy/compressed pixels within an otherwise-flat cell,
        /// without the cost/complexity of a full mode-color histogram.
        /// </summary>
        private static Color32 SampleRobustCellColor(Texture2D source, SampleRange xRange, SampleRange yRange)
        {
            int fullW = xRange.End - xRange.Start + 1;
            int fullH = yRange.End - yRange.Start + 1;
            int marginX = fullW / 5; // trim ~20% off each side
            int marginY = fullH / 5;

            int sx = Mathf.Clamp(xRange.Start + marginX, xRange.Start, xRange.End);
            int ex = Mathf.Clamp(xRange.End - marginX, sx, xRange.End);
            int sy = Mathf.Clamp(yRange.Start + marginY, yRange.Start, yRange.End);
            int ey = Mathf.Clamp(yRange.End - marginY, sy, yRange.End);

            int blockW = ex - sx + 1, blockH = ey - sy + 1;
            Color[] block = source.GetPixels(sx, sy, blockW, blockH);
            if (block.Length == 1) return block[0];

            int n = block.Length;
            var rs = new byte[n]; var gs = new byte[n]; var bs = new byte[n]; var als = new byte[n];
            for (int i = 0; i < n; i++)
            {
                Color32 c = block[i];
                rs[i] = c.r; gs[i] = c.g; bs[i] = c.b; als[i] = c.a;
            }
            System.Array.Sort(rs); System.Array.Sort(gs); System.Array.Sort(bs); System.Array.Sort(als);
            int mid = n / 2;
            return new Color32(rs[mid], gs[mid], bs[mid], als[mid]);
        }

        /// <summary>
        /// Collapses near-duplicate colors (leftover sampling noise, or a source that shades what's
        /// meant to be one flat color across a few off-by-one-bit values) down to a single canonical
        /// Color32 per visual cluster, using the same squared-distance test as background removal.
        /// Greedy/order-dependent (first-seen shade in each cluster becomes canonical) but stable and
        /// deterministic for a given image. Skips fully-transparent (already-removed-background)
        /// cells. Returns the resulting palette size, purely for the diagnostic log line.
        /// </summary>
        private static int SnapColorsToPalette(Color32[] pixels, float tolerance)
        {
            var palette = new List<Color32>();
            for (int i = 0; i < pixels.Length; i++)
            {
                if (pixels[i].a == 0) continue;

                int match = -1;
                for (int p = 0; p < palette.Count; p++)
                {
                    if (IsSimilarColor(pixels[i], palette[p], tolerance)) { match = p; break; }
                }

                if (match >= 0) pixels[i] = palette[match];
                else palette.Add(pixels[i]);
            }
            return palette.Count;
        }

        private static List<EdgeCluster> DetectEdgeClusters(float[] sumDiff, int minSize, float thresholdRatio)
        {
            int length = sumDiff.Length;
            if (length < minSize * 2) return new List<EdgeCluster>();

            float maxValue = 0f;
            foreach (float value in sumDiff) if (value > maxValue) maxValue = value;
            if (maxValue == 0f) return new List<EdgeCluster>();

            float threshold = thresholdRatio * maxValue;
            List<EdgeCluster> clusters = new();

            int clusterStart = -1;
            float clusterMax = 0f;
            int clusterPeak = 0;
            for (int i = 0; i < length; i++)
            {
                if (sumDiff[i] > threshold)
                {
                    if (clusterStart < 0) { clusterStart = i; clusterMax = sumDiff[i]; clusterPeak = i; }
                    else if (sumDiff[i] > clusterMax) { clusterMax = sumDiff[i]; clusterPeak = i; }
                }
                else if (clusterStart >= 0)
                {
                    clusters.Add(new EdgeCluster(clusterStart, i - 1, clusterPeak));
                    clusterStart = -1;
                }
            }
            if (clusterStart >= 0) clusters.Add(new EdgeCluster(clusterStart, length - 1, clusterPeak));
            return clusters;
        }

        private static int EstimateMaxGapInsideGridBand(List<EdgeCluster> edgeClusters, int minSize)
        {
            if (edgeClusters.Count < 2) return 0;

            List<int> gaps = new();
            for (int i = 1; i < edgeClusters.Count; i++)
            {
                int gap = edgeClusters[i].Start - edgeClusters[i - 1].End;
                if (gap > 0) gaps.Add(gap);
            }
            if (gaps.Count < 2) return Mathf.Max(1, minSize / 2);

            gaps.Sort();
            float bestRatio = 1f;
            int bestLower = 0, bestUpper = 0;
            for (int i = 0; i < gaps.Count - 1; i++)
            {
                int lower = gaps[i], upper = gaps[i + 1];
                float ratio = (upper + 1f) / (lower + 1f);
                if (ratio > bestRatio && upper - lower >= minSize) { bestRatio = ratio; bestLower = lower; bestUpper = upper; }
            }
            return bestRatio >= 1.8f ? Mathf.Max(1, (bestLower + bestUpper) / 2) : Mathf.Max(1, minSize / 2);
        }

        private static List<GridBand> DetectGridBands(float[] sumDiff, int textureLength, int minSize, float thresholdRatio)
        {
            List<EdgeCluster> edgeClusters = DetectEdgeClusters(sumDiff, minSize, thresholdRatio);
            List<GridBand> bands = new();
            if (edgeClusters.Count == 0) return bands;

            int maxGapInsideBand = EstimateMaxGapInsideGridBand(edgeClusters, minSize);
            int bandStartIndex = 0;
            for (int i = 1; i < edgeClusters.Count; i++)
            {
                int gap = edgeClusters[i].Start - edgeClusters[i - 1].End;
                if (gap > maxGapInsideBand)
                {
                    AddGridBand(bands, edgeClusters, bandStartIndex, i - 1, textureLength);
                    bandStartIndex = i;
                }
            }
            AddGridBand(bands, edgeClusters, bandStartIndex, edgeClusters.Count - 1, textureLength);
            return bands;
        }

        private static void AddGridBand(List<GridBand> bands, List<EdgeCluster> edgeClusters, int firstIndex, int lastIndex, int textureLength)
        {
            int start = Mathf.Clamp(edgeClusters[firstIndex].Start + 1, 0, textureLength - 1);
            int end = Mathf.Clamp(edgeClusters[lastIndex].End, 0, textureLength - 1);
            if (end < start)
            {
                int center = Mathf.Clamp(edgeClusters[firstIndex].Peak, 0, textureLength - 1);
                start = center; end = center;
            }

            if (bands.Count > 0 && start <= bands[bands.Count - 1].End + 1)
            {
                GridBand previous = bands[bands.Count - 1];
                previous.End = Mathf.Max(previous.End, end);
                bands[bands.Count - 1] = previous;
                return;
            }
            bands.Add(new GridBand(start, end));
        }

        private static int GetRepresentativeCellSize(List<GridBand> gridBands, int minSize)
        {
            if (gridBands.Count < 2) return 0;

            List<int> distances = new();
            for (int i = 1; i < gridBands.Count; i++)
            {
                int contentSpan = gridBands[i].Start - gridBands[i - 1].End - 1;
                if (contentSpan >= minSize) distances.Add(contentSpan);
            }
            if (distances.Count == 0) return 0;

            distances.Sort();
            return distances[distances.Count / 2];
        }

        private static List<SampleRange> BuildSampleRanges(int textureLength, List<GridBand> gridBands, int fallbackCellSize, int minCellSize)
        {
            List<SampleRange> samples = new();

            if (gridBands.Count > 0)
            {
                gridBands.Sort((a, b) => a.Start.CompareTo(b.Start));
                int minEdgeSpan = fallbackCellSize > 0 ? Mathf.Max(minCellSize, fallbackCellSize / 2) : minCellSize;

                if (gridBands[0].Start >= minEdgeSpan)
                    samples.Add(ClampRange(0, gridBands[0].Start - 1, textureLength));

                for (int i = 0; i < gridBands.Count - 1; i++)
                {
                    int contentStart = gridBands[i].End + 1;
                    int contentEnd = gridBands[i + 1].Start - 1;
                    int span = contentEnd - contentStart + 1;
                    if (span < minCellSize) continue;

                    samples.Add(ClampRange(contentStart, contentEnd, textureLength));
                }

                int trailingStart = gridBands[gridBands.Count - 1].End + 1;
                int trailingSpan = textureLength - trailingStart;
                if (trailingSpan >= minEdgeSpan)
                    samples.Add(ClampRange(trailingStart, textureLength - 1, textureLength));
            }

            if (samples.Count > 0 || fallbackCellSize <= 0) return samples;

            int fallbackCount = textureLength / fallbackCellSize;
            for (int i = 0; i < fallbackCount; i++)
                samples.Add(ClampRange(i * fallbackCellSize, i * fallbackCellSize + fallbackCellSize - 1, textureLength));
            return samples;
        }

        private static SampleRange ClampRange(int start, int end, int textureLength)
        {
            start = Mathf.Clamp(start, 0, textureLength - 1);
            end = Mathf.Clamp(end, start, textureLength - 1);
            return new SampleRange(start, end);
        }

        private static int RemoveBackgroundFromEdges(Color32[] pixels, int width, int height, float tolerance)
        {
            if (width == 0 || height == 0) return 0;

            Color32 backgroundColor = GetDominantBorderColor(pixels, width, height);
            bool[] queued = new bool[pixels.Length];
            Queue<int> queue = new();

            for (int x = 0; x < width; x++)
            {
                TryQueueBackgroundPixel(x, pixels, queued, queue, backgroundColor, tolerance);
                TryQueueBackgroundPixel((height - 1) * width + x, pixels, queued, queue, backgroundColor, tolerance);
            }
            for (int y = 1; y < height - 1; y++)
            {
                TryQueueBackgroundPixel(y * width, pixels, queued, queue, backgroundColor, tolerance);
                TryQueueBackgroundPixel(y * width + width - 1, pixels, queued, queue, backgroundColor, tolerance);
            }

            int removedCount = 0;
            while (queue.Count > 0)
            {
                int index = queue.Dequeue();
                int x = index % width, y = index / width;

                pixels[index] = new Color32(0, 0, 0, 0);
                removedCount++;

                if (x > 0) TryQueueBackgroundPixel(index - 1, pixels, queued, queue, backgroundColor, tolerance);
                if (x < width - 1) TryQueueBackgroundPixel(index + 1, pixels, queued, queue, backgroundColor, tolerance);
                if (y > 0) TryQueueBackgroundPixel(index - width, pixels, queued, queue, backgroundColor, tolerance);
                if (y < height - 1) TryQueueBackgroundPixel(index + width, pixels, queued, queue, backgroundColor, tolerance);
            }
            return removedCount;
        }

        private static Color32 GetDominantBorderColor(Color32[] pixels, int width, int height)
        {
            Dictionary<int, int> colorCounts = new();

            for (int x = 0; x < width; x++)
            {
                CountBorderColor(colorCounts, pixels[x]);
                CountBorderColor(colorCounts, pixels[(height - 1) * width + x]);
            }
            for (int y = 1; y < height - 1; y++)
            {
                CountBorderColor(colorCounts, pixels[y * width]);
                CountBorderColor(colorCounts, pixels[y * width + width - 1]);
            }

            int dominantKey = 0, dominantCount = -1;
            foreach (var pair in colorCounts)
                if (pair.Value > dominantCount) { dominantKey = pair.Key; dominantCount = pair.Value; }

            int r = 0, g = 0, b = 0, a = 0, count = 0;
            for (int x = 0; x < width; x++)
            {
                AccumulateColorForKey(pixels[x], dominantKey, ref r, ref g, ref b, ref a, ref count);
                AccumulateColorForKey(pixels[(height - 1) * width + x], dominantKey, ref r, ref g, ref b, ref a, ref count);
            }
            for (int y = 1; y < height - 1; y++)
            {
                AccumulateColorForKey(pixels[y * width], dominantKey, ref r, ref g, ref b, ref a, ref count);
                AccumulateColorForKey(pixels[y * width + width - 1], dominantKey, ref r, ref g, ref b, ref a, ref count);
            }

            if (count == 0) return pixels[0];
            return new Color32((byte)(r / count), (byte)(g / count), (byte)(b / count), (byte)(a / count));
        }

        private static void CountBorderColor(Dictionary<int, int> colorCounts, Color32 color)
        {
            int key = GetQuantizedColorKey(color);
            colorCounts.TryGetValue(key, out int c);
            colorCounts[key] = c + 1;
        }

        private static void AccumulateColorForKey(Color32 color, int key, ref int r, ref int g, ref int b, ref int a, ref int count)
        {
            if (GetQuantizedColorKey(color) != key) return;
            r += color.r; g += color.g; b += color.b; a += color.a; count++;
        }

        private static int GetQuantizedColorKey(Color32 color)
        {
            int r = color.r >> 4, g = color.g >> 4, b = color.b >> 4, a = color.a >> 4;
            return r | (g << 4) | (b << 8) | (a << 12);
        }

        private static void TryQueueBackgroundPixel(int index, Color32[] pixels, bool[] queued, Queue<int> queue, Color32 backgroundColor, float tolerance)
        {
            if (queued[index]) return;
            if (!IsSimilarColor(pixels[index], backgroundColor, tolerance)) return;
            queued[index] = true;
            queue.Enqueue(index);
        }

        private static bool IsSimilarColor(Color32 color, Color32 referenceColor, float tolerance)
        {
            if (color.a == 0) return true;
            float dr = (color.r - referenceColor.r) / 255f;
            float dg = (color.g - referenceColor.g) / 255f;
            float db = (color.b - referenceColor.b) / 255f;
            return dr * dr + dg * dg + db * db <= tolerance * tolerance;
        }
    }
}
#endif
