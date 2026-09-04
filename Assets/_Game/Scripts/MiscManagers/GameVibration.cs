using System.Collections;
using System.Collections.Generic;
using HapticFeedback;
using UnityEngine;

public class GameVibration : Singleton<GameVibration>
{
    private bool isVibrationEnabled = true;
    private int _suppressedFrame = -1;

    public void SuppressHaptic()
    {
        _suppressedFrame = Time.frameCount;
    }

    public void Haptic(FeedbackType type, bool defaultToRegularVibrate = false)
    {
        if (isVibrationEnabled && _suppressedFrame != Time.frameCount)
            HapticFeedbackGenerator.Haptic(type, defaultToRegularVibrate);
    }
    
    private void OnVibrationChanged(object value)
    {
        if (value is bool bValue)
            isVibrationEnabled = bValue;
    }

    protected override void Init()
    {
        base.Init();
        isVibrationEnabled = DataHelper.Vibration;
        DataHelper.RegisterDataChangedCallback(DataKey.VIBRATION, OnVibrationChanged);
    }
}
