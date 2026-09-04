using System;

namespace Design.Structures
{
    [Serializable]
    public struct IapProductId
    {
        public string IOSId;
        public string AndroidId;

        public string GetProductId()
        {
#if UNITY_ANDROID
            return AndroidId;
#else
            return IOSId;
#endif
        }
    }
}