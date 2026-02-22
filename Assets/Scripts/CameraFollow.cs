using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public float smoothSpeed = 5f;

    [Header("Look Ahead")]
    public float lookAheadDistance = 2f;
    public float lookAheadSmooth = 3f;

    private Vector3 offset;
    private float shakeTimer;
    private float shakeMagnitude;
    private Rigidbody2D targetRb;
    private Vector3 lookAheadOffset;

    // Cinematic pan
    private bool inCinematic;
    private Vector3 cinematicTarget;
    private float cinematicSpeed;

    void Start()
    {
        if (target != null)
        {
            offset = transform.position - target.position;
            targetRb = target.GetComponent<Rigidbody2D>();
        }
    }

    void LateUpdate()
    {
        if (target == null && !inCinematic) return;

        Vector3 smoothed;

        if (inCinematic)
        {
            smoothed = Vector3.Lerp(transform.position, cinematicTarget,
                cinematicSpeed * Time.unscaledDeltaTime);
        }
        else
        {
            Vector3 targetLookAhead = Vector3.zero;
            if (targetRb != null && targetRb.linearVelocity.sqrMagnitude > 0.1f)
                targetLookAhead = (Vector3)targetRb.linearVelocity.normalized * lookAheadDistance;

            lookAheadOffset = Vector3.Lerp(lookAheadOffset, targetLookAhead, lookAheadSmooth * Time.deltaTime);

            Vector3 desiredPosition = target.position + offset + lookAheadOffset;
            smoothed = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
        }

        if (shakeTimer > 0)
        {
            float dt = inCinematic ? Time.unscaledDeltaTime : Time.deltaTime;
            shakeTimer -= dt;
            smoothed += (Vector3)(Random.insideUnitCircle * shakeMagnitude);
            if (shakeTimer <= 0) shakeMagnitude = 0;
        }

        transform.position = smoothed;
    }

    public void Shake(float magnitude, float duration)
    {
        if (magnitude >= shakeMagnitude)
        {
            shakeMagnitude = magnitude;
            shakeTimer = duration;
        }
    }

    public void PanTo(Vector3 worldPos, float speed = 5f)
    {
        inCinematic = true;
        cinematicTarget = new Vector3(worldPos.x, worldPos.y, transform.position.z);
        cinematicSpeed = speed;
    }

    public void ExitCinematic()
    {
        inCinematic = false;
    }

    public bool HasReachedCinematicTarget(float threshold = 0.05f)
    {
        return inCinematic && Vector3.Distance(transform.position, cinematicTarget) < threshold;
    }
}
