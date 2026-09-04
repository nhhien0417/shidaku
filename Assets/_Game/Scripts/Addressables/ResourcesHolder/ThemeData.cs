using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Design.Structures;
using UnityEngine.AddressableAssets;
using Design.Ids;
using Sirenix.OdinInspector;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace AssetsHolder
{
    [Serializable]
    public class ThemeData : MonoBehaviour
    {
        [SerializeField] private List<Data<AssetReferenceT<ThemeSet>>> Themes = new ();

        private Dictionary<string, AsyncOperationHandle<ThemeSet>> _loadedHandles = new ();

        public void GetThemeSet(string themeId, Action<ThemeSet> onLoaded)
        {
            if (_loadedHandles.TryGetValue(themeId, out var handle))
            {
                if (handle.IsDone && handle.Status != AsyncOperationStatus.Succeeded)
                {
                    try
                    {
                        _loadedHandles.Remove(themeId);
                        Addressables.Release(handle);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Error releasing handle for theme {themeId}: {e.Message}");
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
                                Debug.LogError($"Failed to load theme: {themeId}");
                                onLoaded?.Invoke(null);
                            }
                        };
                    }
                    return;
                }
            }

            var themeRef = Themes.Find(t => t.Key == themeId);
            if (themeRef != null)
            {
                handle = themeRef.Value.LoadAssetAsync();
                _loadedHandles[themeId] = handle;
                handle.Completed += h =>
                {
                    if (h.Status == AsyncOperationStatus.Succeeded)
                    {
                        var loadedTheme = h.Result;
                        onLoaded?.Invoke(loadedTheme);
                    }
                    else
                    {
                        Debug.LogError($"Failed to load theme: {themeId}");
                        onLoaded?.Invoke(null);
                    }
                };
            }
            else
            {
                onLoaded?.Invoke(null);
            }
        }

        public void GetSprite(string themeId, string themeType, Action<Sprite> onLoaded)
        {
            GetThemeSet(themeId, themeSet =>
            {
                var sprite = themeSet != null ? themeSet.GetSprite(themeType) : null;
                onLoaded?.Invoke(sprite);
            });
        }

        public void GetColor(string themeId, string themeType, Action<Color> onLoaded)
        {
            GetThemeSet(themeId, themeSet =>
            {
                var color = themeSet != null ? themeSet.GetColor(themeType) : Color.white;
                onLoaded?.Invoke(color);
            });
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

        [Serializable]
        protected class Data<T>
        {
            [ValueDropdown("GetAllThemeIds")]
            public string Key;
            public T Value;

            #if UNITY_EDITOR
            protected virtual string[] GetAllThemeIds()
            {
                return ThemeId.All;
            }
            #endif
        }
    }
}
