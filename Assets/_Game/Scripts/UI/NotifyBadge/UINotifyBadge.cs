using System.Collections.Generic;
using DG.Tweening;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

namespace _Game.UI.NotifyBadge
{
    /// <summary>
    /// Attach as a child of any button/icon to show a red notification badge.
    /// Self-manages visibility by subscribing to <see cref="BadgeNotificationManager"/>.
    /// </summary>
    public class UINotifyBadge : MonoBehaviour
    {
        [ValueDropdown("GetNotificationTypes")] [SerializeField]
        private List<string> _notificationKeys = new();

        [Tooltip("Root transform of the badge visuals (scaled to 0 when hidden).")] [SerializeField]
        private Transform _badgeRoot;

        [Tooltip("Optional — displays count or '!' when count is 1.")] [SerializeField]
        private TextMeshProUGUI _txtCount;

        [Header("Animation")] [SerializeField] private float _showDuration = 0.3f;
        [SerializeField] private float _hideDuration = 0.15f;
        [SerializeField] private Ease _showEase = Ease.OutBack;
        [SerializeField] private Ease _hideEase = Ease.InBack;

        [Tooltip("Optional — looping jump animation that plays while badge is visible.")]
        [SerializeField] private UINotifyBadgeJumpAnimation _jumpAnimation;

        // ---- Odin dropdown helper ----
#if UNITY_EDITOR
        private static string[] GetNotificationTypes() => BadgeNotificationType.All;
#endif

        // ---- Lifecycle ----
        private void OnEnable()
        {
            if (BadgeNotificationManager.Instance != null)
            {
                BadgeNotificationManager.Instance.OnNotificationChanged += OnNotificationChanged;
                // Immediately sync state
                UpdateBadge(false);
            }
        }

        private void OnDisable()
        {
            if (BadgeNotificationManager.Instance != null)
                BadgeNotificationManager.Instance.OnNotificationChanged -= OnNotificationChanged;
        }

        // ---- Internals ----
        private void OnNotificationChanged(string key)
        {
            if (_notificationKeys.Contains(key))
                UpdateBadge(true);
        }

        private void UpdateBadge(bool animate)
        {
            if (BadgeNotificationManager.Instance == null) return;

            bool shouldShow = BadgeNotificationManager.Instance.HasAnyNotification(_notificationKeys);
            int count = BadgeNotificationManager.Instance.GetCount(_notificationKeys);

            // Update text
            if (_txtCount != null)
                _txtCount.text = count <= 1 ? "!" : count.ToString();

            if (shouldShow)
                ShowBadge(animate);
            else
                HideBadge(animate);
        }

        private void ShowBadge(bool animate)
        {
            if (_badgeRoot == null) return;

            _badgeRoot.DOKill();

            if (animate)
            {
                _badgeRoot.localScale = Vector3.zero;
                _badgeRoot.gameObject.SetActive(true);
                _badgeRoot.DOScale(Vector3.one, _showDuration).SetEase(_showEase)
                    .OnComplete(() => _jumpAnimation?.Play());
            }
            else
            {
                _badgeRoot.localScale = Vector3.one;
                _badgeRoot.gameObject.SetActive(true);
                _jumpAnimation?.Play();
            }
        }

        private void HideBadge(bool animate)
        {
            if (_badgeRoot == null) return;

            _jumpAnimation?.Stop();
            _badgeRoot.DOKill();

            if (animate)
            {
                _badgeRoot.DOScale(Vector3.zero, _hideDuration).SetEase(_hideEase)
                    .OnComplete(() => _badgeRoot.gameObject.SetActive(false));
            }
            else
            {
                _badgeRoot.localScale = Vector3.zero;
                _badgeRoot.gameObject.SetActive(false);
            }
        }
    }
}
