using System;
using System.Collections.Generic;
using UnityEngine;

public class UITopBar : UIGroup
{
    [SerializeField] private List<UIResourceCounter> _resourceCounters = new ();

    public List<UIResourceCounter> TryShow(Data data)
    {
        if (data == null || data.SubtractValues is {Count: 0})
            return new ();

        var uiMatchs = _resourceCounters.FindAll(c => data.SubtractValues.Exists(sv => sv.Key == c.ResourceId));
        if (uiMatchs.Count == 0)
            return new ();

        Show(data);
        return uiMatchs;
    }

    public override void Show(object data = null, Action onCompleted = null)
    {
        base.Show(data, onCompleted);

        if (data is Data d)
        {
            foreach (var subtractValue in d.SubtractValues)
            {
                var resourceId = subtractValue.Key;
                var amount = subtractValue.Value;
                var counter = _resourceCounters.Find(c => c.ResourceId == resourceId);
                if (counter != null)
                {
                    counter.IncreaseValueBy(-amount);
                }
            }
        }
    }

    public class Data
    {
        public List<KeyValue<string, int>> SubtractValues = new ();
    }
}
