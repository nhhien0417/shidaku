using UnityEngine;

public interface IEffectTarget
{
    public Transform GetTransform();
    public void IncreaseValueBy(int amount, float duration);
    public void PlayCollectAnimation();
}
