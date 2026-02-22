using UnityEngine;
using UnityEngine.Rendering.Universal;

public class LightKillOnCamera : MonoBehaviour
{
    public float fadeSpeed = 2f;

    private Light2D light2D;
    private bool fading;

    void Start()
    {
        light2D = GetComponent<Light2D>();
        Debug.Log($"[LightKill] Start on {gameObject.name} — light2D={light2D != null}, GM={GameManager.Instance != null}, chase={GameManager.Instance?.bossChaseActive}");
    }

    void Update()
    {
        if (light2D == null) return;

        if (!fading)
        {
            if (GameManager.Instance != null && GameManager.Instance.bossChaseActive)
            {
                Debug.Log($"[LightKill] Fading started on {gameObject.name}");
                fading = true;
            }
            else
                return;
        }

        light2D.intensity -= fadeSpeed * Time.deltaTime;
        if (light2D.intensity <= 0f)
        {
            light2D.intensity = 0f;
            light2D.enabled = false;
            enabled = false;
        }
    }
}
