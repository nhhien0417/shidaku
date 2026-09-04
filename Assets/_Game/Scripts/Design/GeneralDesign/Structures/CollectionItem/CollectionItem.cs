using System;

namespace Design.Structures
{
    [Serializable]
    public class CollectionItem : Item
    {
        public string CollectionType
        {
            get => Id;
            set => Id = value;
        }
    }
}