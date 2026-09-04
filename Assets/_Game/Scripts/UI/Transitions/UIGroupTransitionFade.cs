using System;
using DG.Tweening;
using UnityEngine;

public class UIGroupTransitionFade : UIGroupTransition
{
    [SerializeField] private CanvasGroup _pnlPopup;
    [SerializeField] private float _duration = 0.3f;

    public override void Show(Action onComplete, bool immediately = false)
    {
        var duration = immediately ? 0 : _duration;

        _pnlPopup.DOKill();
        _pnlPopup.alpha = 0;
        TransitionPhase = UITransitionPhase.Showing;
        _pnlPopup.DOFade(1, duration).OnComplete(() =>
        {
            TransitionPhase = UITransitionPhase.Complete;
            onComplete?.Invoke();
        });
    }

    public override void Hide(Action onComplete, bool immediately = false)
    {
        var duration = immediately ? 0 : _duration;

        _pnlPopup.DOKill();
        _pnlPopup.alpha = 1;
        TransitionPhase = UITransitionPhase.Hiding;
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
        
        _pnlPopup.DOComplete();
        TransitionPhase = UITransitionPhase.Complete;
    }

    public override float GetTransitionDuration()
    {
        return _duration;
    }
}
