using System;
using System.Collections.Generic;
using UserDataPack.Structures.CollectionData;

namespace Design.Structures
{
    public static class CollectionItemUtils
    {
        public static List<SingleIdCollectionItem<T>> ToSingleIdCollectionItemList<T>(this SingleIdCollectionData<T> data, string collectionType) where T : IConvertible
        {
            var items = new List<SingleIdCollectionItem<T>>();
            foreach (var id in data.OwnedIds)
            {
                items.Add(new SingleIdCollectionItem<T>
                {
                    CollectionType = collectionType,
                    CollectionId = id,
                });
            }
            items.Sort((a, b) => Comparer<T>.Default.Compare(a.CollectionId, b.CollectionId));
            return items;
        }
    }
}