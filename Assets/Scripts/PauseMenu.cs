using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using TMPro;

public class PauseMenu : MonoBehaviour
{
    GameObject pausePanel;
    GameObject mainPanel;
    GameObject settingsPanel;

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

        // Always show main panel when opening pause menu
        mainPanel.SetActive(true);
        settingsPanel.SetActive(false);

        pausePanel.SetActive(true);
        GameManager.Instance.PauseGame();

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayMenuOpenSFX();
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

        // Panel (root container for everything)
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

        BuildMainPanel(pausePanel.transform);
        BuildSettingsPanel(pausePanel.transform);
    }

    void BuildMainPanel(Transform parent)
    {
        mainPanel = new GameObject("MainPanel");
        mainPanel.transform.SetParent(parent, false);
        StretchFill(mainPanel);

        // Title
        Sprite pausedSprite = LoadButtonSprite("PAUSED");
        if (pausedSprite != null)
            CreateSpriteHeader(mainPanel.transform, pausedSprite, new Vector2(0, 150));
        else
            CreateText(mainPanel.transform, "PAUSED", 60, new Vector2(0, 150));

        // Buttons
        CreateButton(mainPanel.transform, "Resume", new Vector2(0, 50), Resume);
        CreateButton(mainPanel.transform, "Restart", new Vector2(0, -20),
            () => GameManager.Instance.RestartLevel());
        CreateButton(mainPanel.transform, "Settings", new Vector2(0, -90), ShowSettings);
        CreateButton(mainPanel.transform, "Main Menu", new Vector2(0, -160),
            () => GameManager.Instance.GoToMainMenu());
        CreateButton(mainPanel.transform, "Quit", new Vector2(0, -230),
            () => GameManager.Instance.QuitGame());
    }

    void BuildSettingsPanel(Transform parent)
    {
        settingsPanel = new GameObject("SettingsPanel");
        settingsPanel.transform.SetParent(parent, false);
        StretchFill(settingsPanel);
        settingsPanel.SetActive(false);

        // Title
        Sprite settingsSprite = LoadButtonSprite("Settings");
        if (settingsSprite != null)
            CreateSpriteHeader(settingsPanel.transform, settingsSprite, new Vector2(0, 150));
        else
            CreateText(settingsPanel.transform, "Settings", 60, new Vector2(0, 150));

        // Sound sub-header
        Sprite soundSprite = LoadButtonSprite("Sound");
        if (soundSprite != null)
            CreateSpriteHeader(settingsPanel.transform, soundSprite, new Vector2(0, 80), new Vector2(300, 60));
        else
        {
            var header = CreateText(settingsPanel.transform, "Sound", 36, new Vector2(0, 80));
            header.color = new Color(0.7f, 0.7f, 0.8f);
        }

        // Music Volume
        float musicVol = AudioManager.Instance != null ? AudioManager.Instance.musicVolume : 0.5f;
        Sprite musicVolSprite = LoadButtonSprite("Music Volume");
        if (musicVolSprite != null)
            CreateSpriteHeader(settingsPanel.transform, musicVolSprite, new Vector2(0, 35), new Vector2(250, 40));
        else
            CreateText(settingsPanel.transform, "Music Volume", 24, new Vector2(0, 35));
        CreateSlider(settingsPanel.transform, new Vector2(0, 0), musicVol, val =>
        {
            if (AudioManager.Instance != null)
                AudioManager.Instance.SetMusicVolume(val);
        });

        // SFX Volume
        float sfxVol = AudioManager.Instance != null ? AudioManager.Instance.sfxVolume : 0.7f;
        Sprite sfxVolSprite = LoadButtonSprite("SFX Volume");
        if (sfxVolSprite != null)
            CreateSpriteHeader(settingsPanel.transform, sfxVolSprite, new Vector2(0, -55), new Vector2(250, 40));
        else
            CreateText(settingsPanel.transform, "SFX Volume", 24, new Vector2(0, -55));
        CreateSlider(settingsPanel.transform, new Vector2(0, -90), sfxVol, val =>
        {
            if (AudioManager.Instance != null)
                AudioManager.Instance.SetSFXVolume(val);
        });

        // Back
        CreateButton(settingsPanel.transform, "Back", new Vector2(0, -170), ShowMain);
    }

    void ShowSettings()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayMenuOpenSFX();
        mainPanel.SetActive(false);
        settingsPanel.SetActive(true);
    }

    void ShowMain()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayMenuOpenSFX();
        settingsPanel.SetActive(false);
        mainPanel.SetActive(true);
    }

    static void StretchFill(GameObject go)
    {
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    static TextMeshProUGUI CreateText(Transform parent, string text, float fontSize, Vector2 pos)
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
        return tmp;
    }

    static void CreateSpriteHeader(Transform parent, Sprite sprite, Vector2 pos,
        Vector2? size = null)
    {
        GameObject obj = new GameObject("Header_Sprite");
        obj.transform.SetParent(parent, false);
        Image img = obj.AddComponent<Image>();
        img.sprite = sprite;
        img.preserveAspect = true;
        img.raycastTarget = false;

        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size ?? new Vector2(400, 100);
    }

    static Sprite LoadButtonSprite(string label)
    {
        string fileName = label.ToLower().Replace(" ", "");
        Texture2D tex = Resources.Load<Texture2D>("Buttons/" + fileName);
        if (tex == null) return null;
        return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
            new Vector2(0.5f, 0.5f));
    }

    static Button CreateButton(Transform parent, string label, Vector2 pos,
        UnityEngine.Events.UnityAction onClick)
    {
        GameObject btnObj = new GameObject("Button_" + label);
        btnObj.transform.SetParent(parent, false);

        Image btnImage = btnObj.AddComponent<Image>();
        Sprite btnSprite = LoadButtonSprite(label);
        bool hasSprite = btnSprite != null;

        if (hasSprite)
        {
            btnImage.sprite = btnSprite;
            btnImage.preserveAspect = true;
            btnImage.color = Color.white;
        }
        else
        {
            btnImage.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);
        }

        Button btn = btnObj.AddComponent<Button>();
        ColorBlock colors = btn.colors;
        if (hasSprite)
        {
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.7f, 0.7f, 0.85f, 1f);
            colors.pressedColor = new Color(0.5f, 0.5f, 0.6f, 1f);
            colors.selectedColor = new Color(0.7f, 0.7f, 0.85f, 1f);
        }
        else
        {
            colors.normalColor = new Color(0.2f, 0.2f, 0.2f, 0.9f);
            colors.highlightedColor = new Color(0.35f, 0.35f, 0.35f, 1f);
            colors.pressedColor = new Color(0.1f, 0.1f, 0.1f, 1f);
            colors.selectedColor = new Color(0.35f, 0.35f, 0.35f, 1f);
        }
        btn.colors = colors;
        btn.onClick.AddListener(onClick);

        // Hover SFX
        EventTrigger trigger = btnObj.AddComponent<EventTrigger>();
        var hoverEntry = new EventTrigger.Entry();
        hoverEntry.eventID = EventTriggerType.PointerEnter;
        hoverEntry.callback.AddListener((_) =>
        {
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlayMenuScrollSFX();
        });
        trigger.triggers.Add(hoverEntry);

        RectTransform btnRect = btnObj.GetComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(0.5f, 0.5f);
        btnRect.anchorMax = new Vector2(0.5f, 0.5f);
        btnRect.anchoredPosition = pos;
        btnRect.sizeDelta = hasSprite ? new Vector2(300, 75) : new Vector2(300, 50);

        // Label text (only for fallback buttons without sprites)
        if (!hasSprite)
        {
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
        }

        return btn;
    }

    static Slider CreateSlider(Transform parent, Vector2 pos, float initialValue,
        UnityEngine.Events.UnityAction<float> onChanged)
    {
        GameObject sliderObj = new GameObject("Slider");
        sliderObj.transform.SetParent(parent, false);

        RectTransform sliderRect = sliderObj.AddComponent<RectTransform>();
        sliderRect.anchorMin = new Vector2(0.5f, 0.5f);
        sliderRect.anchorMax = new Vector2(0.5f, 0.5f);
        sliderRect.anchoredPosition = pos;
        sliderRect.sizeDelta = new Vector2(300, 20);

        // Background (track)
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(sliderObj.transform, false);
        Image bgImg = bgObj.AddComponent<Image>();
        bgImg.color = new Color(0.1f, 0.1f, 0.15f, 1f);
        RectTransform bgRt = bgObj.GetComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = Vector2.zero;
        bgRt.offsetMax = Vector2.zero;

        // Fill Area
        GameObject fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(sliderObj.transform, false);
        RectTransform fillAreaRt = fillArea.AddComponent<RectTransform>();
        fillAreaRt.anchorMin = Vector2.zero;
        fillAreaRt.anchorMax = Vector2.one;
        fillAreaRt.offsetMin = new Vector2(5, 0);
        fillAreaRt.offsetMax = new Vector2(-5, 0);

        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(fillArea.transform, false);
        Image fillImg = fill.AddComponent<Image>();
        fillImg.color = new Color(0.4f, 0.4f, 0.6f, 1f);
        RectTransform fillRt = fill.GetComponent<RectTransform>();
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = Vector2.one;
        fillRt.offsetMin = Vector2.zero;
        fillRt.offsetMax = Vector2.zero;

        // Handle Slide Area
        GameObject handleArea = new GameObject("Handle Slide Area");
        handleArea.transform.SetParent(sliderObj.transform, false);
        RectTransform handleAreaRt = handleArea.AddComponent<RectTransform>();
        handleAreaRt.anchorMin = Vector2.zero;
        handleAreaRt.anchorMax = Vector2.one;
        handleAreaRt.offsetMin = new Vector2(10, 0);
        handleAreaRt.offsetMax = new Vector2(-10, 0);

        GameObject handle = new GameObject("Handle");
        handle.transform.SetParent(handleArea.transform, false);
        Image handleImg = handle.AddComponent<Image>();
        handleImg.color = Color.white;
        RectTransform handleRt = handle.GetComponent<RectTransform>();
        handleRt.sizeDelta = new Vector2(20, 20);

        // Slider component
        Slider slider = sliderObj.AddComponent<Slider>();
        slider.fillRect = fillRt;
        slider.handleRect = handleRt;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = initialValue;
        slider.onValueChanged.AddListener(onChanged);

        return slider;
    }
}
