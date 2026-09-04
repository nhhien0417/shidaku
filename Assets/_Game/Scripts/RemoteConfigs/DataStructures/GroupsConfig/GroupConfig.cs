using System;
using System.Collections.Generic;
using UnityEngine;

namespace RemoteConfigs
{
    /// <summary>
    /// A generic class that represents a group configuration with three type parameters.
    /// </summary>
    /// <typeparam name="T1">The type of the group data.</typeparam>
    /// <typeparam name="T2">The type of the value to be compared.</typeparam>
    /// <typeparam name="T3">The type of group id (usually an integer or string).</typeparam>
    [Serializable]
    public class GroupConfig<T1, T2, T3> where T1 : IGroupData<T2, T3> where T2 : IComparable
    {
        [SerializeReference] public List<T1> Groups = new();

        public virtual void ApplyRemoteConfig(string config)
        {

        }

        public virtual T3 GetGroupId(T2 value)
        {
            if (Groups.Count == 0)
                return default;

            foreach (var t in Groups)
            {
                if (t.IsMatch(value))
                    return t.GetGroupId();
            }

            return default;
        }
    }

    [Serializable]
    public abstract class IGroupData<T1,T2> where T1 : IComparable
    {
        public abstract bool IsMatch(T1 value);
        public abstract T2 GetGroupId();
    }

    [Serializable]
    public class GroupDataMinMax<T1, T2> : IGroupData<T1, T2> where T1 : IComparable
    {
        public T1 Min;
        public T1 Max;
        public T2 GroupId;

        public override bool IsMatch(T1 value)
        {
            return value.CompareTo(Min) >= 0 && value.CompareTo(Max) <= 0;
        }

        public override T2 GetGroupId()
        {
            return GroupId;
        }
    }
}