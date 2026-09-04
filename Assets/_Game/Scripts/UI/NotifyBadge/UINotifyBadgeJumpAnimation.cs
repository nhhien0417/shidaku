using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

namespace _Game.UI.NotifyBadge
{
    /// <summary>
    /// Plays a repeating "jump-jump" bounce animation on a UI notify badge icon.
    /// Attach to the badge root (or its child icon) to draw the user's attention.
    /// The sequence: jump up → land → small jump → land → pause → repeat.
    /// </summary>
    public class UINotifyBadgeJumpAnimation : MonoBehaviour
    {
        [Header("Target")]
        [Tooltip("The RectTransform to animate. Defaults to this GameObject if not set.")]
        [SerializeField] private RectTransform _target;

        [Header("Jump Settings")]
        [SerializeField] private float _firstJumpHeight = 30f;
        [SerializeField] private float _firstJumpDuration = 0.25f;

        [SerializeField] private float _secondJumpHeight = 25f;
        [SerializeField] private float _secondJumpDuration = 0.18f;

        [Header("Timing")]
        [Tooltip("Pause between the double-jump cycles.")]
        [SerializeField] private float _pauseBetweenCycles = 2f;

        [Header("Squash & Stretch")]
        [SerializeField] private bool _useSquashStretch = true;
        [SerializeField] private float _squashScaleX = 1.2f;
        [SerializeField] private float _squashScaleY = 0.8f;
        [SerializeField] private float _stretchScaleX = 0.85f;
        [SerializeField] private float _stretchScaleY = 1.2f;
        [SerializeField] private float _squashStretchDuration = 0.08f;

        private Sequence _sequence;
        private Vector2 _originalAnchoredPos;

        private void Awake()
        {
            if (_target == null)
                _target = GetComponent<RectTransform>();
        }

        private void OnEnable()
        {
            if (_target == null) return;

            _originalAnchoredPos = _target.anchoredPosition;
            Play();
        }

        private void OnDisable()
        {
            Stop();
        }

        [Button("Play")]
        public void Play()
        {
            if (_target == null) return;

            Stop();

            _target.anchoredPosition = _originalAnchoredPos;
            _target.localScale = Vector3.one;

            _sequence = DOTween.Sequence();

            // ── First jump (big) ──
            if (_useSquashStretch)
            {
                // Anticipation squash before launch
                _sequence.Append(
                    _target.DOScale(new Vector3(_squashScaleX, _squashScaleY, 1f), _squashStretchDuration)
                        .SetEase(Ease.OutQuad));
            }

            // Jump up
            _sequence.Append(
                _target.DOAnchorPosY(_originalAnchoredPos.y + _firstJumpHeight, _firstJumpDuration * 0.5f)
                    .SetEase(Ease.OutQuad));

            if (_useSquashStretch)
            {
                // Stretch at peak
                _sequence.Join(
                    _target.DOScale(new Vector3(_stretchScaleX, _stretchScaleY, 1f), _firstJumpDuration * 0.3f)
                        .SetEase(Ease.OutQuad));
            }

            // Fall down
            _sequence.Append(
                _target.DOAnchorPosY(_originalAnchoredPos.y, _firstJumpDuration * 0.5f)
                    .SetEase(Ease.InQuad));

            if (_useSquashStretch)
            {
                // Landing squash
                _sequence.Join(
                    _target.DOScale(new Vector3(_squashScaleX, _squashScaleY, 1f), _squashStretchDuration)
                        .SetEase(Ease.OutQuad));

                // Recover to normal
                _sequence.Append(
                    _target.DOScale(Vector3.one, _squashStretchDuration)
                        .SetEase(Ease.OutQuad));
            }

            // ── Second jump (small) ──
            if (_useSquashStretch)
            {
                _sequence.Append(
                    _target.DOScale(new Vector3(_squashScaleX, _squashScaleY, 1f), _squashStretchDuration * 0.7f)
                        .SetEase(Ease.OutQuad));
            }

            // Jump up (smaller)
            _sequence.Append(
                _target.DOAnchorPosY(_originalAnchoredPos.y + _secondJumpHeight, _secondJumpDuration * 0.5f)
                    .SetEase(Ease.OutQuad));

            if (_useSquashStretch)
            {
                _sequence.Join(
                    _target.DOScale(new Vector3(_stretchScaleX, _stretchScaleY, 1f), _secondJumpDuration * 0.3f)
                        .SetEase(Ease.OutQuad));
            }

            // Fall down
            _sequence.Append(
                _target.DOAnchorPosY(_originalAnchoredPos.y, _secondJumpDuration * 0.5f)
                    .SetEase(Ease.InQuad));

            if (_useSquashStretch)
            {
                // Landing squash
                _sequence.Join(
                    _target.DOScale(new Vector3(_squashScaleX, _squashScaleY, 1f), _squashStretchDuration)
                        .SetEase(Ease.OutQuad));

                // Recover
                _sequence.Append(
                    _target.DOScale(Vector3.one, _squashStretchDuration)
                        .SetEase(Ease.OutQuad));
            }

            // ── Pause before next cycle ──
            _sequence.AppendInterval(_pauseBetweenCycles);

            // Loop forever
            _sequence.SetLoops(-1, LoopType.Restart);
            _sequence.SetUpdate(true); // Ignore timeScale
        }

        [Button("Stop")]
        public void Stop()
        {
            if (_sequence != null && _sequence.IsActive())
            {
                _sequence.Kill();
                _sequence = null;
            }

            if (_target != null)
            {
                _target.DOKill();
                _target.anchoredPosition = _originalAnchoredPos;
                _target.localScale = Vector3.one;
            }
        }
    }
}

