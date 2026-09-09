using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Shidaku
{
    // Screen-space HUD chrome: level label, progress bar, hint/overview/win/next-level
    // buttons. Self-contained (drives its own fades and hint cooldown); raises events for
    // clicks instead of reaching into Board.
    public class GameplayUI : MonoBehaviour
    {
        private const float HintCooldownDuration = 0f; // placeholder — hint has no real cooldown yet

        [SerializeField] private TextMeshProUGUI levelLabel;
        [SerializeField] private CanvasGroup progressBarGroup;
        [SerializeField] private TextMeshProUGUI progressBarText;
        [SerializeField] private TextMeshProUGUI tapToContinueText;
        [SerializeField] private Button overviewButton;
        [SerializeField] private Button hintButton;
        [SerializeField] private TextMeshProUGUI hintButtonLabel;
        [SerializeField] private GameObject winPanel;
        [SerializeField] private Button nextLevelButton;

        public event Action OnHintClicked;
        public event Action OnOverviewClicked;
        public event Action OnNextLevelClicked;

        private Coroutine progressBarFadeRoutine;
        private bool progressBarStickyForOverview;
        private float hintCooldownRemaining;

        private void Awake()
        {
            // Win screen: no title/subtitle/confetti — just the Next Level button, puzzle
            // stays visible behind it (WinPanel's own Image is fully transparent in the scene).
            var nextBtnRect = nextLevelButton.GetComponent<RectTransform>();
            nextBtnRect.anchorMin = new Vector2(0.5f, 0f);
            nextBtnRect.anchorMax = new Vector2(0.5f, 0f);
            nextBtnRect.pivot = new Vector2(0.5f, 0.5f);
            nextBtnRect.anchoredPosition = new Vector2(0f, 110f);
            nextBtnRect.sizeDelta = new Vector2(220f, 110f);

            overviewButton.onClick.AddListener(() => { if (!winPanel.activeSelf) OnOverviewClicked?.Invoke(); });
            hintButton.onClick.AddListener(() => { hintCooldownRemaining = HintCooldownDuration; OnHintClicked?.Invoke(); });
            nextLevelButton.onClick.AddListener(() => OnNextLevelClicked?.Invoke());

            tapToContinueText.text = "Tap anywhere to continue";
            SetTextAlpha(tapToContinueText, 0f);
            winPanel.SetActive(false);
            HideProgressBarImmediate();
        }

        private void Update() => TickHintCooldown();

        public void SetLevelLabel(int levelIndex) => levelLabel.text = $"Level {levelIndex}";

        public void ShowWin() => winPanel.SetActive(true);
        public void HideWin() => winPanel.SetActive(false);

        // Pop the bar in, update it, then fade back out after a moment — unless Overview is
        // open, which keeps it pinned.
        public void FlashProgress(float fraction)
        {
            progressBarText.text = $"Progress: {Mathf.RoundToInt(fraction * 100f)}%";
            if (progressBarStickyForOverview) return;

            if (progressBarFadeRoutine != null) StopCoroutine(progressBarFadeRoutine);
            progressBarFadeRoutine = StartCoroutine(ProgressBarFlashRoutine());
        }

        public void SetOverviewVisuals(bool overviewOn, float progressFraction)
        {
            progressBarStickyForOverview = overviewOn;
            if (overviewOn)
            {
                progressBarText.text = $"Progress: {Mathf.RoundToInt(progressFraction * 100f)}%";
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

        private void HideProgressBarImmediate()
        {
            if (progressBarFadeRoutine != null) { StopCoroutine(progressBarFadeRoutine); progressBarFadeRoutine = null; }
            progressBarStickyForOverview = false;
            progressBarGroup.alpha = 0f;
        }

        private IEnumerator ProgressBarFlashRoutine()
        {
            yield return FadeCanvasGroup(progressBarGroup, 1f, 0.25f);
            yield return new WaitForSeconds(1.4f);
            if (!progressBarStickyForOverview)
                yield return FadeCanvasGroup(progressBarGroup, 0f, 0.35f);
        }

        private static IEnumerator FadeCanvasGroup(CanvasGroup group, float target, float duration)
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

        private static IEnumerator FadeText(TextMeshProUGUI text, float target, float duration)
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

        private static void SetTextAlpha(TextMeshProUGUI text, float alpha)
        {
            var c = text.color;
            c.a = alpha;
            text.color = c;
        }
    }
}
