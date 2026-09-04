using UnityEngine;

namespace Titipi.MocaLib.Runtime.Services.Internal
{
    [CreateAssetMenu(fileName = "MMPConfig", menuName = "MocaLib/MMPConfig")]
    public class MMPConfig : ScriptableObject
    {
        public bool EnableDebug;
        public bool EnableSandboxTest;
        
        [Header("Fill these settings if the game uses Adjust")]
        [Space(5)]
        public string AppTokenIOS;
        public string AppTokenAndroid;

        [Space(12)]

        [Header("Fill these settings if the game uses AppsFlyer")]
        [Space(5)]
        public string DevKey;
        public string AppIdIOS;
        public string GooglePublicKey;
        public AppsflyerEventListener AppsflyerEventListener;
    }
}
