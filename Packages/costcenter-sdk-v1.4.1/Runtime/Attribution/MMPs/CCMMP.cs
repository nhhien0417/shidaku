using System;
using System.Collections.Generic;
using UnityEngine;

namespace CostCenter.Attribution 
{
    public class CCMMP : MonoBehaviour
    {
        // Start is called before the first frame update
        // void Start()
        // {
        //
        // }

        protected Action<string> onGetAttributionID;
        

        public virtual void CheckAndGetAttributionID(Action<string> callback)
        {
            onGetAttributionID = callback;
        }
    }
}

