using UnityEngine;
using UnityEngine.Rendering.Universal;

public class ShadowDetector : MonoBehaviour
{
    public LayerMask obstacleMask;
    public float maxStress = 100f;
    public float stressGainRate = 15f;
    public float stressDecayRate = 10f;
    public float swimSpeedMultiplier = 1.5f;

    public bool isInShadow { get; private set; }
    public bool isShadowSwimming { get; private set; }
    public bool swimHeld;
    public float stress { get; private set; }
    public float maxStressValue => maxStress;
    public float swimSpeedValue => swimSpeedMultiplier;

    private Light2D[] lights;

    void Start()
    {
        RefreshCaches();
    }

    void Update()
    {
        // --- Light-based shadow detection ---
        isInShadow = true; // assume shadow until a light reaches us

        // DarkenRoom overlay active = treat as shadow
        if (GameManager.Instance != null && GameManager.Instance.bossChaseActive)
            goto Swimming;

        if (lights != null)
        {
            foreach (Light2D light in lights)
            {
                if (light == null || !light.enabled) continue;

                // Skip global lights — they're ambient, not directional
                if (light.lightType == Light2D.LightType.Global)
                    continue;

                Vector2 lightPos = light.transform.position;
                Vector2 playerPos = transform.position;
                Vector2 toPlayer = playerPos - lightPos;
                float distance = toPlayer.magnitude;

                // Outside light radius
                if (distance > light.pointLightOuterRadius)
                    continue;

                // Cone check (spotlight)
                if (light.pointLightOuterAngle < 360f)
                {
                    Vector2 lightDir = light.transform.right;
                    float angle = Vector2.Angle(lightDir, toPlayer);
                    if (angle > light.pointLightOuterAngle / 2f)
                        continue;
                }

                // Raycast ONLY against obstacles
                RaycastHit2D hit = Physics2D.Raycast(
                    lightPos,
                    toPlayer.normalized,
                    distance,
                    obstacleMask
                );

                // If nothing blocks the light -> player is lit
                if (hit.collider == null)
                {
                    isInShadow = false;
                    break;
                }
            }
        }

        Swimming:
        // --- Shadow swimming ---
        isShadowSwimming = isInShadow && swimHeld;

        if (isShadowSwimming)
            stress = Mathf.Min(stress + stressGainRate * Time.deltaTime, maxStress);
        else
            stress = Mathf.Max(stress - stressDecayRate * Time.deltaTime, 0f);
    }

    public void RefreshCaches()
    {
        lights = FindObjectsByType<Light2D>(FindObjectsSortMode.None);
    }
}
