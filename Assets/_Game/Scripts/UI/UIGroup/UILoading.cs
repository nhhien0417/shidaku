using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UILoading : UIGroup
{
    [SerializeField] private GameObject _visual;
    [SerializeField] private TextMeshProUGUI _text;

    private Coroutine _hideAfterSecondsCoroutine;
    private Coroutine _delayToAppearCoroutine;

    public override void Show(object data = null, Action onCompleted = null)
    {
        base.Show(data, onCompleted);

        if (_text != null)
            _text.text = "";
        
        StopAllCoroutines();
        if (data is Data dataObj)
        {
            if (_text != null)
                _text.text = dataObj.Message;
            
            if (dataObj.TimeOut > 0)
            {
                _hideAfterSecondsCoroutine = StartCoroutine(HideAfterSeconds(dataObj.TimeOut));
            }

            _delayToAppearCoroutine = StartCoroutine(DelayToAppear(dataObj.DelayToAppear));
        }
        else
        {
            _visual.SetActive(true);
        }
    }

    public override void Hide()
    {
        if (_hideAfterSecondsCoroutine != null)
            StopCoroutine(_hideAfterSecondsCoroutine);

        if (_delayToAppearCoroutine != null)
            StopCoroutine(_delayToAppearCoroutine);

        base.Hide();
    }

    private IEnumerator HideAfterSeconds(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        Hide();
    }

    private IEnumerator DelayToAppear(float delay)
    {
        _visual.SetActive(false);
        yield return new WaitForSeconds(delay);
        _visual.SetActive(true);
    }

    public class Data
    {
        public float TimeOut = 0;
        public float DelayToAppear = 0;
        public string Message = "";
    }
}
