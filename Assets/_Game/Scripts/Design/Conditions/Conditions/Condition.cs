using System;
using UnityEngine;

namespace Design.Conditions
{
    [Serializable]
    public class Condition
    {
        public virtual bool IsMet()
        {
            return true;
        }
    }
}