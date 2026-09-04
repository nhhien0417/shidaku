// This class is the Entry Point of the game
// It is attached to the LoadingController game object in the Startup prefab (hierarchy: Startup->Loading->LoadingController)
// Once the Loading process is done, it will load the second scene (id=1 in the scene list) - this usually is the Home (Main Menu) scene

// HOW TO USE:
// Create a `LoadingController.cs` file in your project's `Scripts` folder
// And make its class inherited from this `BaseLoadingController` class
// Implement the `protected virtual IEnumerator StartLoadingScreen()` method to initialize things with steps
// If needed, you can also override other virtual methods to customize the loading mechanism
// You can use the content of the StartLoadingScreen() method in this BaseLoadingController class as a starting point
// Attach your class to the `LoadingController` game object (and uncheck the default BaseLoadingController script)


using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

#if !UNITY_EDITOR
using DG.Tweening;
#endif

using Titipi.MocaLib.Runtime.Common;

namespace Titipi.MocaLib.Runtime.Startup
{
    public class BaseLoadingController : MonoBehaviour
    {
        [SerializeField] protected GameObject _splashScreen;
        [SerializeField] protected GameObject _loadingScreen;

        [SerializeField] protected float _maxLoadingTime = 7f;
        [SerializeField] protected Slider _loadingBar;
        [SerializeField] protected Text _versionText;

        protected static float _loadingTime;
        protected static bool _loadingDone;

        protected virtual void Start()
        {
            Application.targetFrameRate = 60;

#if UNITY_EDITOR
            _maxLoadingTime = 3f;
#endif

            StartSplashScreen();
        }

        protected virtual void UpdateLoadingBarProgress(float loadingTime)
        {
            if (loadingTime > _maxLoadingTime) loadingTime = _maxLoadingTime;
            var progress = loadingTime / _maxLoadingTime;
            _loadingBar.value = progress;
        }

        protected virtual void StartSplashScreen()
        {
#if UNITY_EDITOR
            _splashScreen.SetActive(false);
            _loadingScreen.SetActive(true);
            StartCoroutine(nameof(StartLoadingScreen));
#else
            _loadingScreen.SetActive(false);
            _splashScreen.SetActive(true);

            DOVirtual.DelayedCall(2.5f, () =>
            {
                _splashScreen.gameObject.GetComponent<CanvasGroup>().DOFade(0, 1f).OnComplete(() =>
                {
                    _splashScreen.SetActive(false);
                    _loadingScreen.SetActive(true);

                    StartCoroutine(nameof(StartLoadingScreen));
                });
            });
#endif
        }

        protected virtual IEnumerator StartLoadingScreen()
        {
            AsyncOperation op = SceneManager.LoadSceneAsync(1);
            op.allowSceneActivation = false;

            bool isAppOpenAdShown = false;
            int step = 0;

            while (!_loadingDone)
            {
                _loadingTime += Time.deltaTime;

                switch (step)
                {
                    // Allow some "warm-up"
                    case 0:
                    case 1:
                    case 2:
                        step++;
                        break;

                    case 3:
#if UNITY_ANDROID
                        _versionText.text = $"Version: {GameVersionInfo.BUILD_VERSION}";
#elif UNITY_IOS
                        _versionText.text = $"Version: {GameVersionInfo.BUILD_VERSION} ({GameVersionInfo.BUILD_NUMBER})";
#endif

                        step++;
                        break;

                    case 4:
                        Services.MocaLib.Instance.Initialize();

                        step++;
                        break;

                    case 5:
                        if (_loadingTime >= _maxLoadingTime)
                        {
                            _loadingTime = _maxLoadingTime;
                            _loadingDone = true;
                        }

                        break;
                }

                UpdateLoadingBarProgress(_loadingTime);

                yield return new WaitForEndOfFrame();
            }

            op.allowSceneActivation = true;
        }
    }
}
