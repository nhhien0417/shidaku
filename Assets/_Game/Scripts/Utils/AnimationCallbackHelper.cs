using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationCallbackHelper : MonoBehaviour
{
    private Action callback;

    public void SetCallback(Action callback)
    {
        this.callback = callback;
    }

    public void InvokeCallback()
    {
        if (callback != null)
        {
            callback.Invoke();
            callback = null;
        }
    }
}
