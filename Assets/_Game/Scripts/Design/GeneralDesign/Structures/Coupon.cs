using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Design.Ids;

namespace Design.Structures
{
    [Serializable]
    public class Coupon
    {
        public string Id;
        
        [ValueDropdown("GetAllCouponTypes")]
        public string Type;
        
        public float CouponValue;
        
        #if UNITY_EDITOR
        private string[] GetAllCouponTypes()
        {
            return CouponType.All;
        }
        #endif
    }
}