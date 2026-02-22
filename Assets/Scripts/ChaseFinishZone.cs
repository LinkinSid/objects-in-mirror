using UnityEngine;
using UnityEngine.Events;

public class ChaseFinishZone : MonoBehaviour
{
    public BossController boss;
    public UnityEvent onChaseComplete;

    bool triggered;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (triggered) return;
        if (other.GetComponent<PlayerScript>() == null) return;

        triggered = true;

        if (boss != null)
            boss.StopChase();

        onChaseComplete?.Invoke();
    }
}
