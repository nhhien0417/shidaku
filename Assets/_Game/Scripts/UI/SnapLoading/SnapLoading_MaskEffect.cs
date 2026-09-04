using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class SnapLoading_MaskEffect : SnapLoading
{
    public List<Sprite> spriteMasks;
    public SpriteMask mask;
    public SpriteRenderer backgroundImg;
    public float showHideDuration;
    public float scaleRatio;

    public override void Show(string customId = "", bool useAnim = true, System.Action callback = null)
    {
        gameObject.SetActive(true);
        backgroundImg.DOKill();

        if (useAnim)
        {
            RandomSpriteMask();

            mask.transform.DOKill();
            mask.transform.localScale = Vector3.zero;
            mask.transform.DOScale(Vector3.one * scaleRatio, showHideDuration).OnComplete(() => callback?.Invoke());

            Color bgColor = backgroundImg.color;
            bgColor.a = 0.5f;
            backgroundImg.color = bgColor;
            backgroundImg.DOFade(1, showHideDuration);
        }
        else
        {
            mask.transform.localScale = Vector3.one * scaleRatio;
            backgroundImg.DOFade(1, 0);
            callback?.Invoke();
        }
    }

    public override void Hide()
    {
        RandomSpriteMask();

        mask.transform.DOKill();
        mask.transform.localScale = Vector3.one * scaleRatio;
        mask.transform.DOScale(Vector3.zero, showHideDuration)
            .OnComplete(() =>
            {
                gameObject.SetActive(false);
            });

        Color bgColor = backgroundImg.color;
        bgColor.a = 1;
        backgroundImg.color = bgColor;
        backgroundImg.DOKill();
        backgroundImg.DOFade(0.5f, showHideDuration);
    }

    private void RandomSpriteMask()
    {
        if (spriteMasks.Count > 0)
        {
            int index = Random.Range(0, spriteMasks.Count);

            if (spriteMasks[index] != null)
            {
                mask.sprite = spriteMasks[index];
            }
        }
    }
}
