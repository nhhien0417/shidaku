using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Shidaku
{
    /// <summary>
    /// Single-scene Shidaku "Pixel Fill Shikaku" demo controller.
    ///
    /// Everything lives under ONE Canvas as real UI objects (Image, TextMeshProUGUI) — no
    /// SpriteRenderer, no world-space camera zoom. "Zooming" into a Section / the whole picture
    /// is done by scaling + moving a "BoardContent" RectTransform inside a "BoardViewport" frame,
    /// the same way a masked scroll-view's content would be panned/zoomed. This mirrors
    /// smart-queens' own Gameplay.unity structure (one Canvas, a "GameManager" object holding the
    /// controller script, everything else a real Inspector-wired scene object).
    ///
    /// The only thing still built procedurally at runtime is the per-level board cells (their
    /// count/shape changes every level) — same as smart-queens' own "Grid" container.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        private const float HintCooldownDuration = 0f;
        private const float CellSize = 100f; // UI units per board cell
        private const float BoardFramePadding = 1.14f;
        private const float BoardTweenDuration = 0.45f;
        private const float BorderThicknessPx = CellSize * 0.07f;
        private const float BadgeOffsetPx = CellSize * 0.5f;

        [Header("Board (UI)")]
        [SerializeField] private RectTransform boardViewport; // defines the visible framing area
        [SerializeField] private RectTransform boardContent;  // panned/scaled to "zoom"; Cells + SelectionVisuals live here
        [SerializeField] private RectTransform cellsContainer; // cells are spawned/destroyed here each level

        [Header("Selection visuals (UI, children of BoardContent)")]
        [SerializeField] private Image selectionFill;
        [SerializeField] private Image borderTop;
        [SerializeField] private Image borderBottom;
        [SerializeField] private Image borderLeft;
        [SerializeField] private Image borderRight;
        [SerializeField] private RectTransform selectionBadgeRoot;
        [SerializeField] private TextMeshProUGUI selectionBadgeText;

        [Header("HUD (Canvas)")]
        [SerializeField] private TextMeshProUGUI levelLabel;
        [SerializeField] private CanvasGroup progressBarGroup;
        [SerializeField] private TextMeshProUGUI progressBarText;
        [SerializeField] private TextMeshProUGUI tapToContinueText;
        [SerializeField] private Button overviewButton;
        [SerializeField] private Button hintButton;
        [SerializeField] private TextMeshProUGUI hintButtonLabel;
        [SerializeField] private GameObject winPanel;
        [SerializeField] private Button nextLevelButton;

        private Coroutine progressBarFadeRoutine;
        private bool progressBarStickyForOverview;

        private Vector2 selectionTargetPos;
        private Vector2 selectionTargetSize;

        private LevelData level;
        private int currentLevelIndex;
        private int currentSectionIndex;
        private int combo;
        private int mistakes;
        private int hintsUsed;
        private float hintCooldownRemaining;
        private bool overviewMode;

        private readonly List<Image[,]> cellImagesPerSection = new();
        private readonly List<TextMeshProUGUI[,]> clueTextPerSection = new();
        private readonly List<GameObject> sectionRoots = new();

        // Exact tones from the reference video / latest direction — see Docs/Video-Analysis-Findings.md.
        private static readonly Color BackgroundColor = HexColor(0xF8F2ED);
        private static readonly Color LockedColor = HexColor(0xF5EDE9);      // future section — no clue number, not clickable
        private static readonly Color ActiveEmptyColor = HexColor(0xEDE5E0); // current section — playable, still unsolved, shows its clue number
        private static readonly Color NormalPreviewColor = new(0.35f, 0.55f, 0.95f, 0.38f); // fallback only; real drag fill uses the touched cell's own color
        private static readonly Color RejectPreviewColor = new(0.85f, 0.25f, 0.25f, 0.55f);

        private static Color HexColor(int rgb) => new(
            ((rgb >> 16) & 0xFF) / 255f,
            ((rgb >> 8) & 0xFF) / 255f,
            (rgb & 0xFF) / 255f);

        private bool dragging;
        private int dragSectionIndex;
        private Vector2Int dragStartCellLocal;
        private Vector2Int dragCurrentCellLocal;

        private void Awake()
        {
            // Win screen: no title/subtitle/confetti — just the Next Level button, puzzle stays
            // fully visible behind it (WinPanel's own Image is left fully transparent in the scene).
            var nextBtnRect = nextLevelButton.GetComponent<RectTransform>();
            nextBtnRect.anchorMin = new Vector2(0.5f, 0f);
            nextBtnRect.anchorMax = new Vector2(0.5f, 0f);
            nextBtnRect.pivot = new Vector2(0.5f, 0.5f);
            nextBtnRect.anchoredPosition = new Vector2(0f, 110f);
            nextBtnRect.sizeDelta = new Vector2(220f, 110f);

            overviewButton.onClick.AddListener(OnOverviewClicked);
            hintButton.onClick.AddListener(OnHintClicked);
            nextLevelButton.onClick.AddListener(OnNextLevelClicked);

            tapToContinueText.text = "Tap anywhere to continue";
            SetTextAlpha(tapToContinueText, 0f);
            selectionBadgeRoot.localScale = Vector3.zero;
            selectionBadgeRoot.gameObject.SetActive(false);
        }

        private static void SetTextAlpha(TextMeshProUGUI text, float alpha)
        {
            var c = text.color;
            c.a = alpha;
            text.color = c;
        }

        private void Start()
        {
            Camera.main.backgroundColor = BackgroundColor;

            currentLevelIndex = 1;
            selectionFill.color = NormalPreviewColor;
            selectionFill.gameObject.SetActive(false);
            winPanel.SetActive(false);
            SpawnLevel(currentLevelIndex);
            StartCoroutine(FrameBoard(false, instant: true));
        }

        private void Update()
        {
            TickHintCooldown();

            if (winPanel.activeSelf) return;

            if (overviewMode)
            {
                if (Input.GetMouseButtonDown(0) && !IsPointerOverUI())
                    SetOverviewMode(false);
                return;
            }

            if (Input.GetMouseButtonDown(0) && !IsPointerOverUI())
                TryBeginDrag();
            else if (Input.GetMouseButton(0) && dragging)
                UpdateDragPreview();
            else if (Input.GetMouseButtonUp(0) && dragging)
                EndDrag();

            if (dragging)
                AnimateSelectionTowardTarget();
        }

        // ---------------------------------------------------------------- level lifecycle

        private void SpawnLevel(int levelIndex)
        {
            winPanel.SetActive(false); // defensive: Next Level must only ever show once a level is actually completed
            overviewMode = false;
            HideProgressBarImmediate();
            ClearBoard();
            level = LevelFactory.BuildLevel(levelIndex);
            currentSectionIndex = 0;
            combo = 0;
            mistakes = 0;
            hintsUsed = 0;
            levelLabel.text = $"Level {levelIndex}";

            cellImagesPerSection.Clear();
            clueTextPerSection.Clear();
            sectionRoots.Clear();

            for (int s = 0; s < level.Sections.Count; s++)
            {
                var section = level.Sections[s];
                var images = new Image[section.Width, section.Height];
                var clues = new TextMeshProUGUI[section.Width, section.Height];

                var sectionRoot = new GameObject($"Section_{s}", typeof(RectTransform));
                sectionRoot.transform.SetParent(cellsContainer, false);
                var sectionRootRt = (RectTransform)sectionRoot.transform;
                sectionRootRt.anchorMin = sectionRootRt.anchorMax = new Vector2(0f, 0f);
                sectionRootRt.pivot = new Vector2(0f, 0f);
                sectionRootRt.anchoredPosition = Vector2.zero;
                sectionRoots.Add(sectionRoot);

                for (int y = 0; y < section.Height; y++)
                {
                    for (int x = 0; x < section.Width; x++)
                    {
                        if (!section.IsPlayable(x, y)) continue; // not part of the artwork silhouette — no cell here at all

                        var go = new GameObject($"cell_{s}_{x}_{y}", typeof(RectTransform));
                        go.transform.SetParent(sectionRoot.transform, false);
                        var rt = (RectTransform)go.transform;
                        rt.anchorMin = rt.anchorMax = new Vector2(0f, 0f);
                        rt.pivot = new Vector2(0.5f, 0.5f);
                        rt.anchoredPosition = new Vector2((section.WorldOrigin.x + x + 0.5f) * CellSize, (section.WorldOrigin.y + y + 0.5f) * CellSize);
                        rt.sizeDelta = new Vector2(CellSize * 0.9f, CellSize * 0.9f);

                        var img = go.AddComponent<Image>();
                        img.raycastTarget = false; // board input is driven by direct math, not UI raycasts
                        img.color = LockedColor; // overwritten immediately by RefreshSectionActivationColors below
                        images[x, y] = img;
                    }
                }

                foreach (var region in section.Regions)
                {
                    var cellRt = images[region.ClueCell.x, region.ClueCell.y].rectTransform;
                    var clueGO = new GameObject("clue", typeof(RectTransform));
                    clueGO.transform.SetParent(cellRt, false);
                    var clueRt = (RectTransform)clueGO.transform;
                    clueRt.anchorMin = Vector2.zero;
                    clueRt.anchorMax = Vector2.one;
                    clueRt.offsetMin = Vector2.zero;
                    clueRt.offsetMax = Vector2.zero;

                    var tmp = clueGO.AddComponent<TextMeshProUGUI>();
                    tmp.raycastTarget = false;
                    tmp.text = region.Area.ToString();
                    tmp.fontSize = 44f;
                    tmp.enableAutoSizing = true;
                    tmp.fontSizeMin = 10f;
                    tmp.fontSizeMax = 60f;
                    tmp.alignment = TextAlignmentOptions.Center;
                    tmp.color = new Color(0.32f, 0.27f, 0.24f);

                    clues[region.ClueCell.x, region.ClueCell.y] = tmp;
                }

                cellImagesPerSection.Add(images);
                clueTextPerSection.Add(clues);
            }

            RefreshSectionActivationColors();
        }

        private void ClearBoard()
        {
            if (cellsContainer == null) return;
            for (int i = cellsContainer.childCount - 1; i >= 0; i--)
                Destroy(cellsContainer.GetChild(i).gameObject);
        }

        /// <summary>
        /// Recolors every NOT-YET-SOLVED region's cells according to its section's activation
        /// state relative to currentSectionIndex: past sections are left alone (already real
        /// colors from being solved), the current section gets ActiveEmptyColor, future/locked
        /// sections get LockedColor. Call after spawning a level and every time
        /// currentSectionIndex advances.
        /// </summary>
        private void RefreshSectionActivationColors()
        {
            for (int s = 0; s < level.Sections.Count; s++)
            {
                if (s < currentSectionIndex) continue; // already fully solved — don't touch real colors

                bool isCurrent = s == currentSectionIndex;
                Color stateColor = isCurrent ? ActiveEmptyColor : LockedColor;
                var section = level.Sections[s];
                var images = cellImagesPerSection[s];
                var clues = clueTextPerSection[s];

                foreach (var region in section.Regions)
                {
                    if (region.Solved) continue;
                    for (int x = region.Rect.x; x < region.Rect.x + region.Rect.width; x++)
                        for (int y = region.Rect.y; y < region.Rect.y + region.Rect.height; y++)
                            images[x, y].color = stateColor;

                    // Locked (future) sections hide their clue numbers entirely — showing a number
                    // on a non-interactive cell reads as "you can solve this now", which is wrong.
                    var clueTm = clues[region.ClueCell.x, region.ClueCell.y];
                    if (clueTm != null) clueTm.gameObject.SetActive(isCurrent);
                }
            }
        }

        // ---------------------------------------------------------------- input / drag

        private bool IsPointerOverUI() => EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

        /// <summary>Screen point → local pixel position inside BoardContent (already accounts for
        /// BoardContent's current pan/zoom, since this inverts its full transform).</summary>
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
            if (!section.IsPlayable(localX, localY) || section.IsCellSolved(localX, localY)) return; // hole, or already solved

            dragging = true;
            dragSectionIndex = currentSectionIndex;
            dragStartCellLocal = new Vector2Int(localX, localY);
            dragCurrentCellLocal = dragStartCellLocal;

            // Highlight fill = the real pixel-art color of the first touched cell, lightened —
            // "tô sáng" the region you're currently shaping, not a generic blue overlay.
            var startColor = section.GetCellColor(localX, localY);
            var highlightFill = Color.Lerp(startColor, Color.white, 0.5f);
            highlightFill.a = 0.62f;
            selectionFill.color = highlightFill;

            var rect = MakeRect(dragStartCellLocal, dragCurrentCellLocal);
            var center = CellRectCenterPx(section, rect);
            var size = CellRectSizePx(rect);
            selectionTargetPos = center;
            selectionTargetSize = size;

            SetSelectionVisible(true);
            PositionSelectionGroup(center, size); // snap on the very first cell, no lerp needed yet

            ShowSelectionBadge();
            UpdateSelectionBadge(rect, center, size);
        }

        private void UpdateDragPreview()
        {
            var local = BoardLocalPoint();
            var section = level.Sections[dragSectionIndex];
            int cx = Mathf.Clamp(Mathf.FloorToInt(local.x / CellSize) - section.WorldOrigin.x, 0, section.Width - 1);
            int cy = Mathf.Clamp(Mathf.FloorToInt(local.y / CellSize) - section.WorldOrigin.y, 0, section.Height - 1);
            var candidate = new Vector2Int(cx, cy);

            // The selection must never cross into a hole in the silhouette, nor into an already-
            // solved cell — if extending to this candidate would include one, simply don't grow
            // the selection that far; it stops right at that edge instead of bleeding across it.
            if (IsRectSelectable(section, MakeRect(dragStartCellLocal, candidate)))
                dragCurrentCellLocal = candidate;

            var rect = MakeRect(dragStartCellLocal, dragCurrentCellLocal);
            var center = CellRectCenterPx(section, rect);
            var size = CellRectSizePx(rect);
            selectionTargetPos = center;
            selectionTargetSize = size;
            UpdateSelectionBadge(rect, center, size);
        }

        private void EndDrag()
        {
            dragging = false;
            SetSelectionVisible(false);
            HideSelectionBadge();
            var rect = MakeRect(dragStartCellLocal, dragCurrentCellLocal);
            TryPlaceRect(dragSectionIndex, rect);
        }

        private static RectInt MakeRect(Vector2Int a, Vector2Int b)
        {
            int minX = Mathf.Min(a.x, b.x);
            int minY = Mathf.Min(a.y, b.y);
            int maxX = Mathf.Max(a.x, b.x);
            int maxY = Mathf.Max(a.y, b.y);
            return new RectInt(minX, minY, maxX - minX + 1, maxY - minY + 1);
        }

        /// <summary>A cell is selectable for a NEW drag only if it's part of the artwork AND not
        /// already solved — dragging must stop at both holes and previously-completed cells.</summary>
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

        private const float SelectionFollowSharpness = 22f;

        /// <summary>Smoothly eases the fill+border toward the latest drag target every frame —
        /// this is the "viền anim chạy theo" behavior instead of an instant snap.</summary>
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
            selectionFill.gameObject.SetActive(visible);
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

        private void HideSelectionBadge()
        {
            selectionBadgeRoot.gameObject.SetActive(false);
        }

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

        // ---------------------------------------------------------------- validate

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

                if (section.IsFullySolved())
                    StartCoroutine(OnSectionCleared(sectionIndex));
            }
            else
            {
                combo = 0;
                mistakes++;
                StartCoroutine(RejectFeedback(localRect, sectionIndex));
            }
        }

        private void PaintRegion(int sectionIndex, RegionData region, bool isHint)
        {
            var section = level.Sections[sectionIndex];
            var images = cellImagesPerSection[sectionIndex];
            var clueTexts = clueTextPerSection[sectionIndex];

            var flash = isHint ? new Color(1f, 0.95f, 0.55f) : Color.white;

            // Each cell gets ITS OWN real color from the source picture (not one flat color per
            // region) — a region can legitimately span more than one shade/hue in the artwork.
            for (int x = region.Rect.x; x < region.Rect.x + region.Rect.width; x++)
            {
                for (int y = region.Rect.y; y < region.Rect.y + region.Rect.height; y++)
                {
                    Color target = section.GetCellColor(x, y);
                    StartCoroutine(FlashAndSet(images[x, y], target, flash));
                    section.MarkCellSolved(x, y);
                }
            }

            var clueTm = clueTexts[region.ClueCell.x, region.ClueCell.y];
            if (clueTm != null) clueTm.gameObject.SetActive(false);
        }

        private IEnumerator FlashAndSet(Image img, Color target, Color flash)
        {
            float t = 0f;
            while (t < 0.15f)
            {
                t += Time.deltaTime;
                img.color = Color.Lerp(flash, target, t / 0.15f);
                yield return null;
            }
            img.color = target;
        }

        private IEnumerator RejectFeedback(RectInt localRect, int sectionIndex)
        {
            var section = level.Sections[sectionIndex];
            var basePos = CellRectCenterPx(section, localRect);
            var size = CellRectSizePx(localRect);

            selectionFill.color = RejectPreviewColor;
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
            selectionFill.color = NormalPreviewColor;
        }

        // ---------------------------------------------------------------- section / level flow

        private IEnumerator OnSectionCleared(int sectionIndex)
        {
            FlashProgressBar();
            yield return new WaitForSeconds(0.2f);
            if (currentSectionIndex < level.Sections.Count - 1)
            {
                currentSectionIndex++;
                RefreshSectionActivationColors();
                yield return StartCoroutine(FrameBoard(false));
            }
            else
            {
                ShowWinPanel();
            }
        }

        private void ShowWinPanel()
        {
            // No title/subtitle/confetti — just the Next Level button. The completed picture
            // zooms out to fill the whole viewport, centered, so it reads as the finished artwork.
            winPanel.SetActive(true);
            StartCoroutine(FrameBoard(true));
        }

        private void OnNextLevelClicked()
        {
            winPanel.SetActive(false);
            currentLevelIndex++;
            SpawnLevel(currentLevelIndex);
            StartCoroutine(FrameBoard(false, instant: true));
        }

        // ---------------------------------------------------------------- progress bar

        private float ComputeProgressFraction()
        {
            int solved = 0, total = 0;
            foreach (var s in level.Sections)
            {
                foreach (var r in s.Regions)
                {
                    total++;
                    if (r.Solved) solved++;
                }
            }
            return total > 0 ? (float)solved / total : 0f;
        }

        /// <summary>Called whenever a Section completes: pop the bar in, update it, then fade it
        /// back out after a moment — unless Overview is currently open, which keeps it pinned.</summary>
        private void FlashProgressBar()
        {
            progressBarText.text = $"Progress: {Mathf.RoundToInt(ComputeProgressFraction() * 100f)}%";
            if (progressBarStickyForOverview) return; // already shown and will stay shown

            if (progressBarFadeRoutine != null) StopCoroutine(progressBarFadeRoutine);
            progressBarFadeRoutine = StartCoroutine(ProgressBarFlashRoutine());
        }

        private IEnumerator ProgressBarFlashRoutine()
        {
            yield return FadeCanvasGroup(progressBarGroup, 1f, 0.25f);
            yield return new WaitForSeconds(1.4f);
            if (!progressBarStickyForOverview)
                yield return FadeCanvasGroup(progressBarGroup, 0f, 0.35f);
        }

        private void HideProgressBarImmediate()
        {
            if (progressBarFadeRoutine != null) { StopCoroutine(progressBarFadeRoutine); progressBarFadeRoutine = null; }
            progressBarStickyForOverview = false;
            if (progressBarGroup != null) progressBarGroup.alpha = 0f;
        }

        private IEnumerator FadeCanvasGroup(CanvasGroup group, float target, float duration)
        {
            float start = group.alpha;
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                group.alpha = Mathf.Lerp(start, target, t / duration);
                yield return null;
            }
            group.alpha = target;
        }

        private IEnumerator FadeText(TextMeshProUGUI text, float target, float duration)
        {
            float start = text.color.a;
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                SetTextAlpha(text, Mathf.Lerp(start, target, t / duration));
                yield return null;
            }
            SetTextAlpha(text, target);
        }

        // ---------------------------------------------------------------- hint

        private void TickHintCooldown()
        {
            if (hintCooldownRemaining <= 0f)
            {
                if (hintButtonLabel.text != "Hint") hintButtonLabel.text = "Hint";
                hintButton.interactable = true;
                return;
            }

            hintCooldownRemaining -= Time.deltaTime;
            if (hintCooldownRemaining < 0f) hintCooldownRemaining = 0f;
            hintButtonLabel.text = hintCooldownRemaining > 0.05f ? $"Hint {Mathf.CeilToInt(hintCooldownRemaining)}s" : "Hint";
            hintButton.interactable = hintCooldownRemaining <= 0f;
        }

        private void OnHintClicked()
        {
            if (hintCooldownRemaining > 0f || winPanel.activeSelf || overviewMode) return;

            var section = level.Sections[currentSectionIndex];
            var unsolved = section.UnsolvedRegions();
            if (unsolved.Count == 0) return;

            // Simplified demo heuristic: reveal the largest remaining region in the
            // current section. The full spec's Hint picks the largest region that is
            // currently *uniquely deducible* via Shikaku elimination logic — see
            // Docs/Shidaku-Demo-Plan.md M4 §4c. Out of scope for this demo pass.
            RegionData best = unsolved[0];
            foreach (var r in unsolved)
                if (r.Area > best.Area) best = r;

            best.Solved = true;
            hintsUsed++;
            PaintRegion(currentSectionIndex, best, isHint: true);
            hintCooldownRemaining = HintCooldownDuration;

            if (section.IsFullySolved())
                StartCoroutine(OnSectionCleared(currentSectionIndex));
        }

        // ---------------------------------------------------------------- overview

        private void OnOverviewClicked()
        {
            if (winPanel.activeSelf) return;
            SetOverviewMode(!overviewMode);
        }

        private void SetOverviewMode(bool value)
        {
            overviewMode = value;
            StartCoroutine(FrameBoard(overviewMode));

            progressBarStickyForOverview = overviewMode;
            if (overviewMode)
            {
                progressBarText.text = $"Progress: {Mathf.RoundToInt(ComputeProgressFraction() * 100f)}%";
                if (progressBarFadeRoutine != null) StopCoroutine(progressBarFadeRoutine);
                progressBarFadeRoutine = StartCoroutine(FadeCanvasGroup(progressBarGroup, 1f, 0.25f));
                StartCoroutine(FadeText(tapToContinueText, 1f, 0.25f));
            }
            else
            {
                if (progressBarFadeRoutine != null) StopCoroutine(progressBarFadeRoutine);
                progressBarFadeRoutine = StartCoroutine(FadeCanvasGroup(progressBarGroup, 0f, 0.35f));
                StartCoroutine(FadeText(tapToContinueText, 0f, 0.35f));
            }
        }

        // ---------------------------------------------------------------- board pan/zoom ("camera")

        private IEnumerator FrameBoard(bool overview, bool instant = false)
        {
            var boundsCells = overview ? ComputeLevelBounds() : ComputeSectionBounds(currentSectionIndex);

            float availW = boardViewport.rect.width;
            float availH = boardViewport.rect.height;
            float neededW = boundsCells.size.x * CellSize;
            float neededH = boundsCells.size.y * CellSize;
            float targetScale = Mathf.Min(availW / neededW, availH / neededH) / BoardFramePadding;
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

        /// <summary>
        /// TIGHT bounding box (in CELL units) of only the PLAYABLE cells in this section — not its
        /// full Width x Height rectangle, which can include hole/empty margins on any side for an
        /// irregular silhouette.
        /// </summary>
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
            if (maxX < minX) { minX = 0; maxX = s.Width - 1; minY = 0; maxY = s.Height - 1; } // guard: no playable cells at all (shouldn't happen)

            float w = maxX - minX + 1;
            float h = maxY - minY + 1;
            var center = new Vector3(s.WorldOrigin.x + minX + w / 2f, s.WorldOrigin.y + minY + h / 2f, 0f);
            var size = new Vector3(w, h, 1f);
            return new Bounds(center, size);
        }

        private Bounds ComputeLevelBounds()
        {
            var b = ComputeSectionBounds(0);
            for (int i = 1; i < level.Sections.Count; i++)
                b.Encapsulate(ComputeSectionBounds(i));
            return b;
        }
    }
}
