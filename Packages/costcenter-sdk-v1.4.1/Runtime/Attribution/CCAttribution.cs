using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Firebase.Extensions;
#if UNITY_IOS && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

namespace CostCenter.Attribution
{
    public class CCAttribution : MonoBehaviour
    {
        public static CCAttribution instance;

        [SerializeField] private bool _autoTracking = true;
        
        void Awake() {
            instance = this;
            if (_autoTracking) {
                CCFirebase.OnFirebaseInitialized += () => {
                    TrackingAttribution(null);
                };
            }
        }
        
        private CCMMP initCurrentMMP()
        {
            
            string[] adapterNameArray = {
                "CostCenter.Attribution.CCAdjustAdapter",
                "CostCenter.Attribution.CCAppsflyerAdapter",
                "CostCenter.Attribution.CCSingularAdapter",
                "CostCenter.Attribution.CCMMP"
            };
            

            System.Type GetType(string typeName)
            {
                var type = System.Type.GetType(typeName);
                if (type != null) return type;

                foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
                {
                    // Debug.Log("FullName:" + a.GetName());
                    // if(a.FullName == "LihuhuGameEvent")
                    {
                        type = a.GetType(typeName);
                        if (type != null)
                        {
                            // Debug.Log("FullName 0:" + a.GetName());
                            return type;
                        }

                    }

                }
                return null;
            }

            foreach (string adapterName in adapterNameArray)
            {
                // Debug.Log(" CCAttribution InitWithAdapter 0 " + adapterName);
                System.Type adapterType = GetType(adapterName);
                if (adapterType == null)
                {
                    continue;
                }

                // Debug.Log(" CCAttribution InitWithAdapter 2 ");
                var finalAdapter = this.gameObject.AddComponent(adapterType) as CCMMP;
                // Debug.Log(" CCAttribution InitWithAdapter 3 ");
                if (finalAdapter == null)
                {
                    continue;
                }

                Debug.Log("CCSDK - CCAttribution InitWithAdapter init successfully with " + adapterName);
                return finalAdapter;
            }

            return null;
        }

        public void TrackingAttribution(string firebaseAppInstanceId) {
            if (CCConstant.IsFirstOpen) {
                StartCoroutine(CCTracking.AppOpen(
                    firebaseAppInstanceId: firebaseAppInstanceId
                ));
            }
            
            if (!CCTracking.IsTrackedMMP) {
                var adapter = initCurrentMMP();
                if (adapter != null)
                {
                    adapter.CheckAndGetAttributionID((string attributionId) => OnGetAttributtionId(attributionId, firebaseAppInstanceId));
                }
            }
            
            #if UNITY_IOS && !UNITY_EDITOR
            if (!CCTracking.IsTrackedATT) {
                StartCoroutine(CCTracking.TrackATT(
                    firebaseAppInstanceId: firebaseAppInstanceId,
                    delayTime: 5.0f
                ));
            }
            #endif
        }

        private void OnGetAttributtionId(string attributionId, string firebaseAppInstanceId) {
            if (string.IsNullOrEmpty(attributionId)) {
                return;
            }
            StartCoroutine(CCTracking.TrackMMP(
                attributionId: attributionId,
                firebaseAppInstanceId: firebaseAppInstanceId,
                delayTime: 15.0f
            ));
        }

        #if UNITY_IOS && !UNITY_EDITOR
        [DllImport ("__Internal")]
	    private static extern void _CCRequestTrackingAuthorization();
        #endif
        public static void RequestAppTrackingTransparency() {
            #if UNITY_IOS && !UNITY_EDITOR
                _CCRequestTrackingAuthorization();
            #endif
        }
    }
}
