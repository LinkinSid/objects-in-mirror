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
        if (target == null) return;

        Vector3 targetLookAhead = Vector3.zero;
        if (targetRb != null && targetRb.linearVelocity.sqrMagnitude > 0.1f)
            targetLookAhead = (Vector3)targetRb.linearVelocity.normalized * lookAheadDistance;

        lookAheadOffset = Vector3.Lerp(lookAheadOffset, targetLookAhead, lookAheadSmooth * Time.deltaTime);

        Vector3 desiredPosition = target.position + offset + lookAheadOffset;
        Vector3 smoothed = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);

        if (shakeTimer > 0)
        {
            shakeTimer -= Time.deltaTime;
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
}
