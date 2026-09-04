using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Sirenix.OdinInspector;
using UnityEngine.ResourceManagement.AsyncOperations;
using System.Threading.Tasks;
using Spine;
using Spine.Unity;

namespace AssetsHolder
{
    [Serializable]
    public class SkeletonDataList : MonoBehaviour
    {
        [SerializeField] protected List<AssetReferenceT<SkeletonDataAsset>> SkeletonDatas = new ();

        protected Dictionary<int, AsyncOperationHandle<SkeletonDataAsset>> _loadedHandles = new ();

        public int TotalSkeletonDatas => SkeletonDatas.Count;

        public void GetSkeletonData(int index, Action<SkeletonDataAsset> onLoaded)
        {
            if (index < 0 || index >= SkeletonDatas.Count)
            {
                Debug.LogError($"Invalid index: {index}");
                onLoaded?.Invoke(null);
                return;
            }

            if (_loadedHandles.TryGetValue(index, out var handle))
            {
                if (handle.IsDone && handle.Status != AsyncOperationStatus.Succeeded)
                {
                    try
                    {
                        _loadedHandles.Remove(index);
                        Addressables.Release(handle);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Error releasing handle for item index {index}: {e.Message}");
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
                                Debug.LogError($"Failed to load sprite: {index}");
                                onLoaded?.Invoke(null);
                            }
                        };
                    }
                    return;
                }
            }

            var skeletonDataRef = SkeletonDatas[index];
            if (skeletonDataRef != null)
            {
                handle = skeletonDataRef.LoadAssetAsync();
                _loadedHandles[index] = handle;
                handle.Completed += h =>
                {
                    if (h.Status == AsyncOperationStatus.Succeeded)
                    {
                        var loadedSkeletonData = h.Result;
                        onLoaded?.Invoke(loadedSkeletonData);
                    }
                    else
                    {
                        Debug.LogError($"Failed to load skeleton data: {index}");
                        onLoaded?.Invoke(null);
                    }
                };
            }
            else
            {
                onLoaded?.Invoke(null);
            }
        }

        public Task<SkeletonDataAsset> GetSkeletonDataAsync(int index)
        {
            var tcs = new TaskCompletionSource<SkeletonDataAsset>();

            GetSkeletonData(index, tcs.SetResult);

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
    }
}
