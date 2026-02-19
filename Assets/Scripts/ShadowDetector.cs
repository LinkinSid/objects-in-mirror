using UnityEngine;
using UnityEngine.Rendering.Universal;
using System.Collections.Generic;

public class ShadowDetector : MonoBehaviour
{
    public LayerMask obstacleMask;
    public float maxStress = 100f;
    public float stressGainRate = 15f;
    public float stressDecayRate = 10f;
    public float swimSpeedMultiplier = 0.6f;

    [Tooltip("Minimum effective light intensity to count as lit")]
    public float minLightContribution = 1f;

    public bool isInShadow { get; private set; }
    public bool isShadowSwimming { get; private set; }   // now controlled by PlayerScript
    public float stress { get; private set; }
    public float maxStressValue => maxStress;
    public float swimSpeedValue => swimSpeedMultiplier;

    Light2D[] lights;
    ShadowCaster2D[] shadowCasters;

    void Start()
    {
        lights = FindObjectsByType<Light2D>(FindObjectsSortMode.None);

        var all = FindObjectsByType<ShadowCaster2D>(FindObjectsSortMode.None);
        var filtered = new List<ShadowCaster2D>();
        foreach (var sc in all)
        {
            if (((1 << sc.gameObject.layer) & obstacleMask) != 0)
                filtered.Add(sc);
        }
        shadowCasters = filtered.ToArray();
    }

    void Update()
    {
        bool wasInShadow = isInShadow;
        Vector2 playerPos = transform.position;

        isInShadow = !IsPointLit(playerPos);

        // ✅ Stress only increases when actively swimming
        if (isShadowSwimming)
            stress = Mathf.Min(stress + stressGainRate * Time.deltaTime, maxStress);
        else
            stress = Mathf.Max(stress - stressDecayRate * Time.deltaTime, 0f);

        if (isInShadow != wasInShadow)
        {
            if (isInShadow)
                Debug.Log("Player is IN SHADOW");
            else
                Debug.Log("Player is IN LIGHT");
        }
    }

    // Called by PlayerScript
    public void SetShadowSwimming(bool swimming)
    {
        isShadowSwimming = swimming;
    }

    bool IsPointLit(Vector2 point)
    {
        foreach (Light2D light in lights)
        {
            Vector2 lightPos = light.transform.position;
            Vector2 toPoint = point - lightPos;
            float distance = toPoint.magnitude;

            if (distance > light.pointLightOuterRadius)
                continue;

            float t = Mathf.Clamp01((distance - light.pointLightInnerRadius)
                / (light.pointLightOuterRadius - light.pointLightInnerRadius));
            float effectiveIntensity = light.intensity * (1f - t);
            if (effectiveIntensity < minLightContribution)
                continue;

            Vector2 lightDir = light.transform.right;
            float angle = Vector2.Angle(lightDir, toPoint);
            if (angle > light.pointLightOuterAngle / 2f)
                continue;

            bool blocked = false;
            foreach (var caster in shadowCasters)
            {
                if (IsInShadowVolume(point, lightPos, caster))
                {
                    blocked = true;
                    break;
                }
            }

            if (!blocked)
                return true;
        }
        return false;
    }

    bool IsInShadowVolume(Vector2 point, Vector2 lightPos, ShadowCaster2D caster)
    {
        Vector3[] shapePath = caster.shapePath;
        if (shapePath == null || shapePath.Length < 2)
            return false;

        Transform t = caster.transform;

        Vector2 center = t.position;
        Vector2 baseDir = (center - lightPos).normalized;
        float minAngle = float.MaxValue;
        float maxAngle = float.MinValue;
        float nearestDist = float.MaxValue;

        foreach (Vector3 localVert in shapePath)
        {
            Vector2 worldVert = t.TransformPoint(localVert);
            float a = Vector2.SignedAngle(baseDir, worldVert - lightPos);
            if (a < minAngle) minAngle = a;
            if (a > maxAngle) maxAngle = a;

            float dist = (worldVert - lightPos).magnitude;
            if (dist < nearestDist) nearestDist = dist;
        }

        float pointAngle = Vector2.SignedAngle(baseDir, point - lightPos);
        if (pointAngle < minAngle || pointAngle > maxAngle)
            return false;

        float pointDist = (point - lightPos).magnitude;
        return pointDist > nearestDist;
    }
}
