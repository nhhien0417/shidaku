#if UNITY_EDITOR
using UnityEngine;

namespace Shidaku.Editor
{
    /// <summary>Deterministic, visually-distinct tint per Section index for editor-only previews.</summary>
    internal static class PuzzleColors
    {
        public static Color SectionTint(int sectionIndex)
        {
            if (sectionIndex < 0) return new Color(0.85f, 0.2f, 0.2f, 0.6f); // unassigned warning
            float hue = (sectionIndex * 0.61803398875f) % 1f; // golden-ratio hue spread
            return Color.HSVToRGB(hue, 0.55f, 0.95f);
        }
    }
}
#endif
