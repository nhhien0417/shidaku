using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

[RequireComponent(typeof(Button))]
public class HiddenButton : MonoBehaviour
{
    [SerializeField] private int tapsToActive = 5;
    [SerializeField] private float resetTime = 2;
    [SerializeField] private bool resetTapsAfterActive = true;
    [SerializeField] private UnityEvent buttonEvent;

    private Button button;
    private int currentTaps;
    private DateTime lastFirstTapTime = default(DateTime);

    private void Start()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(OnClick);
    }

    private void OnClick()
    {
        DateTime tapTime = DateTime.Now;
        if ((tapTime - lastFirstTapTime).TotalSeconds >= resetTime)
        {
            ResetTaps();
            lastFirstTapTime = tapTime;
        }

        currentTaps += 1;
        if (currentTaps >= tapsToActive)
        {
            buttonEvent.Invoke();

            if (resetTapsAfterActive)
            {
                currentTaps = 0;
            }
        }
    }

    private void ResetTaps()
    {
        if (currentTaps < tapsToActive || !resetTapsAfterActive)
        {
            currentTaps = 0;
        }
    }
}