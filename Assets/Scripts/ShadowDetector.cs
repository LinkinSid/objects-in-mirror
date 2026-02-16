using UnityEngine;
using UnityEngine.Rendering.Universal;

public class ShadowDetector : MonoBehaviour
{
    public LayerMask obstacleMask;
    public bool isInShadow;

    Light2D[] lights;

    void Start()
    {
        lights = FindObjectsOfType<Light2D>();
    }

    void Update()
    {
        bool wasInShadow = isInShadow;
        isInShadow = true; // assume shadow

        foreach (Light2D light in lights)
        {
            Vector2 lightPos = light.transform.position;
            Vector2 playerPos = transform.position;

            Vector2 toPlayer = playerPos - lightPos;
            float distance = toPlayer.magnitude;

            // Outside light radius
            if (distance > light.pointLightOuterRadius)
                continue;

            // Cone check (spotlight)
            Vector2 lightDir = light.transform.right;
            float angle = Vector2.Angle(lightDir, toPlayer);

            if (angle > light.pointLightOuterAngle / 2f)
                continue;

            // Raycast ONLY against obstacles
            RaycastHit2D hit = Physics2D.Raycast(
                lightPos,
                toPlayer.normalized,
                distance,
                obstacleMask
            );

            // If nothing blocks the light → player is lit
            if (hit.collider == null)
            {
                isInShadow = false;
                break;
            }
        }

        if (isInShadow != wasInShadow)
        {
            if (isInShadow)
                Debug.Log("Player is IN SHADOW");
            else
                Debug.Log("Player is IN LIGHT");
        }
    }
}