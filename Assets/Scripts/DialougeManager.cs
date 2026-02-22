using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;

public class DialogueManager : MonoBehaviour
{
    public GameObject dialogueBox;
    public GameObject dialogueBorder;
    public TextMeshProUGUI dialogueText;
    public float typingSpeed = 0.03f;
    public float animationDuration = 0.3f;
    public float slideDistance = 300f;
    public InputActionReference interactAction;

    private string[] lines;
    private int currentLine;
    private bool isTyping;
    private bool isDialogueActive;
    private CanvasGroup boxCanvasGroup;
    private CanvasGroup borderCanvasGroup;
    private Vector3 boxOriginalPos;
    private Vector3 borderOriginalPos;

    void Start()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler != null)
            {
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0.5f;
            }
        }

        if (dialogueBox != null)
        {
            dialogueBox.SetActive(false);
            if (dialogueBox.GetComponent<CanvasGroup>() == null)
                boxCanvasGroup = dialogueBox.AddComponent<CanvasGroup>();
            else
                boxCanvasGroup = dialogueBox.GetComponent<CanvasGroup>();
            boxOriginalPos = dialogueBox.transform.localPosition;
        }
        if (dialogueBorder != null)
        {
            dialogueBorder.SetActive(false);
            if (dialogueBorder.GetComponent<CanvasGroup>() == null)
                borderCanvasGroup = dialogueBorder.AddComponent<CanvasGroup>();
            else
                borderCanvasGroup = dialogueBorder.GetComponent<CanvasGroup>();
            borderOriginalPos = dialogueBorder.transform.localPosition;
        }
    }

    private void OnEnable()
    {
        interactAction.action.Enable();
    }

    private void OnDisable()
    {
        interactAction.action.Disable();
    }

    void Update()
    {
        if (dialogueBox.activeSelf && interactAction.action.WasPressedThisFrame())
        {
            if (isTyping)
            {
                StopAllCoroutines();
                dialogueText.text = lines[currentLine];
                isTyping = false;
            }
            else
            {
                NextLine();
            }
        }
    }

    public void StartDialogue(string[] dialogueLines)
    {
        if (isDialogueActive) return;

        lines = dialogueLines;
        currentLine = 0;
        isDialogueActive = true;
        StartCoroutine(ShowDialogueWithAnimation());
    }

    IEnumerator ShowDialogueWithAnimation()
    {
        dialogueBox.SetActive(true);
        if (dialogueBorder != null)
            dialogueBorder.SetActive(true);

        dialogueBox.transform.localPosition = boxOriginalPos - new Vector3(0, slideDistance, 0);
        if (dialogueBorder != null)
            dialogueBorder.transform.localPosition = borderOriginalPos - new Vector3(0, slideDistance, 0);

        if (boxCanvasGroup != null)
            boxCanvasGroup.alpha = 0f;
        if (borderCanvasGroup != null)
            borderCanvasGroup.alpha = 0f;

        Canvas.ForceUpdateCanvases();
        Time.timeScale = 0f;

        float elapsed = 0f;
        while (elapsed < animationDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / animationDuration;
            float easeOut = 1f - Mathf.Pow(1f - t, 3f);

            dialogueBox.transform.localPosition = Vector3.Lerp(
                boxOriginalPos - new Vector3(0, slideDistance, 0),
                boxOriginalPos,
                easeOut
            );
            if (dialogueBorder != null)
                dialogueBorder.transform.localPosition = Vector3.Lerp(
                    borderOriginalPos - new Vector3(0, slideDistance, 0),
                    borderOriginalPos,
                    easeOut
                );

            if (boxCanvasGroup != null)
                boxCanvasGroup.alpha = t;
            if (borderCanvasGroup != null)
                borderCanvasGroup.alpha = t;

            yield return null;
        }

        dialogueBox.transform.localPosition = boxOriginalPos;
        if (dialogueBorder != null)
            dialogueBorder.transform.localPosition = borderOriginalPos;
        if (boxCanvasGroup != null)
            boxCanvasGroup.alpha = 1f;
        if (borderCanvasGroup != null)
            borderCanvasGroup.alpha = 1f;

        StartCoroutine(TypeLine());
    }

    IEnumerator TypeLine()
    {
        isTyping = true;
        dialogueText.text = "";

        foreach (char c in lines[currentLine])
        {
            dialogueText.text += c;
            if (c != ' ' && AudioManager.Instance != null)
                AudioManager.Instance.PlaySpeechBlipSFX();
            yield return new WaitForSecondsRealtime(typingSpeed);
        }

        isTyping = false;
    }

    void NextLine()
    {
        currentLine++;

        if (currentLine < lines.Length)
        {
            StartCoroutine(TypeLine());
        }
        else
        {
            StartCoroutine(HideDialogueWithAnimation());
        }
    }

    IEnumerator HideDialogueWithAnimation()
    {
        float elapsed = 0f;
        while (elapsed < animationDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = 1f - (elapsed / animationDuration);
            float easeIn = Mathf.Pow(t, 3f);

            dialogueBox.transform.localPosition = Vector3.Lerp(
                boxOriginalPos - new Vector3(0, slideDistance, 0),
                boxOriginalPos,
                easeIn
            );
            if (dialogueBorder != null)
                dialogueBorder.transform.localPosition = Vector3.Lerp(
                    borderOriginalPos - new Vector3(0, slideDistance, 0),
                    borderOriginalPos,
                    easeIn
                );

            if (boxCanvasGroup != null)
                boxCanvasGroup.alpha = t;
            if (borderCanvasGroup != null)
                borderCanvasGroup.alpha = t;

            yield return null;
        }

        dialogueBox.SetActive(false);
        if (dialogueBorder != null)
            dialogueBorder.SetActive(false);
        isDialogueActive = false;
        Time.timeScale = 1f;
    }
}