// #if !UNITY_EDITOR
// using UnityEngine;
//
// public static class DisableUnityLogger
// {
//     [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
//     private static void DisableLogging()
//     {
//         if (!Debug.isDebugBuild)
//             Debug.unityLogger.logEnabled = false;
//     }
// }
// #endif