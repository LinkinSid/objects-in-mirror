using UnityEngine;
using UnityEngine.Rendering.Universal;

public class TorchFlicker : MonoBehaviour
{
    private Light2D light2D;

    public float baseIntensity = 2.0f;
    public float flickerAmount = 0.4f;
    public float flickerSpeed = 3f;

    void Start()
    {
        light2D = GetComponent<Light2D>();
    }

    void Update()
    {
        float noise = Mathf.PerlinNoise(Time.time * flickerSpeed, 0f);
        light2D.intensity = baseIntensity + (noise - 0.5f) * flickerAmount;
    }
}
