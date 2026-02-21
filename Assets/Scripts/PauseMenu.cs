using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using TMPro;

public class PauseMenu : MonoBehaviour
{
    private GameObject pausePanel;

    void Start()
    {
        EnsureEventSystem();
        BuildUI();
        pausePanel.SetActive(false);
    }

    void Update()
    {
        bool escPressed = Keyboard.current != null
            && Keyboard.current.escapeKey.wasPressedThisFrame;
        bool startPressed = Gamepad.current != null
            && Gamepad.current.startButton.wasPressedThisFrame;

        if (escPressed || startPressed)
            Toggle();
    }

    void Toggle()
    {
        if (GameManager.IsPaused)
            Resume();
        else
            Pause();
    }

    void Pause()
    {
        // Don't pause if player is dead
        var player = FindAnyObjectByType<PlayerScript>();
        if (player != null)
        {
            var health = player.GetComponent<Health>();
            if (health != null && health.isDead) return;
        }

        pausePanel.SetActive(true);
        GameManager.Instance.PauseGame();
    }

    public void Resume()
    {
        pausePanel.SetActive(false);
        GameManager.Instance.ResumeGame();
    }

    static void EnsureEventSystem()
    {
        if (FindAnyObjectByType<EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<InputSystemUIInputModule>();
        }
    }

    void BuildUI()
    {
        // Canvas
        GameObject canvasObj = new GameObject("PauseMenuCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;
        var scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasObj.AddComponent<GraphicRaycaster>();

        // Panel
        pausePanel = new GameObject("PausePanel");
        pausePanel.transform.SetParent(canvasObj.transform, false);
        RectTransform panelRect = pausePanel.AddComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        // Dark overlay
        Image overlay = pausePanel.AddComponent<Image>();
        overlay.color = new Color(0, 0, 0, 0.7f);

        // Title
        CreateText(pausePanel.transform, "PAUSED", 60, new Vector2(0, 120));

        // Buttons
        CreateButton(pausePanel.transform, "Resume", new Vector2(0, 20), Resume);
        CreateButton(pausePanel.transform, "Restart", new Vector2(0, -50),
            () => GameManager.Instance.RestartLevel());
        CreateButton(pausePanel.transform, "Main Menu", new Vector2(0, -120),
            () => GameManager.Instance.GoToMainMenu());
        CreateButton(pausePanel.transform, "Quit", new Vector2(0, -190),
            () => GameManager.Instance.QuitGame());
    }

    static void CreateText(Transform parent, string text, float fontSize, Vector2 pos)
    {
        GameObject obj = new GameObject("Text_" + text);
        obj.transform.SetParent(parent, false);
        TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;

        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(600, fontSize + 20);
    }

    static Button CreateButton(Transform parent, string label, Vector2 pos,
        UnityEngine.Events.UnityAction onClick)
    {
        GameObject btnObj = new GameObject("Button_" + label);
        btnObj.transform.SetParent(parent, false);

        Image btnImage = btnObj.AddComponent<Image>();
        btnImage.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);

        Button btn = btnObj.AddComponent<Button>();
        ColorBlock colors = btn.colors;
        colors.normalColor = new Color(0.2f, 0.2f, 0.2f, 0.9f);
        colors.highlightedColor = new Color(0.35f, 0.35f, 0.35f, 1f);
        colors.pressedColor = new Color(0.1f, 0.1f, 0.1f, 1f);
        colors.selectedColor = new Color(0.35f, 0.35f, 0.35f, 1f);
        btn.colors = colors;
        btn.onClick.AddListener(onClick);

        RectTransform btnRect = btnObj.GetComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(0.5f, 0.5f);
        btnRect.anchorMax = new Vector2(0.5f, 0.5f);
        btnRect.anchoredPosition = pos;
        btnRect.sizeDelta = new Vector2(300, 50);

        // Label text
        GameObject labelObj = new GameObject("Label");
        labelObj.transform.SetParent(btnObj.transform, false);
        TextMeshProUGUI tmp = labelObj.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 28;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;

        RectTransform labelRect = labelObj.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        return btn;
    }
}
