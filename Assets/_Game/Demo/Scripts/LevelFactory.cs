using System.Collections.Generic;
using UnityEngine;

namespace Shidaku
{
    /// <summary>
    /// Loads a PuzzleCatalog (see Editor/PuzzleDesignerWindow.cs, which rebuilds it on every save)
    /// and cycles through it: level 1 = catalog[0], level 2 = catalog[1], ... wrapping back to
    /// catalog[0] once every entry has been used. Each PuzzleData's Regions are already in WORLD
    /// coordinates and read their color live from SourceArt (never stored/baked, so they can never
    /// go stale relative to it) - converting to the runtime LevelData is just: compute each
    /// Section's tight bounding box from its Regions, then paint local Colors from the Template.
    /// </summary>
    public static class LevelFactory
    {
        private static PuzzleCatalog _catalog;

        private static PuzzleCatalog Catalog
        {
            get
            {
                if (_catalog == null)
                    _catalog = Resources.Load<PuzzleCatalog>("PuzzleCatalog");
                return _catalog;
            }
        }

        public static LevelData BuildLevel(int levelIndex)
        {
            var catalog = Catalog;
            if (catalog == null || catalog.Puzzles == null || catalog.Puzzles.Count == 0)
            {
                Debug.LogError("LevelFactory: no PuzzleCatalog found at Resources/PuzzleCatalog. " +
                                "Save a puzzle from Tools > Shidaku > Puzzle Designer first.");
                return new LevelData { LevelIndex = levelIndex, DisplayName = "(missing)", Sections = new List<SectionData>() };
            }

            var puzzle = catalog.Puzzles[(levelIndex - 1) % catalog.Puzzles.Count];
            return Convert(puzzle, levelIndex);
        }

        private static LevelData Convert(PuzzleData puzzle, int levelIndex)
        {
            var data = new LevelData
            {
                LevelIndex = levelIndex,
                DisplayName = puzzle.name,
                Sections = new List<SectionData>(puzzle.Sections.Count),
            };

            foreach (var section in puzzle.Sections)
            {
                int minX = puzzle.Width, minY = puzzle.Height, maxX = -1, maxY = -1;
                foreach (var region in section.Regions)
                {
                    minX = Mathf.Min(minX, region.Rect.x);
                    minY = Mathf.Min(minY, region.Rect.y);
                    maxX = Mathf.Max(maxX, region.Rect.x + region.Rect.width - 1);
                    maxY = Mathf.Max(maxY, region.Rect.y + region.Rect.height - 1);
                }
                if (maxX < minX) continue; // section with no regions - shouldn't happen for a saved puzzle

                int w = maxX - minX + 1, h = maxY - minY + 1;

                // Sections can be organic (non-rectangular) shapes with OVERLAPPING bounding boxes,
                // so a cell inside this section's box may actually belong to a different section.
                // Painting exactly the cells each Region covers - and nothing else - both fills in
                // this section's real colors AND naturally leaves any cell belonging to a
                // different section as a hole (alpha=0, the array's default).
                var colors = new Color32[w * h];
                var regions = new List<RegionData>(section.Regions.Count);
                foreach (var region in section.Regions)
                {
                    for (int y = region.Rect.y; y < region.Rect.y + region.Rect.height; y++)
                        for (int x = region.Rect.x; x < region.Rect.x + region.Rect.width; x++)
                            colors[(y - minY) * w + (x - minX)] = puzzle.SourceArt.GetColor(x, y);

                    var localRect = new RectInt(region.Rect.x - minX, region.Rect.y - minY, region.Rect.width, region.Rect.height);
                    var localClue = new Vector2Int(region.ClueCell.x - minX, region.ClueCell.y - minY);
                    regions.Add(new RegionData(localRect, localClue));
                }

                data.Sections.Add(new SectionData
                {
                    Width = w,
                    Height = h,
                    WorldOrigin = new Vector2Int(minX, minY),
                    Colors = colors,
                    Regions = regions,
                });
            }

            return data;
        }
    }
}
