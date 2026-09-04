using System;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

public class ResourceEffectTarget_ZoomAnim : ResourceEffectTarget
{
    [SerializeField] private Transform _tsfZoom;
    [SerializeField] private float _zoomScale = 1.5f;
    [SerializeField] private float _animDuration = 0.3f;

    [Button]
    public override float PlaySendAnim(Action onComplete = null)
    {
        if (_tsfZoom == null)
            return 0f;

        var duration = _animDuration * 0.5f;
        _tsfZoom.DOKill();
        _tsfZoom.DOScale(_zoomScale, duration).OnComplete(() =>
        {
            _tsfZoom.DOScale(1f, duration).OnComplete(() =>
            {
                _tsfZoom.localScale = Vector3.one;
                onComplete?.Invoke();
            });
        });

        return duration*0.5f;
    }

    [Button]
    public override float PlayReceiveAnim(Action onComplete = null)
    {
        if (_tsfZoom == null)
            return 0f;

        var duration = _animDuration * 0.5f;
        _tsfZoom.DOKill();
        _tsfZoom.DOScale(_zoomScale, duration).OnComplete(() =>
        {
            _tsfZoom.DOScale(1f, duration).OnComplete(() =>
            {
                _tsfZoom.localScale = Vector3.one;
                onComplete?.Invoke();
            });
        });

        return duration*0.5f;
    }
}
