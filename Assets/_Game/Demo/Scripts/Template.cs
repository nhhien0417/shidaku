using UnityEngine;

namespace Shidaku
{
    /// <summary>
    /// The pixel art itself: shape + full color palette, nothing about the puzzle. Same per-pixel
    /// model as the real source PNGs (flat Color32[], alpha==0 = not part of the artwork) so it can
    /// be fed straight into the same generation code (SectionClusterer/PixelShikakuGenerator).
    /// Authored/edited via TemplateDesignerWindow (Tools/Shidaku/Template Designer). Once a
    /// Template looks right, design its Puzzle (Sections/Regions) on top of it via PuzzleDesignerWindow.
    /// </summary>
    [CreateAssetMenu(menuName = "Shidaku/Template")]
    public class Template : ScriptableObject
    {
        public int Width;
        public int Height;
        [HideInInspector] public Color32[] Colors;

        public void Initialize(int w, int h)
        {
            Width = w;
            Height = h;
            Colors = new Color32[w * h];
        }

        public void Clear()
        {
            if (Colors == null || Colors.Length != Width * Height) Colors = new Color32[Width * Height];
            else System.Array.Clear(Colors, 0, Colors.Length);
        }

        public bool IsPlayable(int x, int y) => GetColor(x, y).a != 0;

        public void SetPixel(int x, int y, Color32 color)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height) return;
            Colors[y * Width + x] = color;
        }

        public void Erase(int x, int y) => SetPixel(x, y, default);

        public Color32 GetColor(int x, int y)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height) return default;
            if (Colors == null || Colors.Length != Width * Height) return default;
            return Colors[y * Width + x];
        }
    }
}
