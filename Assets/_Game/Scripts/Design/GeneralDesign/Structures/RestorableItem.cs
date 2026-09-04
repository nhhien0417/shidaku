using System;
using System.Collections.Generic;
using Design.Ids;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Design.Structures
{
    [Serializable]
    public class RestorableItem
    {
        [ValueDropdown("GetAllItems")]
        public string Id;
        public int RestoreTimeInSeconds;
        public int RestoreAmount;
        public int StopRestoreThreshold; // Amount of item user owned to stop restore
        
        #if UNITY_EDITOR
        private string[] GetAllItems()
        {
            return ItemId.All;
        }
        #endif
    }
}