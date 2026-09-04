using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using System.Threading.Tasks;

public class PersistentFontLoader : MonoBehaviour // Replace method load fonts from folder Resources - better initial loading time
{
    [SerializeField] private List<AssetReference> _fontReferences;
    [SerializeField] private bool _destroyAfterLoad = true;

    private async Task Start()
    {
        var loadTasks = new List<Task>();
        foreach (var fontRef in _fontReferences)
        {
            loadTasks.Add(LoadAndRegisterFont(fontRef));
        }

        await Task.WhenAll(loadTasks);

        Debug.Log("[FontsAutoLoader] All font preloaded.");

        if (_destroyAfterLoad)
            Destroy(gameObject);
    }

    private async Task LoadAndRegisterFont(AssetReference fontRef)
    {
        try
        {
            var font = await fontRef.LoadAssetAsync<TMP_FontAsset>().Task;
            if (font != null && !MaterialReferenceManager.TryGetFontAsset(font.hashCode, out _) && !MaterialReferenceManager.TryGetMaterial(font.materialHashCode, out _))
                MaterialReferenceManager.AddFontAsset(font);
            Debug.Log($"[FontsAutoLoader] Font preloaded: {font?.name}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[FontsAutoLoader] Font preload failed: {e.Message}");
        }
    }
}
