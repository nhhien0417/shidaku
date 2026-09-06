using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NavigationController : SingletonComponent<NavigationController>
{
    [SerializeField] private SnapLoading snapLoading;

    public void LoadToMainMenu(bool useAnim = true)
    {
        LoadScene(Key.MAIN_MENU_SCENE, null, useAnim);
    }

    public void LoadToGameplay(Action onComplete = null)
    {
        LoadScene(Key.GAMEPLAY_SCENE, onComplete, true);
    }

    public void LoadCurrentLevel(Action onComplete = null, bool useAnim = true)
    {
        LoadLevel(DataHelper.PlayingLevel, onComplete, useAnim);
    }

    public void LoadLevel(int level, Action onComplete = null, bool useAnim = true)
    {
        if (!Validator.ExistsLevel(level))
        {
            Debug.LogError($"Level {level} is not exists!");
            return;
        }

        DataHelper.PlayingLevel = level;
        string levelPath = Key.LEVEL_PATH + level;
        LoadSceneAsync(levelPath, LoadSceneMode.Single, useAnim, onComplete);
    }

    public void LoadScene(string scene, Action onComplete = null, bool useAnim = true)
    {
        if (AdsManager.ShowInterstitialOnChangeScene)
        {
            var sceneName = string.IsNullOrEmpty(scene) ? "" : (scene[(scene.LastIndexOf("/", StringComparison.Ordinal) + 1)..]).ToLower();
            AdsManager.ShowInterstitialAd($"go_to_{sceneName}", () =>
            {
                LoadSceneAsync(scene, LoadSceneMode.Single, useAnim, onComplete);
            });
        }
        else
        {
            LoadSceneAsync(scene, LoadSceneMode.Single, useAnim, onComplete);
        }
    }

    private void LoadSceneAsync(string scene, LoadSceneMode loadMode = LoadSceneMode.Single, bool useAnim = true, Action onComplete = null)
    {
        if (useAnim)
            AudioManager.Instance.PlaySFXOneShot("transition");
        snapLoading.Show(scene, useAnim, () =>
        {
            StopCoroutine("LoadSceneAsync");
            StartCoroutine(LoadSceneAsync(scene, loadMode, () =>
            {
                snapLoading.Hide();
                onComplete?.Invoke();
            }));
        });
    }

    private IEnumerator LoadSceneAsync(string scene, LoadSceneMode loadMode, System.Action onComplete = null)
    {
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(scene, loadMode);

        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        onComplete?.Invoke();
    }
}
