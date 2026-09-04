using System;
using System.Collections.Generic;
using UnityEngine;

namespace Design.Structures
{
    [Serializable]
    public class RandomEntry
    {
        public List<Item> Items = new();

        [Range(0f, 100f)]
        public float Weight = 100f;
    }

    [Serializable]
    public abstract class RandomItemDefined
    {
        [SerializeField] protected string Id;

        public string GetId() => Id;
        public abstract List<Item> GetRandomItems();

        protected List<Item> PickWeighted(List<RandomEntry> entries)
        {
            if (entries == null || entries.Count == 0)
                return new List<Item>();

            var totalWeight = 0f;
            foreach (var entry in entries)
                totalWeight += entry.Weight;

            if (totalWeight <= 0f)
                return new List<Item>();

            var roll = UnityEngine.Random.Range(0f, totalWeight);
            var cumulative = 0f;

            foreach (var entry in entries)
            {
                cumulative += entry.Weight;
                if (roll < cumulative)
                    return new List<Item>(entry.Items);
            }

            return new List<Item>(entries[^1].Items);
        }
    }
}