using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace CostCenter {
    public class CCFirebase : MonoBehaviour
    {
        public static CCFirebase instance;

        [SerializeField] private bool _autoInit = true;

        public static bool IsInitialized {
            private set;
            get;
        }
        private const float RetryInitDelayTime = 60.0f;

        // Listener
        public static System.Action OnFirebaseInitialized;

        private void Awake() {
            instance = this;
        }

        // Start is called before the first frame update
        void Start()
        {
            if (_autoInit) {
                Initialize();
            }
        }

        public void Initialize() {
            StartCoroutine(InitFirebaseApp());
        }

        private IEnumerator InitFirebaseApp() {
            while (!IsInitialized) {
                System.Threading.Tasks.Task<Firebase.DependencyStatus> task = null;
                try {
                    // Debug.Log("CCFirebase: start InitFirebaseApp");
                    task = Firebase.FirebaseApp.CheckAndFixDependenciesAsync();
                } catch(System.Exception e) {
                    Debug.LogError("CCFirebase: InitFirebaseApp failed.");
                    Debug.LogError(e);
                    OnInitialized(false);
                }
                if (task != null) {
                    yield return new WaitUntil(() => task.IsCompleted);
                    var dependencyStatus = task.Result;
                    if (dependencyStatus == Firebase.DependencyStatus.Available) {
                        // Debug.Log("CCFirebase: InitFirebaseApp success");
                        OnInitialized(true);
                    } else {
                        Debug.LogError(System.String.Format(
                        "Could not resolve all Firebase dependencies: {0}", dependencyStatus));
                        // Firebase Unity SDK is not safe to use here.
                        OnInitialized(false);
                        yield return new WaitForSeconds(RetryInitDelayTime);
                    }
                } else {
                    Debug.Log("CCFirebase: init failed. Retrying...");
                    yield return new WaitForSeconds(RetryInitDelayTime);
                }
            }
        }

        public void OnInitialized(bool success) {
            IsInitialized = success;
            if (success) {
                OnFirebaseInitialized?.Invoke();
            }
        }
    }
}
