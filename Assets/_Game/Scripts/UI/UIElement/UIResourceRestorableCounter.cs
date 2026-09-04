using System;
using System.Collections.Generic;
using Design;
using Design.Ids;
using MEC;
using TMPro;
using UnityEngine;
using UserDataPack;

public class UIResourceRestorableCounter : UIResourceCounter
{
    [SerializeField] private GameObject _pnlTime;
    [SerializeField] private TextMeshProUGUI _txtTime;

    private CoroutineHandle _counterCoroutine;
    
    private IEnumerator<float> CounterCoroutine()
    {
        var restoreDesign = DesignDataHolder.Instance?.RestorableItems?.Get(_resourceId);

        if (restoreDesign == null || !DateTimeManager.IsUpToDate)
        {
            _pnlTime.SetActive(false);
            yield break;
        }
        _pnlTime.SetActive(true);
        
        var userData = UserData.Instance;
        var itemAmount = userData.GetResourceItemAmount(_resourceId);
        
        while (itemAmount < restoreDesign.StopRestoreThreshold)
        {
            var lastRestoreTime = userData.SecuredData.GetLastResourceItemRestoreTime(_resourceId);
            var now = DateTimeManager.Now;
            var timeToRestore = (long)((now - lastRestoreTime).TotalSeconds);

            if (timeToRestore >= restoreDesign.RestoreTimeInSeconds)
            {
                userData.UpdateRestorableResourceItem(_resourceId);
            }
            else
            {
                var remainingTime = restoreDesign.RestoreTimeInSeconds - timeToRestore;
                var minutes = (remainingTime / 60);
                var seconds = remainingTime % 60;
                _txtTime.text = $"{minutes:D2}:{seconds:D2}";
                
                yield return Timing.WaitForSeconds(1f);
            }

            itemAmount = userData.GetResourceItemAmount(_resourceId);
        }
        
        _pnlTime.SetActive(false);
    }
    
    protected override void OnEnable()
    {
        base.OnEnable();
        _counterCoroutine = Timing.RunCoroutine(CounterCoroutine());
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        Timing.KillCoroutines(_counterCoroutine);
    }
}
