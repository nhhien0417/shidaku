using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace StorageUtils
{
    public interface IHasId
    {
        public string GetId();
    }
    
    public interface IHasSortOrder
    {
        public int GetSortOrder();
    }
    
    public interface ICustomSerializableData
    {
        public void OnPreSerialize();
        public void OnPostDeserialize();
    }
    
    public interface IHasWeight
    {
        public float GetWeight();
    }
}