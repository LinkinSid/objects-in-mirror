using UnityEngine;
using UnityEngine.Rendering.Universal;

public class LightSwitch : MonoBehaviour
{
    [Tooltip("Light2D components to toggle when the player hits this switch.")]
    public Light2D[] lights;

    [Tooltip("Optional sprite to show after activation.")]
    public Sprite activatedSprite;

    [Tooltip("Allow the switch to be toggled multiple times.")]
    public bool reusable;

    private bool used;
    private bool lightsOn;

    void Start()
    {
        if (lights.Length > 0 && lights[0] != null)
            lightsOn = lights[0].enabled;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (used && !reusable) return;
        if (other.GetComponent<PlayerScript>() == null) return;

        used = true;
        lightsOn = !lightsOn;

        foreach (Light2D light in lights)
        {
            if (light != null)
                light.enabled = lightsOn;
        }

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(AudioManager.Instance.menuOpenSfx);

        if (activatedSprite != null)
        {
            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            if (sr != null)
                sr.sprite = activatedSprite;
        }
    }
}
