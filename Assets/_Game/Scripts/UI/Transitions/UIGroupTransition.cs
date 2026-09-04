using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class UIGroupTransition : MonoBehaviour
{
    public int TransitionPhase { get; protected set; } = UITransitionPhase.Complete;

    public abstract void Show(Action onComplete, bool immediately = false);
    public abstract void Hide(Action onComplete, bool immediately = false);
    public abstract void CompleteImmediately();
    public abstract float GetTransitionDuration();
    public virtual int GetTransitionPhase() => TransitionPhase;
}
