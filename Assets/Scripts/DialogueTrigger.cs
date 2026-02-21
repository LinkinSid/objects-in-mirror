using UnityEngine;
using UnityEngine.InputSystem;

public class DialogueTrigger : MonoBehaviour
{
    [TextArea(3, 10)]
    public string[] dialogueLines;
    public DialogueManager dialogueManager;
    public InputActionReference interactAction;

    private bool playerInRange;

    void OnEnable()
    {
        if (interactAction != null)
            interactAction.action.Enable();
    }

    void OnDisable()
    {
        if (interactAction != null)
            interactAction.action.Disable();
    }

    void Update()
    {
        if (playerInRange && interactAction != null && interactAction.action.WasPressedThisFrame())
        {
            if (dialogueManager != null && !dialogueManager.dialogueBox.activeSelf)
            {
                dialogueManager.StartDialogue(dialogueLines);
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
        }
    }
}
