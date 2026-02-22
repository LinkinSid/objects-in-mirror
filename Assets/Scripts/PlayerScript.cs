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
    private InputAction interactAction;
    private InputAction sprintAction;
    private InputAction dashAction;
    private bool wasPassingThrough;
    private bool swimToggled;
    private bool onCooldown;
    private Health health;
    private bool isDashing;
    private float dashTimeRemaining;

    private Animator animator;
    private Vector2 lastMoveDir = Vector2.down; // default facing down
    private MaterialPropertyBlock mpb;
    private static readonly int SwimmingProp = Shader.PropertyToID("_Swimming");

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        myCollider = GetComponent<Collider2D>();
        sr = GetComponent<SpriteRenderer>();
        shadowDetector = GetComponent<ShadowDetector>();
        health = GetComponent<Health>();
        animator = GetComponent<Animator>();

        if (animator != null)
            animator.updateMode = AnimatorUpdateMode.UnscaledTime;

        mpb = new MaterialPropertyBlock();

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
        bool isPaused = Time.timeScale == 0f;
        
        if (GameManager.IsPaused || isPaused) return;

        isRunning = sprintAction != null && sprintAction.IsPressed();

        if (dashAction != null && dashAction.WasPressedThisFrame() && !isDashing && moveInput != Vector2.zero)
        {
            isDashing = true;
            dashTimeRemaining = dashDuration;

            if (AudioManager.Instance != null)
                AudioManager.Instance.PlayDashSFX();
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
            if (isMoving)
            {
                // prioritize vertical over horizontal (prevents diagonal confusion)
                if (Mathf.Abs(moveInput.y) > Mathf.Abs(moveInput.x))
                    lastMoveDir = new Vector2(0, moveInput.y);
                else
                    lastMoveDir = new Vector2(moveInput.x, 0);
            }

            animator.SetBool("IsMoving", isMoving);
            animator.SetBool("IsRunning", (isRunning || isDashing) && isMoving);
            animator.SetFloat("MoveX", lastMoveDir.x);
            animator.SetFloat("MoveY", lastMoveDir.y);
        }
        // --------------------------------

        if (shadowDetector != null)
        {
            bool justPressed = interactAction != null && interactAction.WasPressedThisFrame();

            if (justPressed)
            {
                if (AudioManager.Instance != null)
                    AudioManager.Instance.PlaySFX(AudioManager.Instance.meowSfx, 10f);

                if (!onCooldown)
                {
                    swimToggled = !swimToggled;
                    if (swimToggled && shadowDetector.isInShadow && AudioManager.Instance != null)
                        AudioManager.Instance.PlaySwimSFX();
                }
            }

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

        // Footsteps
        if (AudioManager.Instance != null)
        {
            if (isMoving && !swimming)
                AudioManager.Instance.StartFootsteps(isRunning);
            else
                AudioManager.Instance.StopFootsteps();
        }

        UpdateSprite();
        UpdateSwimPrompt();
    }

    void UpdateSprite()
    {
        if (sr == null || shadowDetector == null || mpb == null) return;

        bool canSwim = shadowDetector.isShadowSwimming
            && shadowDetector.stress < shadowDetector.maxStressValue;

        sr.GetPropertyBlock(mpb);
        mpb.SetFloat(SwimmingProp, canSwim ? 1f : 0f);
        sr.SetPropertyBlock(mpb);
    }

    void OnDisable()
    {
        // Reset shader to normal mode when disabled (e.g., on death)
        if (sr != null && mpb != null)
        {
            sr.GetPropertyBlock(mpb);
            mpb.SetFloat(SwimmingProp, 0f);
            sr.SetPropertyBlock(mpb);
        }
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
        if (Time.timeScale == 0f)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

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