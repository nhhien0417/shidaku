using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Shidaku
{
    public class UICell : MonoBehaviour
    {
        [SerializeField] private Image _backgroundImage;
        [SerializeField] private Image _blockImage;
        [SerializeField] private GameObject _borderObject;
        [SerializeField] private TextMeshProUGUI _clueText;

        [SerializeField] private Color _hoverColor;
        [SerializeField] private float _squeezeWidth;
        [SerializeField] private float _squeezeHeight;

        [SerializeField] private float _hoverPopDuration;
        [SerializeField] private float _acceptPopDuration;
        [SerializeField] private float _rejectPopDuration;

        private static readonly Vector3 FlatScale = Vector3.zero;
        private static readonly Vector3 RisenScale = Vector3.one;

        public float AcceptPopDuration => _acceptPopDuration;
        public float RejectPopDuration => _rejectPopDuration;

        private RectTransform _rect;
        private RectTransform _blockRect;
        private Tween _tween;

        private void Awake()
        {
            _rect = (RectTransform)transform;
            _blockRect = _blockImage.rectTransform;
        }

        private void OnDisable() => _tween?.Kill(true);

        public void PlaceAt(Vector2 anchoredPosition, Vector2 size)
        {
            _rect.anchoredPosition = anchoredPosition;
            _rect.sizeDelta = size;
        }

        public void SetClue(int area)
        {
            _clueText.text = area.ToString();
            _clueText.gameObject.SetActive(true);
        }

        public void ClearClue() => _clueText.gameObject.SetActive(false);

        public void SetColorInstant(Color backgroundColor, bool showBorder)
        {
            _tween?.Kill(true);
            _backgroundImage.color = backgroundColor;
            _borderObject.SetActive(showBorder);
            _blockImage.color = _hoverColor;
            _blockRect.localScale = FlatScale;
        }

        public void SetBlockColor(Color color) => _blockImage.color = color;
        public void SetBackgroundColor(Color color) => _backgroundImage.color = color;

        public void AnimateHoverEnter() => PlayPopIn(_hoverPopDuration, _hoverColor, 0f);
        public void AnimateHoverExit() => PlayPopOut(_hoverPopDuration, 0f);
        public void AnimateAccept(Color targetColor, float delay = 0f) => PlayPopIn(_acceptPopDuration, targetColor, delay);
        public void AnimateReject(float delay = 0f) => PlayPopOut(_rejectPopDuration, delay);

        private void PlayPopIn(float duration, Color? targetColor, float delay)
        {
            _tween?.Kill(true);
            _blockRect.localScale = FlatScale;
            if (targetColor.HasValue) _blockImage.color = targetColor.Value;

            float t1 = duration * 0.5f;
            float t2 = duration - t1;

            var seq = DOTween.Sequence();
            seq.SetLink(gameObject);
            if (delay > 0f) seq.AppendInterval(delay);
            seq.Append(_blockRect.DOScale(new Vector3(_squeezeWidth, _squeezeHeight, 1f), t1).SetEase(Ease.OutQuad));
            seq.Append(_blockRect.DOScale(RisenScale, t2).SetEase(Ease.OutBounce));

            _tween = seq;
        }

        private void PlayPopOut(float duration, float delay)
        {
            _tween?.Kill(true);

            float t1 = duration * 0.5f;
            float t2 = duration - t1;

            var seq = DOTween.Sequence();
            seq.SetLink(gameObject);
            if (delay > 0f) seq.AppendInterval(delay);
            seq.Append(_blockRect.DOScale(new Vector3(_squeezeWidth, _squeezeHeight, 1f), t1).SetEase(Ease.InBounce));
            seq.Append(_blockRect.DOScale(FlatScale, t2).SetEase(Ease.InQuad));

            _tween = seq;
        }
    }
}
