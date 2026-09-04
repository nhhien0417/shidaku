using System;
using DG.Tweening;
using UnityEngine;

public class SnapLoading_FadeEffect : SnapLoading
{
    public SpriteRenderer backgroundImg;
    public float showHideDuration;

    public override void Show(string customId = "", bool useAnim = true, Action callback = null)
    {
        gameObject.SetActive(true);
        backgroundImg.DOKill();

        if (useAnim)
        {
            Color bgColor = backgroundImg.color;
            bgColor.a = 0;
            backgroundImg.color = bgColor;
            backgroundImg.DOFade(1, showHideDuration).OnComplete(() => callback?.Invoke());
        }
        else
        {
            backgroundImg.DOFade(1, 0);
            callback?.Invoke();
        }
    }

    public override void Hide()
    {
        Color bgColor = backgroundImg.color;
        bgColor.a = 1;
        backgroundImg.color = bgColor;
        backgroundImg.DOKill();
        backgroundImg.DOFade(0, showHideDuration).OnComplete(() =>
        {
            gameObject.SetActive(false);
        });
    }
}
