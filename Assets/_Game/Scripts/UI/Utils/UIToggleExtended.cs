using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Toggle))]
public class UIToggleExtended : MonoBehaviour
{
    [SerializeField] private List<GameObject> _objsHideIfToggleEnabled = new ();
    [SerializeField] private List<GameObject> _objsShowIfToggleEnabled = new ();

    private void OnToggleChanged(bool isOn)
    {
        foreach (var obj in _objsHideIfToggleEnabled)
        {
            obj.SetActive(!isOn);
        }

        foreach (var obj in _objsShowIfToggleEnabled)
        {
            obj.SetActive(isOn);
        }
    }

    private void Awake()
    {
        var toggle = GetComponent<Toggle>();
        if (toggle == null)
        {
            Debug.LogError("UIToggleExtended requires a Toggle component on the same GameObject.");
            return;
        }

        OnToggleChanged(toggle.isOn);
        toggle.onValueChanged.AddListener(OnToggleChanged);
    }
}
