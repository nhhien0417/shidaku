using System;
using DG.Tweening;
using UnityEngine;

public class HideOnUIGroupShow : MonoBehaviour
{
    [SerializeField] private float _duration;
    [SerializeField] private CanvasGroup _canvasGroup;

    private void OnUIGroupShowed(UIGroup group)
    {
        if (group == null)
            return;

        _canvasGroup.DOKill();
        _canvasGroup.DOFade(0f, _duration);
    }
    
    private void OnUIGroupHided(UIGroup group)
    {
        if (UIManager.Instance.TopUIGroup != null)
            return;

        _canvasGroup.DOKill();
        _canvasGroup.DOFade(1f, _duration);
    }
    
    private void Start()
    {
        UIManager.Instance?.RegisterOnUIGroupShowed(OnUIGroupShowed);
        UIManager.Instance?.RegisterOnUIGroupHided(OnUIGroupHided);
    }

    private void OnDestroy()
    {
        UIManager.Instance?.UnRegisterOnUIGroupShowed(OnUIGroupShowed);
        UIManager.Instance?.UnRegisterOnUIGroupHided(OnUIGroupHided);
    }
}
