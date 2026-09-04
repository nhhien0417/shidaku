using System.Collections.Generic;
using UnityEngine;

namespace StorageUtils
{
    public class DataHolderWithListCache<T> : DataHolder<T> where T : IHasId, IHasSortOrder
    {
        public List<T> AllDataList = new();
        
        public DataHolderWithListCache(string filePath, string folderPath) : base(filePath, folderPath) {}
        
        public override void LoadDesign()
        {
            base.LoadDesign();
            CreateCache();
        }

        public override void SetData(params T[] data)
        {
            base.SetData(data);
            CreateCache();
        }
        
        private void CreateCache()
        {
            AllDataList = new ();
            foreach (var item in AllData)
            {
                AllDataList.Add(item.Value);
            }
            
            AllDataList.Sort((a, b) => a.GetSortOrder() - b.GetSortOrder());
        }
    }
}