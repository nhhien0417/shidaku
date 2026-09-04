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
    public class SpriteData : MonoBehaviour
    {
        [SerializeField] protected List<Data<AssetReferenceT<Sprite>>> Sprites = new ();

        protected Dictionary<string, AsyncOperationHandle<Sprite>> _loadedHandles = new ();

        public virtual void GetSprite(string id, Action<Sprite> onLoaded)
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

            var spriteRef = Sprites.Find(t => t.Key == id);
            if (spriteRef != null)
            {
                handle = spriteRef.Value.LoadAssetAsync();
                _loadedHandles[id] = handle;
                handle.Completed += h =>
                {
                    if (h.Status == AsyncOperationStatus.Succeeded)
                    {
                        var loadedSprite = h.Result;
                        onLoaded?.Invoke(loadedSprite);
                    }
                    else
                    {
                        Debug.LogError($"Failed to load sprite: {id}");
                        onLoaded?.Invoke(null);
                    }
                };
            }
            else
            {
                onLoaded?.Invoke(null);
            }
        }

        public Task<Sprite> GetSpriteAsync(string id)
        {
            var tcs = new TaskCompletionSource<Sprite>();

            GetSprite(id, sprite =>
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
