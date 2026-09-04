using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ToggleOnUIGroupShow : MonoBehaviour
{
    [SerializeField] private List<string> _uiGroupNames = new ();
    [SerializeField] private List<string> _sceneNames = new ();
    [SerializeField] private UIGroupTransition _transition;

    private void OnUIGroupShown(UIGroup group)
    {
        HandleUIGroupShowHide(group, false);
    }

    private void OnUIGroupHided(UIGroup group)
    {
        HandleUIGroupShowHide(group, true);
    }

    private void HandleUIGroupShowHide(UIGroup group, bool isHided)
    {
        var topUI = UIManager.Instance.TopUIGroup;
        var topUIName = topUI != null ? topUI.GetType().Name : string.Empty;

        if (string.IsNullOrEmpty(topUIName))
        {
            var currentActiveSceneName = SceneManager.GetActiveScene().name;
            SetActive(_sceneNames.Contains(currentActiveSceneName));
        }
        else if (_uiGroupNames.Contains(topUIName))
        {
            SetActive(true, isHided);
        }
        else
        {
            SetActive(false, !isHided);
        }
    }

    private void SetActive(bool active, bool immediate = false)
    {
        if (_transition != null)
        {
            if (active)
            {
                if (_transition.TransitionPhase == UITransitionPhase.Showing)
                    return;
                else if (_transition.TransitionPhase == UITransitionPhase.Complete && gameObject.activeSelf)
                    return;

                _transition.CompleteImmediately();
                gameObject.SetActive(true);
                _transition.Show(null, immediate);
            }
            else
            {
                if (_transition.TransitionPhase == UITransitionPhase.Hiding)
                    return;
                else if (_transition.TransitionPhase == UITransitionPhase.Complete && !gameObject.activeSelf)
                        return;
                
                _transition.CompleteImmediately();
                _transition.Hide(() => gameObject.SetActive(false), immediate);
            }
        }
        else
        {
            gameObject.SetActive(active);
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (UIManager.Instance.TopUIGroup == null)
        {
            SetActive(_sceneNames.Contains(scene.name));
        }
    }

    private void Start()
    {
        UIManager.Instance?.RegisterOnUIGroupShowed(OnUIGroupShown);
        UIManager.Instance?.RegisterOnUIGroupHided(OnUIGroupHided);

        if (_sceneNames.Count > 0)
            SceneManager.sceneLoaded += OnSceneLoaded;

        gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        UIManager.Instance?.UnRegisterOnUIGroupShowed(OnUIGroupShown);
        UIManager.Instance?.UnRegisterOnUIGroupHided(OnUIGroupHided);
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}
