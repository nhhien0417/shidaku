using System.Collections.Generic;
using UnityEngine;

namespace Shidaku
{
    /// <summary>
    /// Real Shikaku exact-cover solver: given a playable mask and a fixed set of (clueCell, area)
    /// clues, counts how many DIFFERENT global rectangle tilings satisfy every clue simultaneously
    /// (each rectangle has the clue's area, contains exactly that one clue cell, no overlaps, and
    /// together the rectangles cover every playable cell). Stops counting once it reaches the cap
    /// (we only ever need to distinguish "exactly 1" from "2 or more").
    ///
    /// This is the piece that was MISSING from the throwaway demo generator (see
    /// Docs/Shidaku-Demo-Plan.md §2c-bis) — without it, a generated Section could silently admit
    /// more than one fully-valid global solution, and a player solving the "other" one would be
    /// rejected forever even though their reasoning was perfectly correct.
    /// </summary>
    public class ShikakuSolutionCounter
    {
        private readonly int _width;
        private readonly int _height;
        private readonly bool[] _mask;
        private readonly List<(Vector2Int cell, int area)> _clues;
        private readonly HashSet<Vector2Int> _clueCells;
        private bool[] _occupied;
        private int _count;
        private int _cap;

        public ShikakuSolutionCounter(int width, int height, bool[] mask, List<(Vector2Int cell, int area)> clues, HashSet<Vector2Int> clueCells)
        {
            _width = width;
            _height = height;
            _mask = mask;
            _clues = clues;
            _clueCells = clueCells;
        }

        public int CountUpTo(int cap)
        {
            _cap = cap;
            _count = 0;
            _occupied = new bool[_mask.Length];
            var remaining = new List<int>(_clues.Count);
            for (int i = 0; i < _clues.Count; i++) remaining.Add(i);
            Solve(remaining);
            return _count;
        }

        private void Solve(List<int> remainingIdx)
        {
            if (_count >= _cap) return;
            if (remainingIdx.Count == 0)
            {
                _count++;
                return;
            }

            // MRV heuristic: branch on the clue with the fewest legal placements first.
            int bestIdx = -1;
            List<RectInt> bestCandidates = null;
            foreach (var idx in remainingIdx)
            {
                var cands = CandidateRects(_clues[idx]);
                if (bestCandidates == null || cands.Count < bestCandidates.Count)
                {
                    bestCandidates = cands;
                    bestIdx = idx;
                    if (cands.Count <= 1) break;
                }
            }

            if (bestCandidates == null || bestCandidates.Count == 0) return; // dead end

            var nextRemaining = new List<int>(remainingIdx);
            nextRemaining.Remove(bestIdx);

            foreach (var rect in bestCandidates)
            {
                if (_count >= _cap) return;
                SetOccupied(rect, true);
                Solve(nextRemaining);
                SetOccupied(rect, false);
            }
        }

        private void SetOccupied(RectInt r, bool value)
        {
            for (int y = r.y; y < r.y + r.height; y++)
                for (int x = r.x; x < r.x + r.width; x++)
                    _occupied[y * _width + x] = value;
        }

        private List<RectInt> CandidateRects((Vector2Int cell, int area) clue)
        {
            var result = new List<RectInt>();
            int area = clue.area;

            for (int rw = 1; rw <= area; rw++)
            {
                if (area % rw != 0) continue;
                int rh = area / rw;
                if (rw > _width || rh > _height) continue;

                int xMin = Mathf.Max(0, clue.cell.x - rw + 1);
                int xMax = Mathf.Min(clue.cell.x, _width - rw);
                int yMin = Mathf.Max(0, clue.cell.y - rh + 1);
                int yMax = Mathf.Min(clue.cell.y, _height - rh);

                for (int oy = yMin; oy <= yMax; oy++)
                {
                    for (int ox = xMin; ox <= xMax; ox++)
                    {
                        if (IsValidPlacement(ox, oy, rw, rh, clue.cell))
                            result.Add(new RectInt(ox, oy, rw, rh));
                    }
                }
            }

            return result;
        }

        private bool IsValidPlacement(int ox, int oy, int rw, int rh, Vector2Int clueCell)
        {
            for (int y = oy; y < oy + rh; y++)
            {
                for (int x = ox; x < ox + rw; x++)
                {
                    int idx = y * _width + x;
                    if (!_mask[idx]) return false;
                    if (_occupied[idx]) return false;

                    var cell = new Vector2Int(x, y);
                    if (!cell.Equals(clueCell) && _clueCells.Contains(cell)) return false; // must contain exactly one clue
                }
            }
            return true;
        }
    }
}
