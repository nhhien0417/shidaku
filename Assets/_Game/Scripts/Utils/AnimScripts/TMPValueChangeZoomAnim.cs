using System;
using DG.Tweening;
using TMPro;
using UnityEngine;

public class TMPValueChangeZoomAnim : MonoBehaviour
{
    [SerializeField] private TMP_Text _text;
    [SerializeField] private float _zoomScale = 1.2f;

    public void SetValue(int from, int to, float duration, Action onComplete)
    {
        if (from == to)
        {
            _text.text = to.ToString();
            onComplete?.Invoke();
            return;
        }
        
        _text.transform.DOScale(_zoomScale, duration*0.2f);
        DOTween.To(() => from, x => 
        {
            from = x;
            _text.text = from.ToString();
        }, to, duration)
        .SetEase(Ease.Linear)
        .OnComplete(() =>
        {
            _text.transform.localScale = Vector3.one;
            onComplete?.Invoke();
        })
        .OnKill(() =>
        {
            _text.transform.localScale = Vector3.one;
            onComplete?.Invoke();
        });
    }
    
    public void SetText(string text)
    {
        _text.text = text;
    }
}
