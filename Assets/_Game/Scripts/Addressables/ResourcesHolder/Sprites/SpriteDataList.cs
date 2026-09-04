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
    public class SpriteDataList : MonoBehaviour
    {
        [SerializeField] protected List<AssetReferenceT<Sprite>> Sprites = new ();

        protected Dictionary<int, AsyncOperationHandle<Sprite>> _loadedHandles = new ();

        public int TotalSprites => Sprites.Count;

        public void GetSprite(int index, Action<Sprite> onLoaded)
        {
            if (index < 0 || index >= Sprites.Count)
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

            var spriteRef = Sprites[index];
            if (spriteRef != null)
            {
                handle = spriteRef.LoadAssetAsync();
                _loadedHandles[index] = handle;
                handle.Completed += h =>
                {
                    if (h.Status == AsyncOperationStatus.Succeeded)
                    {
                        var loadedSprite = h.Result;
                        onLoaded?.Invoke(loadedSprite);
                    }
                    else
                    {
                        Debug.LogError($"Failed to load sprite: {index}");
                        onLoaded?.Invoke(null);
                    }
                };
            }
            else
            {
                onLoaded?.Invoke(null);
            }
        }

        public Task<Sprite> GetSpriteAsync(int index)
        {
            var tcs = new TaskCompletionSource<Sprite>();

            GetSprite(index, sprite =>
            {
                tcs.SetResult(sprite);
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
    }
}