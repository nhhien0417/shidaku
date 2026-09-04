using System;
using System.Collections.Generic;

namespace UserDataPack.Structures
{
    [Serializable]
    public class KeyValue<T> where T : IConvertible 
    {
        public string Key;
        public T Value;
    }

    [Serializable]
    public class KeyValues<T> where T : IConvertible
    {
        public string Key;
        public List<T> Values;
    }
}
