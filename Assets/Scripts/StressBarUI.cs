using UnityEngine;
using UnityEngine.UI;

public class StressBarUI : MonoBehaviour
{
    public ShadowDetector shadowDetector;

    [Header("Bar Settings")]
    public Color barColor = new Color(0.6f, 0.1f, 0.8f);       // purple
    public Color backgroundColor = new Color(0.2f, 0.2f, 0.2f); // dark grey
    public float barWidth = 200f;
    public float barHeight = 20f;
    public float marginX = 20f;
    public float marginY = 20f;

    private Image fillImage;
    private GameObject barRoot;

    void Start()
    {
        if (shadowDetector == null)
            shadowDetector = FindAnyObjectByType<ShadowDetector>();

        BuildUI();
    }

    void BuildUI()
    {
        // Canvas
        barRoot = new GameObject("StressBarCanvas");
        Canvas canvas = barRoot.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        barRoot.AddComponent<CanvasScaler>();
        barRoot.AddComponent<GraphicRaycaster>();

        // Background
        GameObject bg = new GameObject("StressBarBG");
        bg.transform.SetParent(barRoot.transform, false);
        Image bgImage = bg.AddComponent<Image>();
        bgImage.color = backgroundColor;

        RectTransform bgRect = bg.GetComponent<RectTransform>();
        bgRect.anchorMin = new Vector2(0, 1);
        bgRect.anchorMax = new Vector2(0, 1);
        bgRect.pivot = new Vector2(0, 1);
        bgRect.anchoredPosition = new Vector2(marginX, -marginY);
        bgRect.sizeDelta = new Vector2(barWidth, barHeight);

        // Fill
        GameObject fill = new GameObject("StressBarFill");
        fill.transform.SetParent(bg.transform, false);
        fillImage = fill.AddComponent<Image>();
        fillImage.color = barColor;

        RectTransform fillRect = fill.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = new Vector2(0, 1);
        fillRect.pivot = new Vector2(0, 0.5f);
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
    }

    void Update()
    {
        if (shadowDetector == null || fillImage == null) return;

        float fill = shadowDetector.stress / shadowDetector.maxStressValue;
        RectTransform rt = fillImage.GetComponent<RectTransform>();
        rt.anchorMax = new Vector2(fill, 1);
    }
}
