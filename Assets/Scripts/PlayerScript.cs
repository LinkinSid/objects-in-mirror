using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerScript : MonoBehaviour
{
    public float moveSpeed = 5f;

    [Header("Shadow Swim")]
    public Sprite swimSprite;

    private Rigidbody2D rb;
    private Vector2 moveInput;
    private SpriteRenderer sr;
    private ShadowDetector shadowDetector;
    private Sprite normalSprite;
    private InputAction crouchAction;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        shadowDetector = GetComponent<ShadowDetector>();
        normalSprite = sr.sprite;
        crouchAction = GetComponent<PlayerInput>().actions["Crouch"];
    }

    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }

    void Update()
    {
        if (shadowDetector != null)
            shadowDetector.swimHeld = crouchAction != null && crouchAction.IsPressed();
        UpdateSprite();
    }

    void UpdateSprite()
    {
        if (sr == null || shadowDetector == null) return;

        bool canSwim = shadowDetector.isShadowSwimming
            && shadowDetector.stress < shadowDetector.maxStressValue;

        if (canSwim && swimSprite != null)
            sr.sprite = swimSprite;
        else
            sr.sprite = normalSprite;
    }

    void FixedUpdate()
    {
        float speed = moveSpeed;

        if (shadowDetector != null && shadowDetector.isShadowSwimming)
            speed *= shadowDetector.swimSpeedValue;

        rb.linearVelocity = moveInput * speed;
    }
}
