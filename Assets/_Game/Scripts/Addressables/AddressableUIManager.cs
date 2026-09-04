using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class AddressableUIManager: UIManager
{
    [SerializeField] protected Transform _uiContainer;
    [SerializeField] protected float _delayBeforeShowLoading = 0.5f;
    [Tooltip("Actually, this is the address/name in Addressables Groups's settings of the folder containing all UIGroup prefabs")]
    [SerializeField] protected string _pathToUIGroups = "Assets/_Game/Prefabs/UI/UIGroups/";

    [SerializeField, ReadOnly] private HashSet<string> _uiGroupsOnLoading = new ();

    public override async UniTask<UIGroup> FindGroup(Type type)
    {
        var result = await base.FindGroup(type);
        if (result != null)
            return result;

        var uiName = type.Name;

        if (_uiGroupsOnLoading.Contains(uiName))
        {
            Debug.LogWarning("AddressableUIManager: UIGroup is already loading " + uiName);
            return null;
        }
        _uiGroupsOnLoading.Add(uiName);
        _uiLoading.Show(new UILoading.Data { DelayToAppear = _delayBeforeShowLoading });

        var handle = Addressables.LoadAssetAsync<GameObject>(_pathToUIGroups + uiName +".prefab"); // Not using InstantiateAsync on purpose to prevent memory leak when Destroying the instantiated object
        await handle.Task;

        if (handle.Status != AsyncOperationStatus.Succeeded)
        {
            Debug.LogError("AddressableUIManager: Failed to load UIGroup " + uiName);
            _uiLoading.Hide();
            _uiGroupsOnLoading.Remove(uiName);
            return null;
        }

        if (handle.Result?.GetComponent<UIGroup>() == null)
        {
            Debug.LogError("AddressableUIManager: Loaded prefab is not UIGroup " + uiName);
            _uiLoading.Hide();
            _uiGroupsOnLoading.Remove(uiName);
            Addressables.Release(handle);
            return null;
        }

        var objects = await InstantiateAsync(handle.Result, _uiContainer);
        var uiGroup = objects[0].GetComponent<UIGroup>();
        uiGroups.Add(uiGroup);
        uiGroup.RegisterOnHide(OnUIGroupHided);
        uiGroup.gameObject.SetActive(false);
        
        _uiLoading.Hide();
        _uiGroupsOnLoading.Remove(uiName);
        Addressables.Release(handle);
        
        return uiGroup;
    }

    protected override void Awake()
    {
        base.Awake();

        if (!_pathToUIGroups.EndsWith("/"))
            _pathToUIGroups += "/";
    }

#if UNITY_EDITOR
    [ContextMenu("Test Load UIGroup")]
    public async void TestLoadUIGroup()
    {
        Instance.ShowUIGroupOverlay<UIWin>();
    }
#endif
}