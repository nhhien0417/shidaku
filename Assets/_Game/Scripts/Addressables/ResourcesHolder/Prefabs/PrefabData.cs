using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Design.Structures;
using UnityEngine.AddressableAssets;
using Design.Ids;
using Sirenix.OdinInspector;
using UnityEngine.ResourceManagement.AsyncOperations;
using System.Threading.Tasks;

namespace AssetsHolder
{
    [Serializable]
    public class PrefabData : MonoBehaviour
    {
        [SerializeField] protected List<Data<AssetReferenceT<GameObject>>> Prefabs = new ();

        protected Dictionary<string, AsyncOperationHandle<GameObject>> _loadedHandles = new ();

        public void GetPrefab(string id, Action<GameObject> onLoaded)
        {
            if (_loadedHandles.TryGetValue(id, out var handle))
            {
                if (handle.IsDone && handle.Status != AsyncOperationStatus.Succeeded)
                {
                    try
                    {
                        _loadedHandles.Remove(id);
                        Addressables.Release(handle);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Error releasing handle for item {id}: {e.Message}");
                    }
                }
                else
                {
                    if (handle.IsDone)
                    {
                        onLoaded?.Invoke(handle.Result);
                    }
                    else
                    {
                        handle.Completed += h =>
                        {
                            if (h.Status == AsyncOperationStatus.Succeeded)
                            {
                                onLoaded?.Invoke(h.Result);
                            }
                            else
                            {
                                Debug.LogError($"Failed to load sprite: {id}");
                                onLoaded?.Invoke(null);
                            }
                        };
                    }
                    return;
                }
            }

            var prefabRef = Prefabs.Find(t => t.Key == id);
            if (prefabRef != null)
            {
                handle = prefabRef.Value.LoadAssetAsync();
                _loadedHandles[id] = handle;
                handle.Completed += h =>
                {
                    if (h.Status == AsyncOperationStatus.Succeeded)
                    {
                        var loadedPrefab = h.Result;
                        onLoaded?.Invoke(loadedPrefab);
                    }
                    else
                    {
                        Debug.LogError($"Failed to load prefab: {id}");
                        onLoaded?.Invoke(null);
                    }
                };
            }
            else
            {
                onLoaded?.Invoke(null);
            }
        }

        public Task<GameObject> GetPrefabAsync(string id)
        {
            var tcs = new TaskCompletionSource<GameObject>();

            GetPrefab(id, prefab =>
            {
                tcs.SetResult(prefab);
            });

            return tcs.Task;
        }

        public void ClearCache()
        {
            foreach (var handle in _loadedHandles.Values)
            {
                if (handle.IsValid())
                {
                    Addressables.Release(handle);
                }
            }
            _loadedHandles.Clear();
        }

        private void OnDestroy()
        {
            foreach (var handle in _loadedHandles.Values)
            {
                if (handle.IsValid())
                {
                    Addressables.Release(handle);
                }
            }
            _loadedHandles.Clear();
        }

        #if UNITY_EDITOR
        protected virtual string[] GetAllIds()
        {
            return null;
        }
        #endif

        [Serializable]
        protected class Data<T>
        {
            [ValueDropdown("@$root.GetAllIds()")]
            public string Key;
            public T Value;
        }
    }
}
