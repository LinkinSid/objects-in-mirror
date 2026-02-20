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
    private Collider2D myCollider;
    private Vector2 moveInput;
    private SpriteRenderer sr;
    private ShadowDetector shadowDetector;
    private Sprite normalSprite;
    private InputAction interactAction;
    private bool wasPassingThrough;
    private bool swimToggled;
    private bool onCooldown;
    private Health health;

    // 🔽 NEW
    private Animator animator;
    private Vector2 lastMoveDir = Vector2.down; // default facing down

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        myCollider = GetComponent<Collider2D>();
        sr = GetComponent<SpriteRenderer>();
        shadowDetector = GetComponent<ShadowDetector>();
        health = GetComponent<Health>();
        animator = GetComponent<Animator>(); // NEW

        normalSprite = sr.sprite;

        if (swimPromptText != null)
            swimPromptText.gameObject.SetActive(false);

        interactAction = GetComponent<PlayerInput>().actions["Interact"];
    }

    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }

    void Update()
    {
        // -------- ANIMATION LOGIC --------
        bool isMoving = moveInput != Vector2.zero;

        if (animator != null)
        {
            animator.SetBool("IsMoving", isMoving);

            if (isMoving)
            {
                // prioritize vertical over horizontal (prevents diagonal confusion)
                if (Mathf.Abs(moveInput.y) > Mathf.Abs(moveInput.x))
                {
                    lastMoveDir = new Vector2(0, moveInput.y);
                }
                else
                {
                    lastMoveDir = new Vector2(moveInput.x, 0);
                }

                animator.SetFloat("MoveX", lastMoveDir.x);
                animator.SetFloat("MoveY", lastMoveDir.y);
            }
        }
        // --------------------------------

        if (shadowDetector != null)
        {
            bool justPressed = interactAction != null && interactAction.WasPressedThisFrame();

            if (justPressed && !onCooldown)
                swimToggled = !swimToggled;

            if (shadowDetector.stress >= shadowDetector.maxStressValue)
            {
                swimToggled = false;
                onCooldown = true;
            }

            if (onCooldown && shadowDetector.stress <= shadowDetector.maxStressValue * 0.8f)
                onCooldown = false;

            if (!shadowDetector.isInShadow)
                swimToggled = false;

            shadowDetector.swimHeld = swimToggled;
        }

        bool swimming = shadowDetector != null
            && shadowDetector.isShadowSwimming
            && shadowDetector.stress < shadowDetector.maxStressValue;
        bool inIFrames = health != null && health.isInIFrames;
        bool passThrough = swimming || inIFrames;

        if (passThrough != wasPassingThrough)
        {
            wasPassingThrough = passThrough;
            SetEnemyCollisionIgnored(passThrough);
        }

        UpdateSprite();
        UpdateSwimPrompt();
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

    void UpdateSwimPrompt()
    {
        if (swimPromptText == null || shadowDetector == null) return;

        bool canShow =
            shadowDetector.isInShadow &&
            !shadowDetector.isShadowSwimming &&
            !onCooldown;

        swimPromptText.gameObject.SetActive(canShow);
    }

    void SetEnemyCollisionIgnored(bool ignore)
    {
        if (myCollider == null) return;
        foreach (var enemy in FindObjectsByType<EnemyScript>(FindObjectsSortMode.None))
        {
            Collider2D enemyCol = enemy.GetComponent<Collider2D>();
            if (enemyCol != null)
                Physics2D.IgnoreCollision(myCollider, enemyCol, ignore);
        }
        foreach (var boss in FindObjectsByType<BossController>(FindObjectsSortMode.None))
        {
            Collider2D bossCol = boss.GetComponent<Collider2D>();
            if (bossCol != null)
                Physics2D.IgnoreCollision(myCollider, bossCol, ignore);
        }
    }

    void FixedUpdate()
    {
        float speed = moveSpeed;

        if (shadowDetector != null && shadowDetector.isShadowSwimming)
            speed *= shadowDetector.swimSpeedValue;

        rb.linearVelocity = moveInput * speed;
    }
}