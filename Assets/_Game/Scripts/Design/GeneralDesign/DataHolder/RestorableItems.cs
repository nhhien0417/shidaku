using System;
using System.Collections.Generic;
using UnityEngine;
using Design.Structures;

namespace Design.DataHolder
{
    [CreateAssetMenu(fileName = "RestorableItems", menuName = "Design/RestorableItems")]
    [Serializable]
    public class RestorableItems : ScriptableObject
    {
        [SerializeField] private List<RestorableItem> _restorableItems = new ();
        
        public RestorableItem Get(string id)
        {
            return _restorableItems?.Find(x => x.Id == id);
        }
    }
}
