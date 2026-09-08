using System;
using System.Collections.Generic;
using UnityEngine;

namespace Shidaku
{
    /// <summary>
    /// One Region: a single rectangle, in WORLD coordinates (same space as its Template), that is
    /// both the design-time shape AND the final runtime answer - there is no separate "baked"
    /// format. Every Region is guaranteed (validated in the Designer before save) to be monochrome,
    /// so its color is never stored here - always read live from SourceArt at that Rect. This also
    /// means a Region can never go stale relative to its Template (no separate copy to desync).
    /// </summary>
    [Serializable]
    public class PuzzleRegion
    {
        public RectInt Rect;
        public Vector2Int ClueCell;
    }

    [Serializable]
    public class PuzzleSection
    {
        public List<PuzzleRegion> Regions = new();
    }

    /// <summary>
    /// A complete, ready-to-play Pixel Fill Shikaku puzzle - design data and runtime data are the
    /// SAME asset (no separate "GeneratedLevels" bake step): Sections/Regions here, in WORLD
    /// coordinates, are exactly what LevelFactory reads at runtime. Authored/edited via
    /// PuzzleDesignerWindow (Tools/Shidaku/Puzzle Designer), saved once into Assets/_Game/Demo/Puzzles/.
    /// </summary>
    [CreateAssetMenu(menuName = "Shidaku/Puzzle")]
    public class PuzzleData : ScriptableObject
    {
        public Template SourceArt;
        public int Width;
        public int Height;

        /// <summary>Flat Width*Height. -1 = not yet assigned to any Section. Else a 0-based Section
        /// index into <see cref="Sections"/> (solve order == list order). Design-time aid only -
        /// not read at runtime (Sections/Regions alone are enough to play).</summary>
        public int[] SectionOf = new int[0];

        public List<PuzzleSection> Sections = new();

        /// <summary>(Re)sizes SectionOf to match SourceArt, preserving existing assignments where
        /// dimensions already match. Call after assigning/changing SourceArt.</summary>
        public void SyncSizeFromSourceArt()
        {
            if (SourceArt == null) return;
            Width = SourceArt.Width;
            Height = SourceArt.Height;
            if (SectionOf == null || SectionOf.Length != Width * Height)
            {
                SectionOf = new int[Width * Height];
                for (int i = 0; i < SectionOf.Length; i++) SectionOf[i] = -1;
            }
        }

        public bool IsPlayable(int x, int y) => SourceArt != null && SourceArt.IsPlayable(x, y);

        public int GetSectionAt(int x, int y)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height) return -1;
            if (SectionOf == null || SectionOf.Length != Width * Height) return -1;
            return SectionOf[y * Width + x];
        }

        public void SetSectionAt(int x, int y, int sectionIndex)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height) return;
            if (SectionOf == null || SectionOf.Length != Width * Height) return;
            SectionOf[y * Width + x] = sectionIndex;
        }

        public int EnsureSectionCount(int count)
        {
            while (Sections.Count < count) Sections.Add(new PuzzleSection());
            return Sections.Count;
        }
    }
}
