using System;

namespace Design.Structures
{
    [Serializable]
    public class SingleIdCollectionItem<T> : CollectionItem where T : IConvertible
    {
        public T CollectionId;
    }
}