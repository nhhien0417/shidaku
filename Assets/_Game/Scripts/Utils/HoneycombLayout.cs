using UnityEngine;
using System.Collections.Generic;

public class HoneycombLayout : MonoBehaviour
{
    public enum Constraint { Flexible = 0, FixedColumnCount = 1, FixedRowCount = 2 }

    [SerializeField] private RectOffset padding = new();
    [SerializeField] private Vector2 spacing = new(20, 20);
    [SerializeField] private Constraint constraint = Constraint.FixedColumnCount;
    [SerializeField] private int constraintCount = 3;

    [Range(0.5f, 1f)]
    [SerializeField] private float heightRatio = 01f;

    private RectTransform rectTransform;
    private readonly List<RectTransform> children = new();

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    private void OnValidate()
    {
        UpdateLayout();
    }

    private void OnTransformChildrenChanged()
    {
        UpdateLayout();
    }

    private void OnEnable()
    {
        UpdateLayout();
    }

    public void SetConstraintCount(int count)
    {
        constraintCount = count;
        UpdateLayout();
    }

    public void UpdateLayout()
    {
        if (rectTransform == null) rectTransform = GetComponent<RectTransform>();

        children.Clear();
        for (int i = 0; i < transform.childCount; i++)
        {
            RectTransform child = transform.GetChild(i) as RectTransform;
            if (child != null && child.gameObject.activeSelf)
            {
                children.Add(child);
            }
        }

        if (children.Count == 0) return;

        Vector2 cellSize = children[0].rect.size;

        int columns = 1;
        float rectWidth = rectTransform.rect.width;

        if (constraint == Constraint.FixedColumnCount)
            columns = Mathf.Max(1, constraintCount);
        else if (constraint == Constraint.Flexible)
            columns = Mathf.Max(1, Mathf.FloorToInt((rectWidth - padding.horizontal + spacing.x) / (cellSize.x + spacing.x)));
        else
        {
            int rows = Mathf.Max(1, constraintCount);
            columns = Mathf.CeilToInt((float)(children.Count + rows / 2) / rows);
        }

        List<List<RectTransform>> rowsList = new List<List<RectTransform>>();
        int currentIndex = 0;
        int rowIndex = 0;
        while (currentIndex < children.Count)
        {
            int cap = (rowIndex % 2 == 0) ? columns : columns - 1;
            if (cap <= 0) cap = 1;
            int count = Mathf.Min(cap, children.Count - currentIndex);
            List<RectTransform> rowItems = new List<RectTransform>();
            for (int i = 0; i < count; i++) rowItems.Add(children[currentIndex++]);
            rowsList.Add(rowItems);
            rowIndex++;
        }

        float totalHeight = (rowsList.Count - 1) * (cellSize.y + spacing.y) * heightRatio + cellSize.y;
        float contentHeight = rectTransform.rect.height - padding.vertical;

        float topY = rectTransform.rect.height * (1 - rectTransform.pivot.y);
        float startY = topY - padding.top - (contentHeight - totalHeight) / 2f - cellSize.y / 2f;

        for (int r = 0; r < rowsList.Count; r++)
        {
            List<RectTransform> row = rowsList[r];
            float rowWidth = row.Count * cellSize.x + (row.Count - 1) * spacing.x;

            float leftX = -rectTransform.rect.width * rectTransform.pivot.x;
            float contentWidth = rectTransform.rect.width - padding.horizontal;
            float rowStartX = leftX + padding.left + (contentWidth - rowWidth) / 2f;

            float currentPosY = startY - r * (cellSize.y + spacing.y) * heightRatio;

            for (int i = 0; i < row.Count; i++)
            {
                float currentPosX = rowStartX + i * (cellSize.x + spacing.x) + cellSize.x / 2f;
                row[i].anchoredPosition = new Vector2(currentPosX, currentPosY);
            }
        }
    }
}
