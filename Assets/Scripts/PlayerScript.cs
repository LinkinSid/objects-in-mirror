using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class PlayerScript : MonoBehaviour
{
    public float walkSpeed = 3f;
    public float runSpeed = 6f;

    [Header("Dash")]
    public float dashDistance = 3f;
    public float dashDuration = 0.2f;

    [Header("Shadow Swim")]
    public Sprite swimSprite;

    [Header("UI")]
    public TextMeshProUGUI swimPromptText;

    private Rigidbody2D rb;
    private Collider2D myCollider;
    private Vector2 moveInput;
    private bool isRunning;
    private SpriteRenderer sr;
    private ShadowDetector shadowDetector;
    private Sprite normalSprite;
    private InputAction interactAction;
    private InputAction sprintAction;
    private InputAction dashAction;
    private bool wasPassingThrough;
    private bool swimToggled;
    private bool onCooldown;
    private Health health;
    private bool isDashing;
    private float dashTimeRemaining;

    [Header("Animation")]
    [Tooltip("Scale applied during walk to match idle sprite size")]
    [SerializeField] private float walkScale = 0.88f;

    private Animator animator;
    private Vector2 lastMoveDir = Vector2.down; // default facing down

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        myCollider = GetComponent<Collider2D>();
        sr = GetComponent<SpriteRenderer>();
        shadowDetector = GetComponent<ShadowDetector>();
        health = GetComponent<Health>();
        animator = GetComponent<Animator>();

        normalSprite = sr.sprite;

        if (swimPromptText != null)
            swimPromptText.gameObject.SetActive(false);

        var playerInput = GetComponent<PlayerInput>();
        interactAction = playerInput.actions["Interact"];
        sprintAction = playerInput.actions["Sprint"];
        dashAction = playerInput.actions["Dash"];
    }

    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }

    public void OnSprint(InputValue value)
    {
        isRunning = value.isPressed;
    }

    void Update()
    {
        if (GameManager.IsPaused) return;

        isRunning = sprintAction != null && sprintAction.IsPressed();

        if (dashAction != null && dashAction.WasPressedThisFrame() && !isDashing)
        {
            isDashing = true;
            dashTimeRemaining = dashDuration;
        }

        if (isDashing)
        {
            dashTimeRemaining -= Time.deltaTime;
            if (dashTimeRemaining <= 0)
                isDashing = false;
        }

        // -------- ANIMATION LOGIC --------
        bool isMoving = moveInput != Vector2.zero;

        if (animator != null)
        {
            animator.SetBool("IsMoving", isMoving);
            animator.SetBool("IsRunning", isRunning);

            if (isMoving)
            {
                // prioritize vertical over horizontal (prevents diagonal confusion)
                if (Mathf.Abs(moveInput.y) > Mathf.Abs(moveInput.x))
                    lastMoveDir = new Vector2(0, moveInput.y);
                else
                    lastMoveDir = new Vector2(moveInput.x, 0);

                animator.SetFloat("MoveX", lastMoveDir.x);
                animator.SetFloat("MoveY", lastMoveDir.y);
            }

            transform.localScale = Vector3.one;
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
        if (isDashing)
        {
            rb.linearVelocity = lastMoveDir.normalized * (dashDistance / dashDuration);
            return;
        }

        float speed = isRunning ? runSpeed : walkSpeed;

        if (shadowDetector != null && shadowDetector.isShadowSwimming)
            speed *= shadowDetector.swimSpeedValue;

        rb.linearVelocity = moveInput * speed;
    }
}