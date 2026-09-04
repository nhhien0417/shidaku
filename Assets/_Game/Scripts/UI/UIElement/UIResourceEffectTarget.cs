using Design.Ids;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

public class UIResourceEffectTarget : MonoBehaviour, IEffectTarget
{
    [SerializeField] private UIResourceCounter _counter;
    [SerializeField] private ResourceEffectTarget _effectTarget;

    public string ResourceId => _counter?.ResourceId;

    public Transform GetTransform()
    {
        return _counter?.CurrencyIcon ?? transform;
    }

    public void IncreaseValueBy(int amount, float duration)
    {
        _counter?.IncreaseValueBy(amount, duration);
    }

    public void PlayCollectAnimation()
    {
        _effectTarget?.PlayReceiveAnim();
    }

#if UNITY_EDITOR
    private string[] GetResourceIds()
    {
        return ItemId.All;
    }
    #endif
}
