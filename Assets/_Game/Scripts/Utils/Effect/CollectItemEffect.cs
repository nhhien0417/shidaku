using System;
using AssetKits.ParticleImage;
using AssetsHolder;
using Design.Structures;
using UnityEngine;

public class CollectItemEffect : MonoBehaviour
{
    [SerializeField] private ParticleImage _particle;
    [SerializeField] private float _fixedHeight = 80f;
    private Sprite _customSprite;

    public void SetCustomSprite(Sprite sprite)
    {
        _customSprite = sprite;
    }

    public async void Play(IEffectTarget target, Item item, int amount, float duration, Action onComplete = null)
    {
        if (target == null)
        {
            onComplete?.Invoke();
            return;
        }

        var sprite = _customSprite;
        _customSprite = null;

        if (sprite == null)
        {
            sprite = await ResourcesHolder.Instance.GetItemSpriteAsync(item);
        }

        if (sprite == null)
        {
            Debug.LogError($"Sprite not found for item: {item.Id}");
            onComplete?.Invoke();
            return;
        }

        var aspectRatio = (float)sprite.texture.width / sprite.texture.height;
        var width = _fixedHeight * aspectRatio;
        _particle.startSize = new SeparatedMinMaxCurve()
        {
            separated = true,
            xCurve = new ParticleSystem.MinMaxCurve()
            {
                mode = ParticleSystemCurveMode.Constant,
                constant = width
            },
            yCurve = new ParticleSystem.MinMaxCurve()
            {
                mode = ParticleSystemCurveMode.Constant,
                constant = _fixedHeight
            },
            zCurve = new ParticleSystem.MinMaxCurve()
            {
                mode = ParticleSystemCurveMode.Constant,
                constant = 1f
            }
        };

        _particle.duration = duration;
        _particle.rateOverTime = Mathf.Clamp(amount, 0, 15f) / duration;
        _particle.sprite = sprite;
        _particle.attractorTarget = target.GetTransform();

        _particle.onFirstParticleFinished.RemoveAllListeners();
        _particle.onFirstParticleFinished.AddListener(() =>
        {
            target.IncreaseValueBy(amount, duration);
        });

        _particle.onAnyParticleFinished.RemoveAllListeners();
        _particle.onAnyParticleFinished.AddListener(() =>
        {
            target.PlayCollectAnimation();
            AudioManager.Instance.PlaySFXOneShot("item_collect");
        });

        _particle.onParticleStop.RemoveAllListeners();
        _particle.onParticleStop.AddListener(() => onComplete?.Invoke());

        _particle.Play();
    }
}
