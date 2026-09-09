using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Shidaku
{
    // The puzzle board: level/section data, cell spawning, drag input, solve/reject
    // feedback, selection border+badge, and camera pan/zoom framing. Raises events for
    // progress/level-complete instead of touching HUD directly.
    public class Board : MonoBehaviour
    {
        private const float CellSize = 100f; // UI units per board cell
        private const float BoardFramePadding = 1.14f;
        private const float BoardTweenDuration = 0.45f;
        private const float BorderThicknessPx = CellSize * 0.07f;
        private const float BadgeOffsetPx = CellSize * 0.5f;
        private const float SelectionFollowSharpness = 22f;

        [Header("Board (UI)")]
        [SerializeField] private RectTransform boardViewport;
        [SerializeField] private RectTransform boardContent;
        [SerializeField] private RectTransform cellsContainer;
        [SerializeField] private UICell cellPrefab;

        [Header("Selection visuals (children of BoardContent)")]
        [SerializeField] private Image selectionFill; // kept invisible — border-only selection; still used as the position/size reference
        [SerializeField] private Image borderTop;
        [SerializeField] private Image borderBottom;
        [SerializeField] private Image borderLeft;
        [SerializeField] private Image borderRight;
        [SerializeField] private RectTransform selectionBadgeRoot;
        [SerializeField] private TMPro.TextMeshProUGUI selectionBadgeText;

        public event Action OnProgressChanged;
        public event Action OnLevelCompleted;

        public bool InputEnabled { get; set; } = true;

        public float ProgressFraction
        {
            get
            {
                int solved = 0, total = 0;
                foreach (var s in level.Sections)
                    foreach (var r in s.Regions)
                    {
                        total++;
                        if (r.Solved) solved++;
                    }
                return total > 0 ? (float)solved / total : 0f;
            }
        }

        private static readonly Color LockedColor = HexColor(0xF1EDE9);      // future section
        private static readonly Color ActiveEmptyColor = HexColor(0xECE5DF); // current section, unsolved

        // Background on accept goes to a medium-light HSV tint of the real pixel color
        // (the Block itself reveals the full color — see PaintRegion/AnimateAccept).
        private const float MediumSaturationMultiplier = 0.48f;
        private const float MediumValueLerp = 0.22f;

        private LevelData level;
        private int currentSectionIndex;
        private int combo;
        private int mistakes;
        private int hintsUsed;

        private readonly List<UICell[,]> cellViewsPerSection = new();
        private readonly List<GameObject> sectionRoots = new();
        private readonly HashSet<Vector2Int> hoveredCellsLocal = new();

        private Vector2 selectionTargetPos;
        private Vector2 selectionTargetSize;

        private bool dragging;
        private int dragSectionIndex;
        private Vector2Int dragStartCellLocal;
        private Vector2Int dragCurrentCellLocal;

        private void Awake()
        {
            selectionFill.gameObject.SetActive(false);
            selectionBadgeRoot.localScale = Vector3.zero;
            selectionBadgeRoot.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (!InputEnabled) return;

            if (Input.GetMouseButtonDown(0) && !IsPointerOverUI())
                TryBeginDrag();
            else if (Input.GetMouseButton(0) && dragging)
                UpdateDragPreview();
            else if (Input.GetMouseButtonUp(0) && dragging)
                EndDrag();

            if (dragging)
                AnimateSelectionTowardTarget();
        }

        public bool IsPointerOverUI() => EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

        // ---------------------------------------------------------------- level lifecycle

        public void SpawnLevel(int levelIndex)
        {
            ClearBoard();
            level = LevelFactory.BuildLevel(levelIndex);
            currentSectionIndex = 0;
            combo = 0;
            mistakes = 0;
            hintsUsed = 0;

            cellViewsPerSection.Clear();
            sectionRoots.Clear();
            hoveredCellsLocal.Clear();

            for (int s = 0; s < level.Sections.Count; s++)
            {
                var section = level.Sections[s];
                var views = new UICell[section.Width, section.Height];

                var sectionRoot = new GameObject($"Section_{s}", typeof(RectTransform));
                sectionRoot.transform.SetParent(cellsContainer, false);
                var sectionRootRt = (RectTransform)sectionRoot.transform;
                sectionRootRt.anchorMin = sectionRootRt.anchorMax = Vector2.zero;
                sectionRootRt.pivot = Vector2.zero;
                sectionRootRt.anchoredPosition = Vector2.zero;
                sectionRoots.Add(sectionRoot);

                // Top-to-bottom, left-to-right — Y increases upward, so counting down from the
                // top row gives lower cells a higher sibling index than the cells above them.
                for (int y = section.Height - 1; y >= 0; y--)
                {
                    for (int x = 0; x < section.Width; x++)
                    {
                        if (!section.IsPlayable(x, y)) continue;

                        var cell = Instantiate(cellPrefab, sectionRoot.transform);
                        cell.name = $"cell_{s}_{x}_{y}";
                        cell.PlaceAt(
                            new Vector2((section.WorldOrigin.x + x + 0.5f) * CellSize, (section.WorldOrigin.y + y + 0.5f) * CellSize),
                            new Vector2(CellSize * 0.9f, CellSize * 0.9f));
                        cell.SetColorInstant(LockedColor, showBorder: false); // overwritten by RefreshSectionActivationColors below
                        cell.ClearClue();
                        views[x, y] = cell;
                    }
                }

                foreach (var region in section.Regions)
                    views[region.ClueCell.x, region.ClueCell.y].SetClue(region.Area);

                cellViewsPerSection.Add(views);
            }

            RefreshSectionActivationColors();
        }

        private void ClearBoard()
        {
            if (cellsContainer == null) return;
            for (int i = cellsContainer.childCount - 1; i >= 0; i--)
                Destroy(cellsContainer.GetChild(i).gameObject);
        }

        // Recolors every not-yet-solved region per its section's activation state: current
        // section gets ActiveEmptyColor + visible border + clue; locked sections get
        // LockedColor, no border, no clue (a number on a non-playable cell reads as playable).
        private void RefreshSectionActivationColors()
        {
            for (int s = 0; s < level.Sections.Count; s++)
            {
                if (s < currentSectionIndex) continue; // already solved — real colors, don't touch

                bool isCurrent = s == currentSectionIndex;
                Color stateColor = isCurrent ? ActiveEmptyColor : LockedColor;
                var section = level.Sections[s];
                var views = cellViewsPerSection[s];

                foreach (var region in section.Regions)
                {
                    if (region.Solved) continue;
                    for (int x = region.Rect.x; x < region.Rect.x + region.Rect.width; x++)
                        for (int y = region.Rect.y; y < region.Rect.y + region.Rect.height; y++)
                            views[x, y].SetColorInstant(stateColor, showBorder: isCurrent);

                    if (isCurrent) views[region.ClueCell.x, region.ClueCell.y].SetClue(region.Area);
                    else views[region.ClueCell.x, region.ClueCell.y].ClearClue();
                }
            }
        }

        // ---------------------------------------------------------------- input / drag

        private Vector2 BoardLocalPoint()
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(boardContent, Input.mousePosition, Camera.main, out var local);
            return local;
        }

        private void TryBeginDrag()
        {
            var local = BoardLocalPoint();
            var section = level.Sections[currentSectionIndex];
            int localX = Mathf.FloorToInt(local.x / CellSize) - section.WorldOrigin.x;
            int localY = Mathf.FloorToInt(local.y / CellSize) - section.WorldOrigin.y;
            if (localX < 0 || localY < 0 || localX >= section.Width || localY >= section.Height) return;
            if (!section.IsPlayable(localX, localY) || section.IsCellSolved(localX, localY)) return;

            dragging = true;
            dragSectionIndex = currentSectionIndex;
            dragStartCellLocal = new Vector2Int(localX, localY);
            dragCurrentCellLocal = dragStartCellLocal;

            var rect = MakeRect(dragStartCellLocal, dragCurrentCellLocal);
            var center = CellRectCenterPx(section, rect);
            var size = CellRectSizePx(rect);
            selectionTargetPos = center;
            selectionTargetSize = size;

            SetSelectionVisible(true);
            PositionSelectionGroup(center, size); // snap on the first cell, no lerp needed yet

            ShowSelectionBadge();
            UpdateSelectionBadge(rect, center, size);
            UpdateHoverCells(dragSectionIndex, rect);
        }

        private void UpdateDragPreview()
        {
            var local = BoardLocalPoint();
            var section = level.Sections[dragSectionIndex];
            int cx = Mathf.Clamp(Mathf.FloorToInt(local.x / CellSize) - section.WorldOrigin.x, 0, section.Width - 1);
            int cy = Mathf.Clamp(Mathf.FloorToInt(local.y / CellSize) - section.WorldOrigin.y, 0, section.Height - 1);
            var candidate = new Vector2Int(cx, cy);

            // Never let the selection cross into a hole or an already-solved cell — stop at
            // that edge instead of bleeding across it.
            if (IsRectSelectable(section, MakeRect(dragStartCellLocal, candidate)))
                dragCurrentCellLocal = candidate;

            var rect = MakeRect(dragStartCellLocal, dragCurrentCellLocal);
            var center = CellRectCenterPx(section, rect);
            var size = CellRectSizePx(rect);
            selectionTargetPos = center;
            selectionTargetSize = size;
            UpdateSelectionBadge(rect, center, size);
            UpdateHoverCells(dragSectionIndex, rect);
        }

        private void EndDrag()
        {
            dragging = false;
            SetSelectionVisible(false);
            HideSelectionBadge();
            var rect = MakeRect(dragStartCellLocal, dragCurrentCellLocal);
            hoveredCellsLocal.Clear(); // the accept/reject animation takes over from here
            TryPlaceRect(dragSectionIndex, rect);
        }

        private void UpdateHoverCells(int sectionIndex, RectInt rect)
        {
            var views = cellViewsPerSection[sectionIndex];

            var newSet = new HashSet<Vector2Int>();
            for (int x = rect.x; x < rect.x + rect.width; x++)
                for (int y = rect.y; y < rect.y + rect.height; y++)
                    newSet.Add(new Vector2Int(x, y));

            foreach (var cell in hoveredCellsLocal)
                if (!newSet.Contains(cell))
                    views[cell.x, cell.y]?.AnimateHoverExit();

            foreach (var cell in newSet)
                if (!hoveredCellsLocal.Contains(cell))
                    views[cell.x, cell.y]?.AnimateHoverEnter();

            hoveredCellsLocal.Clear();
            foreach (var cell in newSet)
                hoveredCellsLocal.Add(cell);
        }

        private static RectInt MakeRect(Vector2Int a, Vector2Int b)
        {
            int minX = Mathf.Min(a.x, b.x);
            int minY = Mathf.Min(a.y, b.y);
            int maxX = Mathf.Max(a.x, b.x);
            int maxY = Mathf.Max(a.y, b.y);
            return new RectInt(minX, minY, maxX - minX + 1, maxY - minY + 1);
        }

        private static bool IsRectSelectable(SectionData section, RectInt rect)
        {
            for (int x = rect.x; x < rect.x + rect.width; x++)
                for (int y = rect.y; y < rect.y + rect.height; y++)
                    if (!section.IsPlayable(x, y) || section.IsCellSolved(x, y)) return false;
            return true;
        }

        private static Vector2 CellRectCenterPx(SectionData section, RectInt rect) => new(
            (section.WorldOrigin.x + rect.x + rect.width / 2f) * CellSize,
            (section.WorldOrigin.y + rect.y + rect.height / 2f) * CellSize);

        private static Vector2 CellRectSizePx(RectInt rect) => new(rect.width * CellSize * 0.94f, rect.height * CellSize * 0.94f);

        // ---------------------------------------------------------------- selection follow / border / badge

        private void AnimateSelectionTowardTarget()
        {
            float k = 1f - Mathf.Exp(-SelectionFollowSharpness * Time.deltaTime);
            var pos = Vector2.Lerp(selectionFill.rectTransform.anchoredPosition, selectionTargetPos, k);
            var size = Vector2.Lerp(selectionFill.rectTransform.sizeDelta, selectionTargetSize, k);
            PositionSelectionGroup(pos, size);
        }

        private void PositionSelectionGroup(Vector2 center, Vector2 size)
        {
            selectionFill.rectTransform.anchoredPosition = center;
            selectionFill.rectTransform.sizeDelta = size;
            PositionBorder(center, size);
        }

        private void PositionBorder(Vector2 center, Vector2 size)
        {
            float hw = size.x / 2f, hh = size.y / 2f;
            borderTop.rectTransform.anchoredPosition = new Vector2(center.x, center.y + hh);
            borderTop.rectTransform.sizeDelta = new Vector2(size.x + BorderThicknessPx, BorderThicknessPx);
            borderBottom.rectTransform.anchoredPosition = new Vector2(center.x, center.y - hh);
            borderBottom.rectTransform.sizeDelta = new Vector2(size.x + BorderThicknessPx, BorderThicknessPx);
            borderLeft.rectTransform.anchoredPosition = new Vector2(center.x - hw, center.y);
            borderLeft.rectTransform.sizeDelta = new Vector2(BorderThicknessPx, size.y + BorderThicknessPx);
            borderRight.rectTransform.anchoredPosition = new Vector2(center.x + hw, center.y);
            borderRight.rectTransform.sizeDelta = new Vector2(BorderThicknessPx, size.y + BorderThicknessPx);
        }

        private void SetSelectionVisible(bool visible)
        {
            borderTop.gameObject.SetActive(visible);
            borderBottom.gameObject.SetActive(visible);
            borderLeft.gameObject.SetActive(visible);
            borderRight.gameObject.SetActive(visible);
        }

        private void ShowSelectionBadge()
        {
            selectionBadgeRoot.gameObject.SetActive(true);
            StopCoroutine(nameof(PunchScaleInRoutine));
            StartCoroutine(PunchScaleInRoutine(selectionBadgeRoot, 1f, 0.18f));
        }

        private void HideSelectionBadge() => selectionBadgeRoot.gameObject.SetActive(false);

        private void UpdateSelectionBadge(RectInt rect, Vector2 center, Vector2 size)
        {
            selectionBadgeText.text = (rect.width * rect.height).ToString();
            selectionBadgeRoot.anchoredPosition = new Vector2(center.x, center.y + size.y / 2f + BadgeOffsetPx);
        }

        private IEnumerator PunchScaleInRoutine(Transform t, float targetScale, float duration)
        {
            float time = 0f;
            while (time < duration)
            {
                time += Time.deltaTime;
                float k = time / duration;
                float eased = 1f - Mathf.Pow(1f - k, 3f);
                float overshoot = 1f + 0.18f * Mathf.Sin(k * Mathf.PI);
                t.localScale = Vector3.one * (targetScale * eased * overshoot);
                yield return null;
            }
            t.localScale = Vector3.one * targetScale;
        }

        // ---------------------------------------------------------------- validate / solve

        private void TryPlaceRect(int sectionIndex, RectInt localRect)
        {
            var section = level.Sections[sectionIndex];
            RegionData match = null;
            foreach (var r in section.Regions)
            {
                if (r.Solved) continue;
                if (r.Rect.x == localRect.x && r.Rect.y == localRect.y &&
                    r.Rect.width == localRect.width && r.Rect.height == localRect.height)
                {
                    match = r;
                    break;
                }
            }

            if (match != null)
            {
                match.Solved = true;
                combo++;
                PaintRegion(sectionIndex, match, isHint: false);
                AfterRegionSolved(sectionIndex, section);
            }
            else
            {
                combo = 0;
                mistakes++;
                StartCoroutine(RejectFeedback(localRect, sectionIndex));
            }
        }

        public void RevealHint()
        {
            if (!InputEnabled) return;

            var section = level.Sections[currentSectionIndex];
            var unsolved = section.UnsolvedRegions();
            if (unsolved.Count == 0) return;

            RegionData best = unsolved[0];
            foreach (var r in unsolved)
                if (r.Area > best.Area) best = r;

            best.Solved = true;
            hintsUsed++;
            PaintRegion(currentSectionIndex, best, isHint: true);
            AfterRegionSolved(currentSectionIndex, section);
        }

        private void AfterRegionSolved(int sectionIndex, SectionData section)
        {
            OnProgressChanged?.Invoke();
            if (section.IsFullySolved())
                StartCoroutine(SectionCleared(sectionIndex));
        }

        // Top-to-bottom, left-to-right — the one fixed order for both accept and reject waves.
        private static List<Vector2Int> RowMajorCells(RectInt rect)
        {
            var list = new List<Vector2Int>(rect.width * rect.height);
            for (int y = rect.y + rect.height - 1; y >= rect.y; y--)
                for (int x = rect.x; x < rect.x + rect.width; x++)
                    list.Add(new Vector2Int(x, y));
            return list;
        }

        private void PaintRegion(int sectionIndex, RegionData region, bool isHint)
        {
            var section = level.Sections[sectionIndex];
            var views = cellViewsPerSection[sectionIndex];
            var cells = RowMajorCells(region.Rect);

            // Background keeps the medium tint (set here too, so a hint — which never went
            // through drag/hover — still ends up this way); the Block reveals the real color.
            foreach (var cell in cells)
                views[cell.x, cell.y].SetBackgroundColor(MediumTint(section.GetCellColor(cell.x, cell.y)));

            // Chained, not staggered: cell i's delay is i * one full pop-in duration, so each
            // cell only starts rising once the previous one has completely finished.
            float acceptStep = cellPrefab.AcceptPopDuration * 0.5f;
            for (int index = 0; index < cells.Count; index++)
            {
                var cell = cells[index];
                Color target = section.GetCellColor(cell.x, cell.y);
                if (isHint) views[cell.x, cell.y].SetBlockColor(new Color(1f, 0.95f, 0.55f)); // flash cue before revealing
                views[cell.x, cell.y].AnimateAccept(target, index * acceptStep);
                section.MarkCellSolved(cell.x, cell.y);
            }

            views[region.ClueCell.x, region.ClueCell.y].ClearClue();
        }

        private IEnumerator RejectFeedback(RectInt localRect, int sectionIndex)
        {
            var section = level.Sections[sectionIndex];
            var views = cellViewsPerSection[sectionIndex];
            var basePos = CellRectCenterPx(section, localRect);
            var size = CellRectSizePx(localRect);
            var cells = RowMajorCells(localRect);

            float rejectStep = cellPrefab.RejectPopDuration * 0.25f;
            for (int i = 0; i < cells.Count; i++)
                views[cells[i].x, cells[i].y]?.AnimateReject(i * rejectStep);

            SetSelectionVisible(true);
            PositionSelectionGroup(basePos, size);

            const float duration = 0.22f;
            const float amplitude = CellSize * 0.12f;
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float offset = Mathf.Sin(t / duration * Mathf.PI * 6f) * amplitude * (1f - t / duration);
                PositionSelectionGroup(basePos + new Vector2(offset, 0f), size);
                yield return null;
            }
            SetSelectionVisible(false);
        }

        private IEnumerator SectionCleared(int sectionIndex)
        {
            yield return new WaitForSeconds(0.2f);
            if (currentSectionIndex < level.Sections.Count - 1)
            {
                currentSectionIndex++;
                RefreshSectionActivationColors();
                yield return StartCoroutine(FrameBoardRoutine(false, instant: false));
            }
            else
            {
                OnLevelCompleted?.Invoke();
            }
        }

        // ---------------------------------------------------------------- camera pan/zoom

        public void FrameToCurrentSection(bool instant) => StartCoroutine(FrameBoardRoutine(false, instant));
        public void FrameToOverview(bool instant) => StartCoroutine(FrameBoardRoutine(true, instant));

        private IEnumerator FrameBoardRoutine(bool overview, bool instant)
        {
            var boundsCells = overview ? ComputeLevelBounds() : ComputeSectionBounds(currentSectionIndex);

            float neededW = boundsCells.size.x * CellSize;
            float neededH = boundsCells.size.y * CellSize;
            float targetScale = Mathf.Min(boardViewport.rect.width / neededW, boardViewport.rect.height / neededH) / BoardFramePadding;
            targetScale = Mathf.Max(targetScale, 0.01f);

            var centerPx = new Vector2(boundsCells.center.x * CellSize, boundsCells.center.y * CellSize);
            var targetAnchoredPos = -centerPx * targetScale;

            if (instant)
            {
                boardContent.localScale = Vector3.one * targetScale;
                boardContent.anchoredPosition = targetAnchoredPos;
                yield break;
            }

            float startScale = boardContent.localScale.x;
            Vector2 startPos = boardContent.anchoredPosition;
            float t = 0f;
            while (t < BoardTweenDuration)
            {
                t += Time.deltaTime;
                float k = Mathf.SmoothStep(0f, 1f, t / BoardTweenDuration);
                boardContent.localScale = Vector3.one * Mathf.Lerp(startScale, targetScale, k);
                boardContent.anchoredPosition = Vector2.Lerp(startPos, targetAnchoredPos, k);
                yield return null;
            }
            boardContent.localScale = Vector3.one * targetScale;
            boardContent.anchoredPosition = targetAnchoredPos;
        }

        // Tight bounding box (in cell units) of only the playable cells — not the full
        // Width x Height rect, which can include hole/empty margins for an irregular silhouette.
        private Bounds ComputeSectionBounds(int idx)
        {
            var s = level.Sections[idx];
            int minX = s.Width, minY = s.Height, maxX = -1, maxY = -1;
            for (int y = 0; y < s.Height; y++)
            {
                for (int x = 0; x < s.Width; x++)
                {
                    if (!s.IsPlayable(x, y)) continue;
                    if (x < minX) minX = x;
                    if (x > maxX) maxX = x;
                    if (y < minY) minY = y;
                    if (y > maxY) maxY = y;
                }
            }
            if (maxX < minX) { minX = 0; maxX = s.Width - 1; minY = 0; maxY = s.Height - 1; } // guard: no playable cells (shouldn't happen)

            float w = maxX - minX + 1;
            float h = maxY - minY + 1;
            var center = new Vector3(s.WorldOrigin.x + minX + w / 2f, s.WorldOrigin.y + minY + h / 2f, 0f);
            return new Bounds(center, new Vector3(w, h, 1f));
        }

        private Bounds ComputeLevelBounds()
        {
            var b = ComputeSectionBounds(0);
            for (int i = 1; i < level.Sections.Count; i++)
                b.Encapsulate(ComputeSectionBounds(i));
            return b;
        }

        // ---------------------------------------------------------------- color

        private static Color HexColor(int rgb) => new(
            ((rgb >> 16) & 0xFF) / 255f,
            ((rgb >> 8) & 0xFF) / 255f,
            (rgb & 0xFF) / 255f);

        // HSV lighten (hue fixed, saturation scaled down, value pulled toward white) —
        // fitted to the reference tones #B23085 (original) -> #C37FA4 (medium).
        private static Color MediumTint(Color baseColor)
        {
            Color.RGBToHSV(baseColor, out float h, out float s, out float v);
            s *= MediumSaturationMultiplier;
            v = Mathf.Lerp(v, 1f, MediumValueLerp);
            var result = Color.HSVToRGB(h, s, v);
            result.a = baseColor.a;
            return result;
        }
    }
}
