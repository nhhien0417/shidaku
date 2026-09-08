using System.Collections.Generic;
using UnityEngine;

namespace Shidaku
{
    /// <summary>Ordered list of playable puzzles the demo cycles through (1, 2, 3, ... then loops
    /// back to 1). Lives in Resources/ so LevelFactory can load it at runtime with no scene wiring.
    /// Rebuilt automatically by PuzzleDesignerWindow whenever a Puzzle is saved.</summary>
    [CreateAssetMenu(menuName = "Shidaku/Puzzle Catalog")]
    public class PuzzleCatalog : ScriptableObject
    {
        public List<PuzzleData> Puzzles = new();
    }
}
