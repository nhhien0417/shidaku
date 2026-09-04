using UnityEngine;

public class StartScene : MonoBehaviour
{
    private void Start()
    {
        NavigationController.Instance.LoadToMainMenu();
    }
}