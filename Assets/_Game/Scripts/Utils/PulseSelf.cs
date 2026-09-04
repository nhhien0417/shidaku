using DG.Tweening;
using UnityEngine;

public class PulseSelf : MonoBehaviour
{
    public float amount = 0.1f;
    public float speed = 2f;

    private Tween _pulseTween;
    private Vector3 _initialScale;

    private void OnEnable()
    {
        _initialScale = transform.localScale;
        var pulseScale = _initialScale * (1f + Mathf.Max(0f, amount));
        var duration = 0.5f / Mathf.Max(0.01f, speed);

        _pulseTween = transform
            .DOScale(pulseScale, duration)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo)
            .SetLink(gameObject, LinkBehaviour.KillOnDestroy);
    }

    private void OnDisable()
    {
        _pulseTween?.Kill();
        _pulseTween = null;
        transform.localScale = _initialScale;
    }
}
