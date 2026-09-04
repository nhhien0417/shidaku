using UnityEngine;
using UnityEngine.SceneManagement;

public class InitScene : MonoBehaviour
{
    private void Start()
    {
        SceneManager.LoadSceneAsync("_Game/Scenes/Startup");
    }
}
