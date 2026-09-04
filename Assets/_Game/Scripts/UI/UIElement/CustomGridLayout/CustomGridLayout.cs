using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(VerticalLayoutGroup))]
public class CustomGridLayout : MonoBehaviour
{
    [SerializeField] private CustomHorizontalLayout _horizontalLayoutPrefab;
    [SerializeField] private List<CustomHorizontalLayout> _predefinedLayouts = new ();

    [SerializeField, ReadOnly] private List<CustomHorizontalLayout> _layouts = new ();

    private bool _isInited = false;

    public void Add(Transform item, bool reserveWholeRow = false)
    {
        if (!_isInited)
            Initialize();

        var layout = reserveWholeRow ? GetEmptyLayout() : GetAvailableLayout();
        layout.AddItem(item);
        if (reserveWholeRow)
            layout.ReserveRow();
    }

    public void RefreshRowsVisibility()
    {
        foreach (var layout in _layouts)
            layout.gameObject.SetActive(layout.HasActiveItem());
    }

    public bool Remove(Transform item)
    {
        var layout = item.transform.parent?.GetComponent<CustomHorizontalLayout>();
        if (layout == null || !_layouts.Exists(l => l == layout))
        {
            this.LogError($"Item {item} is not belonging to this layout");
            return false;
        }

        layout.RemoveItem(item);
        return true;
    }

    private CustomHorizontalLayout GetAvailableLayout()
    {
        if (_layouts.Count == 0 || _layouts[^1].IsFull)
            return CreateLayout();

        return _layouts[^1];
    }

    private CustomHorizontalLayout GetEmptyLayout()
    {
        if (_layouts.Count == 0 || _layouts[^1].ItemsCount > 0 || _layouts[^1].IsFull)
            return CreateLayout();

        return _layouts[^1];
    }

    private CustomHorizontalLayout CreateLayout()
    {
        var newLayout = Instantiate(_horizontalLayoutPrefab, transform);
        _layouts.Add(newLayout);
        return newLayout;
    }

    private void Initialize()
    {
        if (_isInited)
            return;

        foreach (var layout in _predefinedLayouts)
        {
            if (layout.transform.parent != transform)
            {
                this.LogError($"Predefined layout {layout} is not a child of {this}");
                continue;
            }

            _layouts.Add(layout);
        }

        _isInited = true;
    }

    private void Awake()
    {
        Initialize();
    }
}
