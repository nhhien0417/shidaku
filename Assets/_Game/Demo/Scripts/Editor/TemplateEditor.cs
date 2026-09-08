#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Shidaku.Editor
{
    [CustomEditor(typeof(Template))]
    public class TemplateEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var template = (Template)target;
            DrawDefaultInspector();

            EditorGUILayout.Space(10);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Preview", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            GUI.backgroundColor = new Color(0.3f, 0.7f, 1f);
            if (GUILayout.Button("Edit", GUILayout.Width(60)))
            {
                TemplateDesignerWindow.OpenWithTemplate(template);
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();

            if (template.Width > 0 && template.Height > 0 && template.Colors != null && template.Colors.Length == template.Width * template.Height)
                DrawPreview(template);
            else
                EditorGUILayout.HelpBox("Empty or invalid template data.", MessageType.Info);
        }

        private void DrawPreview(Template template)
        {
            int cols = template.Width, rows = template.Height;
            float largestSide = Mathf.Max(cols, rows);
            float viewWidth = EditorGUIUtility.currentViewWidth - 40f;
            float cellSize = Mathf.Min(viewWidth / largestSide, 24f);

            Rect rect = GUILayoutUtility.GetRect(cols * cellSize, rows * cellSize);
            rect.width = cols * cellSize;
            rect.height = rows * cellSize;
            rect.x += (viewWidth + 40f - rect.width) / 2f - 20f;

            for (int y = 0; y < rows; y++)
            {
                for (int x = 0; x < cols; x++)
                {
                    // Row 0 = bottom, matching the world-space convention used by real source art.
                    Rect cellRect = new(rect.x + x * cellSize, rect.y + (rows - 1 - y) * cellSize, cellSize, cellSize);
                    var color = template.GetColor(x, y);
                    bool playable = color.a != 0;
                    Color drawColor = playable
                        ? (Color)color
                        : ((x + y) % 2 == 0 ? new Color(0.45f, 0.45f, 0.45f) : new Color(0.55f, 0.55f, 0.55f));
                    EditorGUI.DrawRect(cellRect, drawColor);
                }
            }
        }
    }
}
#endif
