using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindAnyObjectByType<GameManager>();
                if (_instance == null)
                {
                    var go = new GameObject("GameManager");
                    _instance = go.AddComponent<GameManager>();
                    DontDestroyOnLoad(go);
                }
            }
            return _instance;
        }
    }

    public static bool IsPaused { get; private set; }

    [Header("Scenes")]
    public string mainMenuScene = "MainMenu";
    public string firstLevelScene = "Room-1";
    public string bossRoomScene = "Final-room";

    [HideInInspector]
    public bool bossChaseActive;

    private static GameManager _instance;

    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void PauseGame()
    {
        IsPaused = true;
        Time.timeScale = 0f;
    }

    public void ResumeGame()
    {
        IsPaused = false;
        Time.timeScale = 1f;
    }

    public void RestartLevel()
    {
        IsPaused = false;
        Time.timeScale = 1f;

        string targetScene;
        if (bossChaseActive)
            targetScene = bossRoomScene;
        else
            targetScene = SceneManager.GetActiveScene().name;

        bossChaseActive = false;
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayRoomChangeSFX();
        SceneManager.LoadScene(targetScene);
    }

    public void GoToMainMenu()
    {
        IsPaused = false;
        Time.timeScale = 1f;
        bossChaseActive = false;
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayRoomChangeSFX();
        SceneManager.LoadScene(mainMenuScene);
    }

    public void LoadScene(string sceneName)
    {
        IsPaused = false;
        Time.timeScale = 1f;
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayRoomChangeSFX();
        SceneManager.LoadScene(sceneName);
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
