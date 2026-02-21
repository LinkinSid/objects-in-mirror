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
    public InputActionReference interactAction;

    private string[] lines;
    private int currentLine;
    private bool isTyping;

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
            dialogueBox.SetActive(false);
        if (dialogueBorder != null)
            dialogueBorder.SetActive(false);
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
        lines = dialogueLines;
        currentLine = 0;
        dialogueBox.SetActive(true);
        if (dialogueBorder != null)
            dialogueBorder.SetActive(true);
        Canvas.ForceUpdateCanvases();
        StartCoroutine(TypeLine());
        Time.timeScale = 0f;
    }

    IEnumerator TypeLine()
    {
        isTyping = true;
        dialogueText.text = "";

        foreach (char c in lines[currentLine])
        {
            dialogueText.text += c;
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
            dialogueBox.SetActive(false);
            if (dialogueBorder != null)
                dialogueBorder.SetActive(false);
            Time.timeScale = 1f;
        }
    }
}