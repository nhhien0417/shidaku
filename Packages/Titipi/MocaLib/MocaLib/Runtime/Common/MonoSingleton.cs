//**********************************************************************************************************
// Based on // http://wiki.unity3d.com/index.php?title=Singleton#Generic_Based_Singleton_for_MonoBehaviours
//**********************************************************************************************************

using UnityEngine;

namespace Titipi.MocaLib.Runtime.Common
{
    public abstract class MonoSingleton<T> : MonoBehaviour where T : MonoSingleton<T>
    {
        private static T _instance;
        private static object _lockObject = new();

        public static T Instance
        {
            get
            {
                // Instance required for the first time, we look for it
                if (_instance == null)
                {
                    lock (_lockObject)
                    {
                        //Search for existing objects
                        var result = FindObjectsByType<T>(FindObjectsSortMode.None);

                        if (result.Length > 0)
                        {
                            _instance = result[0] as T;
                            if (result.Length > 1)
                                Debug.LogError($"[MonoSingleton] Something went really wrong - there should never be more than 1 {typeof(T)}!");
                        }
                        // Object not found, inform that we are trying to acquire not created instance
                        else
                        {
                            Debug.LogError($"[MonoSingleton] You are trying to get not created instance of {typeof(T)}");
                        }
                    }
                }

                return _instance;
            }
        }

        public static bool HasInstance => _instance != null;

        public static T CreateInstance(bool dontDestroyOnLoad = false)
        {
            if (_instance == null)
            {
                lock (_lockObject)
                {
                    //Search for existing objects
                    var result = FindObjectsByType<T>(FindObjectsSortMode.None);

                    if (result.Length > 0)
                    {
                        _instance = result[0] as T;
                        Debug.LogError($"[MonoSingleton]: You trying to create instance of {typeof(T)}, but it is already existing!", _instance.gameObject);

                        if (result.Length > 1)
                            Debug.LogError($"[MonoSingleton]: Something went really wrong - there should never be more than 1 {typeof(T)}!");
                    }
                    else
                    {
                        GameObject parent = new GameObject(typeof(T).Name);
                        _instance = parent.AddComponent<T>();
                        if (dontDestroyOnLoad)
                            DontDestroyOnLoad(parent);
                    }
                }
            }

            return _instance;
        }


        // This function is called when the instance is used the first time
        // Put all the initializations you need here, as you would do in Awake
        protected virtual void Init() { }


        // If no other monobehaviour request the instance in an awake function
        // executing before this one, no need to search the object.
        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this as T;
                if (_instance != null)
                {
                    DontDestroyOnLoad(_instance.gameObject);
                    _instance.Init();
                }
            }
            else if (_instance == this as T)
            {
                DontDestroyOnLoad(_instance.gameObject);
                _instance.Init();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        // Make sure the instance isn't referenced anymore when the object is destroyed, just in case.
        protected virtual void OnDestroy()
        {
            if (_instance == this as T)
            {
                _instance = null;
            }
        }

        // Make sure the instance isn't referenced anymore when the user quit, just in case.
        private void OnApplicationQuit()
        {
            _instance = null;
        }
    }
}
