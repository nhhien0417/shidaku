using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class UIGroupTransitionPopupZoom : UIGroupTransition
{
    [SerializeField] private Transform _pnlContent;
    [SerializeField] private CanvasGroup _pnlPopup;
    [SerializeField] private float _duration = 0.3f;
    [SerializeField] private Ease _ease = Ease.Linear;
    
    public override void Show(Action onComplete, bool immediately = false)
    {
        var duration = immediately ? 0 : _duration;
        
        _pnlContent.DOKill();
        _pnlPopup.DOKill();
        
        TransitionPhase = UITransitionPhase.Showing;

        _pnlContent.localScale = Vector3.one * 0.5f;
        _pnlContent.DOScale(Vector3.one, duration).SetEase(_ease);
        
        _pnlPopup.alpha = 0;
        _pnlPopup.DOFade(1, duration).OnComplete(() =>
        {
            TransitionPhase = UITransitionPhase.Complete;
            onComplete?.Invoke();
        });
    }

    public override void Hide(Action onComplete, bool immediately = false)
    {
        var duration = immediately ? 0 : _duration;
        
        _pnlContent.DOKill();
        _pnlPopup.DOKill();
        
        TransitionPhase = UITransitionPhase.Hiding;
        
        _pnlContent.DOScale(Vector3.one * 0.5f, duration).SetEase(_ease);
        
        _pnlPopup.alpha = 1;
        _pnlPopup.DOFade(0, duration).OnComplete(() =>
        {
            TransitionPhase = UITransitionPhase.Complete;
            onComplete?.Invoke();
        });
    }
    
    public override void CompleteImmediately()
    {
        if (TransitionPhase == UITransitionPhase.Complete)
            return;

        _pnlContent.DOComplete();
        _pnlPopup.DOComplete();
        TransitionPhase = UITransitionPhase.Complete;
    }
    
    public override float GetTransitionDuration()
    {
        return _duration;
    }
}
