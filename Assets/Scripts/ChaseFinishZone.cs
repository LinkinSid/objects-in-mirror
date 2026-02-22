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
        if (GameManager.Instance == null || !GameManager.Instance.bossChaseActive) return;
        if (other.GetComponent<PlayerScript>() == null) return;

        triggered = true;

        if (boss != null)
            boss.StopChase();

        // Lock all doors so player can't leave
        foreach (var door in FindObjectsByType<Door>(FindObjectsSortMode.None))
        {
            door.enabled = false;
            var col = door.GetComponent<Collider2D>();
            if (col != null) col.enabled = false;
        }

        if (GameManager.Instance != null)
            GameManager.Instance.bossChaseActive = false;

        onChaseComplete?.Invoke();
    }
}
