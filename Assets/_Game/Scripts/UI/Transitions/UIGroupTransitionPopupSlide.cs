using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

public class UIGroupTransitionPopupSlide : UIGroupTransition
{
    [SerializeField] private RectTransform _pnlContent;
    [SerializeField] private CanvasGroup _pnlPopup;
    [SerializeField] private float _duration = 0.3f;
    [SerializeField] private Ease _ease = Ease.Linear;
    [SerializeField] private Vector2 _startAnchorPos;
    [SerializeField] private Vector2 _desAnchorPos;
    
    public override void Show( Action onComplete, bool immediately = false)
    {
        var duration = immediately ? 0 : _duration;
        
        _pnlContent.DOKill();
        _pnlPopup.DOKill();
        
        TransitionPhase = UITransitionPhase.Showing;
        
        _pnlContent.anchoredPosition = _startAnchorPos;
        _pnlContent.DOAnchorPos(_desAnchorPos, duration).SetEase(_ease);
        
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
        
        _pnlContent.anchoredPosition = _desAnchorPos;
        _pnlContent.DOAnchorPos(_startAnchorPos, duration);
        
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
    
    #if UNITY_EDITOR
    [Button]
    public void SetPnlContentAsStartAnchorPos()
    {
        if (_pnlContent == null)
        {
            Debug.LogError("PnlContent is not assigned.");
            return;
        }
        _startAnchorPos = _pnlContent.anchoredPosition;
        UnityEditor.EditorUtility.SetDirty(gameObject);
    }
    
    [Button]
    public void SetPnlContentAsDesAnchorPos()
    {
        if (_pnlContent == null)
        {
            Debug.LogError("PnlContent is not assigned.");
            return;
        }
        _desAnchorPos = _pnlContent.anchoredPosition;
        UnityEditor.EditorUtility.SetDirty(gameObject);
    }
    #endif
}
