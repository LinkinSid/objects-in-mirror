using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public float smoothSpeed = 5f;

    private Vector3 offset;
    private float shakeTimer;
    private float shakeMagnitude;

    void Start()
    {
        if (target != null)
            offset = transform.position - target.position;
    }

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 desiredPosition = target.position + offset;
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
