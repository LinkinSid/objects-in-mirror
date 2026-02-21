using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using TMPro;

public class MainMenu : MonoBehaviour
{
    void Start()
    {
        EnsureEventSystem();
        BuildUI();
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
        GameObject canvasObj = new GameObject("MainMenuCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasObj.AddComponent<GraphicRaycaster>();

        // Full-screen background
        GameObject bg = new GameObject("Background");
        bg.transform.SetParent(canvasObj.transform, false);
        Image bgImage = bg.AddComponent<Image>();
        bgImage.color = new Color(0.05f, 0.05f, 0.1f, 1f);
        RectTransform bgRect = bg.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;

        // Title
        CreateText(canvasObj.transform, "Shadow Swimming Fatass", 72, new Vector2(0, 120));

        // Subtitle
        var sub = CreateText(canvasObj.transform, "", 28,
            new Vector2(0, 60));
        sub.fontStyle = FontStyles.Italic;
        sub.color = new Color(0.7f, 0.7f, 0.7f);

        // Buttons
        CreateButton(canvasObj.transform, "Play", new Vector2(0, -40),
            () => GameManager.Instance.LoadScene(GameManager.Instance.firstLevelScene));
        CreateButton(canvasObj.transform, "Quit", new Vector2(0, -110),
            () => GameManager.Instance.QuitGame());
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
        rt.sizeDelta = new Vector2(800, fontSize + 20);
        return tmp;
    }

    static Button CreateButton(Transform parent, string label, Vector2 pos,
        UnityEngine.Events.UnityAction onClick)
    {
        GameObject btnObj = new GameObject("Button_" + label);
        btnObj.transform.SetParent(parent, false);

        Image btnImage = btnObj.AddComponent<Image>();
        btnImage.color = new Color(0.15f, 0.15f, 0.2f, 0.9f);

        Button btn = btnObj.AddComponent<Button>();
        ColorBlock colors = btn.colors;
        colors.normalColor = new Color(0.15f, 0.15f, 0.2f, 0.9f);
        colors.highlightedColor = new Color(0.25f, 0.25f, 0.35f, 1f);
        colors.pressedColor = new Color(0.1f, 0.1f, 0.15f, 1f);
        colors.selectedColor = new Color(0.25f, 0.25f, 0.35f, 1f);
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
        tmp.fontSize = 30;
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
