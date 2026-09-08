#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Shidaku.Editor
{
    [CustomEditor(typeof(PuzzleData))]
    public class PuzzleDataEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var puzzle = (PuzzleData)target;
            DrawDefaultInspector();

            EditorGUILayout.Space(10);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Preview", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            GUI.backgroundColor = new Color(0.3f, 0.7f, 1f);
            if (GUILayout.Button("Edit", GUILayout.Width(60)))
            {
                PuzzleDesignerWindow.OpenWithPuzzleData(puzzle);
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();

            if (puzzle.SourceArt == null)
            {
                EditorGUILayout.HelpBox("No Template assigned.", MessageType.Info);
                return;
            }
            if (puzzle.Width <= 0 || puzzle.Height <= 0 || puzzle.SectionOf == null || puzzle.SectionOf.Length != puzzle.Width * puzzle.Height)
            {
                EditorGUILayout.HelpBox("PuzzleData not yet sized to its Template - open Edit to initialize.", MessageType.Info);
                return;
            }

            int totalRegions = 0;
            foreach (var s in puzzle.Sections) totalRegions += s.Regions.Count;
            EditorGUILayout.LabelField($"Sections: {puzzle.Sections.Count}   Regions: {totalRegions}");

            DrawPreview(puzzle);
        }

        private void DrawPreview(PuzzleData puzzle)
        {
            int cols = puzzle.Width, rows = puzzle.Height;
            float largestSide = Mathf.Max(cols, rows);
            float viewWidth = EditorGUIUtility.currentViewWidth - 40f;
            float cellSize = Mathf.Min(viewWidth / largestSide, 24f);

            Rect rect = GUILayoutUtility.GetRect(cols * cellSize, rows * cellSize);
            rect.width = cols * cellSize;
            rect.height = rows * cellSize;
            rect.x += (viewWidth + 40f - rect.width) / 2f - 20f;

            // Build a "covered by a region" lookup so cells assigned-but-not-yet-regioned read
            // visibly differently (section tint) from fully-designed ones (real art color).
            var covered = new bool[cols * rows];
            foreach (var section in puzzle.Sections)
                foreach (var region in section.Regions)
                    for (int y = region.Rect.y; y < region.Rect.y + region.Rect.height; y++)
                        for (int x = region.Rect.x; x < region.Rect.x + region.Rect.width; x++)
                            if (x >= 0 && x < cols && y >= 0 && y < rows) covered[y * cols + x] = true;

            for (int y = 0; y < rows; y++)
            {
                for (int x = 0; x < cols; x++)
                {
                    Rect cellRect = new(rect.x + x * cellSize, rect.y + (rows - 1 - y) * cellSize, cellSize, cellSize);
                    if (!puzzle.IsPlayable(x, y))
                    {
                        Color emptyColor = (x + y) % 2 == 0 ? new Color(0.45f, 0.45f, 0.45f) : new Color(0.55f, 0.55f, 0.55f);
                        EditorGUI.DrawRect(cellRect, emptyColor);
                        continue;
                    }

                    int section = puzzle.GetSectionAt(x, y);
                    if (section < 0)
                    {
                        EditorGUI.DrawRect(cellRect, new Color(0.8f, 0.2f, 0.2f, 0.5f)); // unassigned
                    }
                    else if (!covered[y * cols + x])
                    {
                        EditorGUI.DrawRect(cellRect, PuzzleColors.SectionTint(section)); // assigned, no region yet
                    }
                    else
                    {
                        EditorGUI.DrawRect(cellRect, puzzle.SourceArt.GetColor(x, y));
                    }
                }
            }
        }
    }
}
#endif
