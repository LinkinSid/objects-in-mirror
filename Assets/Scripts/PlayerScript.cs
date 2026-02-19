using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class PlayerScript : MonoBehaviour
{
    public float moveSpeed = 5f;

    [Header("Shadow Swim")]
    public Sprite swimSprite;

    [Header("UI")]
    public TextMeshProUGUI swimPromptText;

    private Rigidbody2D rb;
    private Vector2 moveInput;
    private SpriteRenderer sr;
    private ShadowDetector shadowDetector;
    private Sprite normalSprite;

    private bool isSwimming;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        shadowDetector = GetComponent<ShadowDetector>();
        normalSprite = sr.sprite;

        if (swimPromptText != null)
            swimPromptText.gameObject.SetActive(false);
    }

    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }

    void Update()
    {
        HandleShadowSwimInput();
        UpdateSprite();
        UpdateSwimPrompt();
    }

    void HandleShadowSwimInput()
    {
        if (shadowDetector == null) return;

        bool pressedE = Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;
        bool releasedE = Keyboard.current != null && Keyboard.current.eKey.wasReleasedThisFrame;

        if (pressedE &&
            shadowDetector.isInShadow &&
            shadowDetector.stress < shadowDetector.maxStressValue)
        {
            isSwimming = true;
        }

        if (!shadowDetector.isInShadow || releasedE)
        {
            isSwimming = false;
        }

        shadowDetector.SetShadowSwimming(isSwimming);
    }

    void UpdateSprite()
    {
        if (sr == null || shadowDetector == null) return;

        if (isSwimming && swimSprite != null)
            sr.sprite = swimSprite;
        else
            sr.sprite = normalSprite;
    }

    void UpdateSwimPrompt()
    {
        if (swimPromptText == null || shadowDetector == null) return;

        bool canShow =
            shadowDetector.isInShadow &&
            !isSwimming &&
            shadowDetector.stress < shadowDetector.maxStressValue;

        swimPromptText.gameObject.SetActive(canShow);
    }

    void FixedUpdate()
    {
        float speed = moveSpeed;

        if (shadowDetector != null && isSwimming)
            speed *= shadowDetector.swimSpeedValue;

        rb.linearVelocity = moveInput * speed;
    }
}
