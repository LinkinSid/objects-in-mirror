using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using TMPro;

public class GameOverScreen : MonoBehaviour
{
    [Tooltip("Seconds to wait before showing")]
    public float showDelay = 2f;

    [Tooltip("Death screen image")]
    public Sprite deathScreenSprite;

    private Health playerHealth;
    private GameObject gameOverPanel;
    private Image fadeImage;
    private bool shown;
    private float deathTimer = -1f;

    void Start()
    {
        var player = FindAnyObjectByType<PlayerScript>();
        if (player != null)
            playerHealth = player.GetComponent<Health>();

        EnsureEventSystem();
        BuildUI();
        gameOverPanel.SetActive(false);
    }

    void Update()
    {
        if (shown || playerHealth == null) return;
        if (!playerHealth.isDead) return;

        // Start counting after death
        if (deathTimer < 0f)
        {
            deathTimer = 0f;
            if (fadeImage != null)
                fadeImage.gameObject.SetActive(true);

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.StopAllAudio();
                AudioManager.Instance.PlayDeathSFX();
            }
        }

        deathTimer += Time.deltaTime;

        // Fade in the death screen image
        if (fadeImage != null && showDelay > 0f)
        {
            float alpha = Mathf.Clamp01(deathTimer / showDelay);
            fadeImage.color = new Color(1f, 1f, 1f, alpha);
        }

        if (deathTimer >= showDelay)
        {
            shown = true;
            gameOverPanel.SetActive(true);
        }
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
        GameObject canvasObj = new GameObject("GameOverCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 210;
        var scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasObj.AddComponent<GraphicRaycaster>();

        // Death screen fade image
        if (deathScreenSprite != null)
        {
            GameObject fadeObj = new GameObject("DeathScreenFade");
            fadeObj.transform.SetParent(canvasObj.transform, false);
            RectTransform fadeRect = fadeObj.AddComponent<RectTransform>();
            fadeRect.anchorMin = Vector2.zero;
            fadeRect.anchorMax = Vector2.one;
            fadeRect.offsetMin = Vector2.zero;
            fadeRect.offsetMax = Vector2.zero;
            fadeImage = fadeObj.AddComponent<Image>();
            fadeImage.sprite = deathScreenSprite;
            fadeImage.preserveAspect = true;
            fadeImage.color = new Color(1f, 1f, 1f, 0f);
            fadeObj.SetActive(false);
        }

        // Panel
        gameOverPanel = new GameObject("GameOverPanel");
        gameOverPanel.transform.SetParent(canvasObj.transform, false);
        RectTransform panelRect = gameOverPanel.AddComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        // Dark overlay
        Image overlay = gameOverPanel.AddComponent<Image>();
        overlay.color = new Color(0.15f, 0, 0, 0.8f);

        // Buttons
        CreateButton(gameOverPanel.transform, "Restart", new Vector2(0, -20),
            () => GameManager.Instance.RestartLevel());
        CreateButton(gameOverPanel.transform, "Main Menu", new Vector2(0, -90),
            () => GameManager.Instance.GoToMainMenu());
    }

    static void CreateText(Transform parent, string text, float fontSize, Vector2 pos)
    {
        GameObject obj = new GameObject("Text_" + text);
        obj.transform.SetParent(parent, false);
        TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = new Color(0.9f, 0.2f, 0.2f);

        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(600, fontSize + 20);
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
            btnImage.color = new Color(0.25f, 0.1f, 0.1f, 0.9f);
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
            colors.normalColor = new Color(0.25f, 0.1f, 0.1f, 0.9f);
            colors.highlightedColor = new Color(0.4f, 0.15f, 0.15f, 1f);
            colors.pressedColor = new Color(0.15f, 0.05f, 0.05f, 1f);
            colors.selectedColor = new Color(0.4f, 0.15f, 0.15f, 1f);
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
}
