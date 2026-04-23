using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class MainMenuController : MonoBehaviour
{
    [SerializeField] private string gameSceneName = "MainScene";
    [SerializeField] private string homeSceneName = "StartScene";

    public void StartGame()
    {
        LoadGameScene();
    }

    public void LoadGameScene()
    {
        SceneManager.LoadScene(gameSceneName);
    }

    public void LoadHomeScene()
    {
        SceneManager.LoadScene(homeSceneName);
    }

    public void RestartCurrentScene()
    {
        Time.timeScale = 1f;
        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.IsValid() && !string.IsNullOrEmpty(activeScene.name))
            SceneManager.LoadScene(activeScene.name);
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
