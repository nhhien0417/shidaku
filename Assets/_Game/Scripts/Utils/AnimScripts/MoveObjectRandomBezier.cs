using System;
using System.Collections;
using System.Collections.Generic;
using MEC;
using UnityEngine;
using Random = UnityEngine.Random;

public class MoveObjectRandomBezier : MonoBehaviour
{
    [SerializeField] private float _zoomScale = 0f;
    [SerializeField] private AnimationCurve _zoomCurve = AnimationCurve.Constant(0, 1, 0);
    private CoroutineHandle moveObjectHandle;

    public void Move(Vector3 startPos, Vector3 endPos, float duration, Action onComplete)
    {
        Timing.KillCoroutines(moveObjectHandle);
        moveObjectHandle = Timing.RunCoroutine(MoveObject(startPos, endPos, duration, onComplete));
    }

    private IEnumerator<float> MoveObject(Vector3 startPos, Vector3 endPos, float duration, Action onComplete)
    {
        if (duration <= 0f)
        {
            transform.position = endPos;
            onComplete?.Invoke();
            yield break;
        }
        
        var controlPoint1 = GetRandomPoint(startPos, endPos);
        var timer = 0f;

        while (timer < duration)
        {
            var distanceCovered = timer / duration;
            var position = MathUtils.CalculateQuadraticBezierPoint(distanceCovered, startPos, controlPoint1, endPos);
            transform.position = position;
            var zoomFactor = 1f + _zoomCurve.Evaluate(distanceCovered) * _zoomScale;
            transform.localScale = new Vector3(zoomFactor, zoomFactor, zoomFactor);
            timer += Time.deltaTime;
            yield return Timing.WaitForOneFrame;
        }
        
        transform.position = endPos;
        onComplete?.Invoke();
    }
    
    private Vector3 GetRandomPoint(Vector3 pointA, Vector3 pointB)
    {
        float randomX = Random.Range(pointA.x-75, pointB.x+75);
        float randomY = Random.Range(pointA.y, pointB.y);
        float randomZ = Random.Range(pointA.z, pointB.z);

        return new Vector3(randomX, randomY, randomZ);
    }
}