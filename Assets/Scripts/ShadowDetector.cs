using UnityEngine;

public class ShadowDetector : MonoBehaviour
{
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

    private int shadowZoneCount;

    void Update()
    {
        isInShadow = shadowZoneCount > 0;
        isShadowSwimming = isInShadow && swimHeld;

        if (isShadowSwimming)
            stress = Mathf.Min(stress + stressGainRate * Time.deltaTime, maxStress);
        else
            stress = Mathf.Max(stress - stressDecayRate * Time.deltaTime, 0f);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponent<ShadowZone>() != null)
            shadowZoneCount++;
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.GetComponent<ShadowZone>() != null)
            shadowZoneCount--;
    }

    public void RefreshCaches() { }
}
