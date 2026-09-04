using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(HorizontalLayoutGroup))]
public class CustomHorizontalLayout : MonoBehaviour
{
    [SerializeField] private int _maxItems = 3;

    private bool _isReserved;

    public int MaxItems => _maxItems;
    public int ItemsCount => transform.childCount;
    public bool IsFull => _isReserved || ItemsCount >= MaxItems;

    public void ReserveRow()
    {
        _isReserved = true;
    }

    public bool AddItem<T>(T item) where T : Component
    {
        if (IsFull)
            return false;

        item.transform.SetParent(transform, false);
        return true;
    }

    public bool RemoveItem<T>(T item) where T : Component
    {
        if (item.transform.parent != transform)
            return false;

        item.transform.SetParent(null, false);
        return true;
    }

    public bool HasActiveItem()
    {
        for (var i = 0; i < transform.childCount; i++)
        {
            if (transform.GetChild(i).gameObject.activeSelf)
                return true;
        }

        return false;
    }
}
