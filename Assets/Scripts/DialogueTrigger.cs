using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class DialogueTrigger : MonoBehaviour
{
    [TextArea(3, 10)]
    public string[] dialogueLines;
    public DialogueManager dialogueManager;
    public InputActionReference interactAction;
    public float promptYOffset = 1f;

    private bool playerInRange;
    private bool hasTriggered;
    private GameObject promptObject;
    private TextMeshPro promptText;

    void OnEnable()
    {
        if (interactAction != null)
            interactAction.action.Enable();
    }

    void Start()
    {
        CreatePrompt();
    }

    void OnDisable()
    {
        if (interactAction != null)
            interactAction.action.Disable();
    }

    void Update()
    {
        if (hasTriggered) return;

        if (promptObject != null)
            promptObject.SetActive(playerInRange);

        if (playerInRange && interactAction != null && interactAction.action.WasPressedThisFrame())
        {
            if (dialogueManager != null && !dialogueManager.dialogueBox.activeSelf)
            {
                dialogueManager.StartDialogue(dialogueLines);
                hasTriggered = true;
            }
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            hasTriggered = false;
        }
    }

    void CreatePrompt()
    {
        promptObject = new GameObject("InteractPrompt");
        promptObject.transform.SetParent(transform);
        promptObject.transform.localPosition = new Vector3(0, promptYOffset, 0);

        promptText = promptObject.AddComponent<TextMeshPro>();
        promptText.text = "Press E to read";
        promptText.fontSize = 3;
        promptText.alignment = TextAlignmentOptions.Center;
        promptText.color = Color.white;
        promptText.sortingOrder = 100;

        RectTransform rt = promptObject.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(5, 1);

        promptObject.SetActive(false);
    }
}
