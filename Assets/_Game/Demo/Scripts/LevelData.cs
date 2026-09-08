using System.Collections.Generic;
using UnityEngine;

namespace Shidaku
{
    /// <summary>One rectangle region within a section: the single accepted answer for its clue.</summary>
    public class RegionData
    {
        public RectInt Rect;
        public readonly Vector2Int ClueCell;
        public bool Solved;

        public int Area => Rect.width * Rect.height;

        public RegionData(RectInt rect, Vector2Int clueCell)
        {
            Rect = rect;
            ClueCell = clueCell;
            Solved = false;
        }
    }

    /// <summary>
    /// One Shikaku board (= one "section" of the picture, per the Pixel Fill Shikaku spec).
    /// Colors are REAL per-cell colors sourced from the picture (not a flat placeholder color) —
    /// see PuzzleData / LevelFactory for where they come from.
    /// </summary>
    public class SectionData
    {
        public int Width;
        public int Height;
        public Vector2Int WorldOrigin;      // this section's bounding-box position within the full picture.
                                             // Sections can be organic (non-rectangular) shapes, so this
                                             // bounding box MAY overlap a neighboring section's - that's fine
                                             // as long as ownership of actual playable cells stays exclusive
                                             // (alpha=0/not-playable marks any cell in this box owned by a
                                             // different section). GameManager only ever reads a section's own
                                             // Colors/IsPlayable, never a shared world-space array.
        public Color32[] Colors;            // flat Width*Height, local to this section; alpha=0 => not playable
        public List<RegionData> Regions;

        /// <summary>Per-cell solved flag (flat Width*Height), set via MarkSolved as regions complete —
        /// lets input reject a drag the instant it would cross into an already-solved cell, same as
        /// it already does for holes. Lazily allocated so LevelFactory doesn't have to know about it.</summary>
        private bool[] solvedCells;
        private bool[] SolvedCells => solvedCells ??= new bool[Width * Height];

        public bool IsPlayable(int localX, int localY) => Colors[localY * Width + localX].a != 0;
        public Color GetCellColor(int localX, int localY) => Colors[localY * Width + localX];

        public bool IsCellSolved(int localX, int localY) => SolvedCells[localY * Width + localX];
        public void MarkCellSolved(int localX, int localY) => SolvedCells[localY * Width + localX] = true;

        public bool IsFullySolved()
        {
            foreach (var r in Regions)
                if (!r.Solved) return false;
            return true;
        }

        public List<RegionData> UnsolvedRegions()
        {
            var list = new List<RegionData>();
            foreach (var r in Regions)
                if (!r.Solved) list.Add(r);
            return list;
        }
    }

    /// <summary>One Level = an ordered list of Sections that together tile one real picture, contiguously.</summary>
    public class LevelData
    {
        public int LevelIndex;
        public string DisplayName;
        public List<SectionData> Sections;
    }
}
