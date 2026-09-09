using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Shidaku.EditorTools
{
    /// <summary>
    /// Exports the puzzles authored in this project to the JSON the web prototype loads
    /// (web/public/puzzles/). This is the primary content path for anything authored in Unity:
    /// Template Designer -> Puzzle Designer -> Verify -> here.
    ///
    /// Two things it deliberately does NOT do:
    ///  - it does not re-derive sections or regions. Whatever Puzzle Designer saved is exactly
    ///    what ships, byte for byte, so the puzzle a designer felt out with the mouse is the
    ///    puzzle the player gets.
    ///  - it does not write anything if a check fails. A broken board found mid-test costs an
    ///    ad budget, not a rebuild, so the three Verify checks run again here.
    ///
    /// The JSON keeps Unity's own convention - y grows upward, pixel index 0 is the bottom-left
    /// row - so nothing has to be flipped on the way out. The web loader flips once on the way
    /// in (see web/src/core/types.ts).
    /// </summary>
    public static class ShidakuWebExporter
    {
        private const string Base64Url = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-_";
        private const int MaxRegionArea = 15;
        private const int MaxSectionWidth = 9;

        [MenuItem("Tools/Shidaku/Export Web JSON")]
        public static void Export()
        {
            var catalog = Resources.Load<PuzzleCatalog>("PuzzleCatalog");
            if (catalog == null || catalog.Puzzles == null || catalog.Puzzles.Count == 0)
            {
                EditorUtility.DisplayDialog(
                    "Shidaku web export",
                    "No PuzzleCatalog found at Resources/PuzzleCatalog, or it is empty.\n\n" +
                    "Save a puzzle from Tools > Shidaku > Puzzle Designer first.",
                    "OK");
                return;
            }

            string outDir = Path.GetFullPath(Path.Combine(Application.dataPath, "../web/public/puzzles"));
            if (!Directory.Exists(outDir))
            {
                EditorUtility.DisplayDialog(
                    "Shidaku web export",
                    $"Expected the web project at:\n{outDir}\n\nIt does not exist.",
                    "OK");
                return;
            }

            var problems = new List<string>();
            var written = new List<string>();
            var ids = new List<string>();

            foreach (var puzzle in catalog.Puzzles)
            {
                if (puzzle == null) continue;
                if (puzzle.SourceArt == null)
                {
                    problems.Add($"{puzzle.name}: no SourceArt assigned");
                    continue;
                }

                string id = SanitiseId(puzzle.name);
                var puzzleProblems = new List<string>();
                string json = BuildJson(puzzle, id, puzzleProblems);
                if (json == null || puzzleProblems.Count > 0)
                {
                    problems.AddRange(puzzleProblems);
                    continue;
                }

                File.WriteAllText(Path.Combine(outDir, id + ".json"), json, new UTF8Encoding(false));
                written.Add(id + ".json");
                ids.Add(id);
            }

            if (problems.Count > 0)
            {
                // Nothing partial: if one puzzle is broken, the whole export is suspect.
                foreach (string problem in problems) Debug.LogError($"[ShidakuWebExporter] {problem}");
                EditorUtility.DisplayDialog(
                    "Shidaku web export FAILED",
                    $"{problems.Count} problem(s). Nothing usable was produced - see the Console.\n\n" +
                    string.Join("\n", problems.ToArray(), 0, Mathf.Min(6, problems.Count)),
                    "OK");
                return;
            }

            /*
             * content_set is "internal", not "safe", and that is not a placeholder.
             *
             * The four templates in this project are cross-stitch charts imported from outside
             * sources and include third-party copyrighted characters. They are fine for
             * checking the pipeline against real Unity-authored puzzles; they must never be
             * what a paid ad points at. web/scripts/check-budget.mjs fails any production build
             * that declares this set. Clean-room artwork lives in web/scripts/art-safe.mjs.
             */
            var catalogJson = new StringBuilder();
            catalogJson.Append("{\"version\":1,\"content_set\":\"internal\",\"pictures\":[");
            for (int i = 0; i < ids.Count; i++)
            {
                if (i > 0) catalogJson.Append(',');
                catalogJson.Append('"').Append(ids[i]).Append('"');
            }
            catalogJson.Append("]}");
            File.WriteAllText(Path.Combine(outDir, "catalog.json"), catalogJson.ToString(), new UTF8Encoding(false));

            Debug.Log($"[ShidakuWebExporter] wrote {written.Count} puzzle(s) + catalog.json to {outDir}");
            EditorUtility.DisplayDialog(
                "Shidaku web export",
                $"Wrote {written.Count} puzzle(s) to:\n{outDir}\n\n" +
                "content_set = \"internal\" (dev/QA only - third-party artwork).",
                "OK");
        }

        private static string SanitiseId(string name)
        {
            var sb = new StringBuilder();
            foreach (char c in name)
            {
                if (char.IsLetterOrDigit(c) || c == '_' || c == '-') sb.Append(char.ToLowerInvariant(c));
            }
            string id = sb.ToString();
            return id.Length == 0 ? "puzzle" : id;
        }

        private static string BuildJson(PuzzleData puzzle, string id, List<string> problems)
        {
            var art = puzzle.SourceArt;
            int w = art.Width;
            int h = art.Height;

            // --- palette: one entry per distinct opaque colour, index 0 reserved for "not art".
            var palette = new List<string> { "#00000000" };
            var lookup = new Dictionary<uint, int>();
            var indices = new int[w * h];

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    Color32 c = art.GetColor(x, y);
                    if (c.a == 0)
                    {
                        indices[y * w + x] = 0;
                        continue;
                    }
                    uint key = (uint)((c.r << 16) | (c.g << 8) | c.b);
                    if (!lookup.TryGetValue(key, out int index))
                    {
                        index = palette.Count;
                        lookup[key] = index;
                        palette.Add($"#{c.r:X2}{c.g:X2}{c.b:X2}");
                    }
                    indices[y * w + x] = index;
                }
            }

            if (palette.Count > Base64Url.Length)
            {
                problems.Add(
                    $"{puzzle.name}: {palette.Count - 1} distinct colours, max {Base64Url.Length - 1}. " +
                    "Merge near-identical shades in Template Designer - every shade is a separate " +
                    "colour for the one-region-one-colour rule, so extra shades also shatter the puzzle.");
                return null;
            }

            // --- pixels, Unity order (index 0 = bottom-left row).
            var pixels = new StringBuilder(w * h);
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++) pixels.Append(Base64Url[indices[y * w + x]]);
            }

            // --- the three Verify checks, run again here so a hand-edited asset cannot ship.
            var owner = new int[w * h];
            for (int i = 0; i < owner.Length; i++) owner[i] = -1;

            for (int s = 0; s < puzzle.Sections.Count; s++)
            {
                var section = puzzle.Sections[s];
                if (section.Regions.Count == 0)
                {
                    problems.Add($"{puzzle.name}/s{s}: section has no regions");
                    continue;
                }

                int minX = int.MaxValue, maxX = int.MinValue;
                foreach (var region in section.Regions)
                {
                    var r = region.Rect;
                    int area = r.width * r.height;
                    if (area > MaxRegionArea)
                    {
                        problems.Add($"{puzzle.name}/s{s}: region at {r.x},{r.y} is {area} cells (max {MaxRegionArea})");
                    }

                    if (!r.Contains(region.ClueCell))
                    {
                        problems.Add($"{puzzle.name}/s{s}: clue {region.ClueCell} is outside its rect {r}");
                    }

                    int firstColour = -1;
                    for (int y = r.y; y < r.y + r.height; y++)
                    {
                        for (int x = r.x; x < r.x + r.width; x++)
                        {
                            if (x < 0 || y < 0 || x >= w || y >= h)
                            {
                                problems.Add($"{puzzle.name}/s{s}: region {r} leaves the canvas");
                                continue;
                            }
                            int flat = y * w + x;

                            // 1. exclusive ownership
                            if (owner[flat] != -1)
                            {
                                problems.Add($"{puzzle.name}: cell {x},{y} claimed by sections {owner[flat]} and {s}");
                            }
                            owner[flat] = s;

                            // 2. monochrome - otherwise a correct answer reveals a patchwork
                            if (indices[flat] == 0)
                            {
                                problems.Add($"{puzzle.name}/s{s}: region {r} covers a transparent cell {x},{y}");
                            }
                            else if (firstColour == -1) firstColour = indices[flat];
                            else if (indices[flat] != firstColour)
                            {
                                problems.Add($"{puzzle.name}/s{s}: region {r} spans two colours");
                            }
                        }
                    }

                    minX = Mathf.Min(minX, r.x);
                    maxX = Mathf.Max(maxX, r.x + r.width - 1);
                }

                int sectionWidth = maxX - minX + 1;
                if (sectionWidth > MaxSectionWidth)
                {
                    problems.Add(
                        $"{puzzle.name}/s{s}: section is {sectionWidth} cells wide (max {MaxSectionWidth}). " +
                        "Wider than that and the camera has to shrink cells below a thumb's accuracy.");
                }
            }

            // 3. coverage - a missed cell means that section can never be completed and the
            //    player is stuck there permanently.
            for (int i = 0; i < indices.Length; i++)
            {
                if (indices[i] != 0 && owner[i] == -1)
                {
                    problems.Add($"{puzzle.name}: artwork cell {i % w},{i / w} belongs to no region");
                }
            }

            if (problems.Count > 0) return null;

            // --- serialise
            var sb = new StringBuilder();
            sb.Append('{');
            sb.Append("\"id\":\"").Append(id).Append("\",");
            sb.Append("\"name\":\"").Append(Escape(puzzle.name)).Append("\",");
            sb.Append("\"w\":").Append(w).Append(',');
            sb.Append("\"h\":").Append(h).Append(',');

            sb.Append("\"palette\":[");
            for (int i = 0; i < palette.Count; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append('"').Append(palette[i]).Append('"');
            }
            sb.Append("],");

            sb.Append("\"pixels\":\"").Append(pixels.ToString()).Append("\",");

            sb.Append("\"sections\":[");
            for (int s = 0; s < puzzle.Sections.Count; s++)
            {
                if (s > 0) sb.Append(',');
                sb.Append("{\"regions\":[");
                var regions = puzzle.Sections[s].Regions;
                for (int i = 0; i < regions.Count; i++)
                {
                    if (i > 0) sb.Append(',');
                    var r = regions[i].Rect;
                    var c = regions[i].ClueCell;
                    sb.Append("{\"r\":[")
                      .Append(r.x).Append(',').Append(r.y).Append(',')
                      .Append(r.width).Append(',').Append(r.height)
                      .Append("],\"c\":[")
                      .Append(c.x).Append(',').Append(c.y)
                      .Append("]}");
                }
                sb.Append("]}");
            }
            sb.Append("],");

            sb.Append("\"source\":\"Unity ShidakuWebExporter\",");
            sb.Append("\"exported_at\":\"")
              .Append(DateTime.UtcNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))
              .Append('"');
            sb.Append('}');
            return sb.ToString();
        }

        private static string Escape(string value)
        {
            return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }
    }
}
