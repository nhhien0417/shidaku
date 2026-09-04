using System;
using System.Collections.Generic;

namespace UserDataPack.Structures.CollectionData
{
    [Serializable]
    public class SingleIdCollectionData<T> where T : IConvertible 
    {
        public List<T> OwnedIds = new();
        
        public bool HasOwned(T id) => OwnedIds.Contains(id);

        public bool AddOwned(T id)
        {
            if (HasOwned(id))
                return false;
            
            OwnedIds.Add(id);
            return true;
        }
    }
}