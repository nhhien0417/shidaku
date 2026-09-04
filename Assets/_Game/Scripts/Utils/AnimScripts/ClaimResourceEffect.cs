using System;
using System.Collections;
using System.Collections.Generic;
using Lean.Pool;
using MEC;
using Sirenix.OdinInspector;
using UnityEngine;

public class ClaimResourceEffect : MonoBehaviour
{
    [SerializeField] private MoveObjectRandomBezier resourcePrefab;
    
    private CoroutineHandle createEffectHandle;
    
    [Button]
    public void PlayEffect(ResourceEffectTarget startTarget, ResourceEffectTarget endTarget, float duration, int amount, float delay, Action onStepComplete, Action onComplete)
    {
        Timing.KillCoroutines(createEffectHandle);
        createEffectHandle = Timing.RunCoroutine(CreateEffect(startTarget, endTarget, duration, amount, delay, onStepComplete, onComplete));
    }
    
    private IEnumerator<float> CreateEffect(ResourceEffectTarget startTarget, ResourceEffectTarget endTarget, float duration, int amount, float delay, Action onStepComplete, Action onComplete)
    {
        yield return Timing.WaitForSeconds(delay);
        
        var delayToNextAnim = startTarget?.PlaySendAnim() ?? 0;
        yield return Timing.WaitForSeconds(delayToNextAnim);
        
        var batchCount = amount / 3;
        for(int i = 1; i <= amount; i++)
        {
            var resource = LeanPool.Spawn(resourcePrefab, startTarget.Position, Quaternion.identity, transform);
            resource.Move(startTarget.Position, endTarget.Position, duration, () =>
            {
                endTarget.PlayReceiveAnim();
                LeanPool.Despawn(resource.gameObject);
                onStepComplete?.Invoke();
            });
            
            if (i % batchCount == 0)
                yield return Timing.WaitForSeconds(0.1f);
        }

        yield return Timing.WaitForSeconds(duration + 0.5f);
        
        onComplete?.Invoke();
    }
    
    [Button]
    public void PlayEffect(Vector3 startPos, Vector3 endPos, float duration, int amount, Action onStepComplete, Action onComplete)
    {
        Timing.KillCoroutines(createEffectHandle);
        createEffectHandle = Timing.RunCoroutine(CreateEffect(startPos, endPos, duration, amount, onStepComplete, onComplete));
    }

    private IEnumerator<float> CreateEffect(Vector3 startPos, Vector3 endPos, float duration, int amount,
        Action onStepComplete, Action onComplete)
    {
        var batchCount = amount / 3;
        for(int i = 1; i <= amount; i++)
        {
            var resource = LeanPool.Spawn(resourcePrefab, startPos, Quaternion.identity, transform);
            resource.Move(startPos, endPos, duration, () =>
            {
                LeanPool.Despawn(resource.gameObject);
                onStepComplete?.Invoke();
            });
            
            if (i % batchCount == 0)
             yield return Timing.WaitForSeconds(0.1f);
        }

        yield return Timing.WaitForSeconds(duration + 0.5f);
        
        onComplete?.Invoke();
    }
}
