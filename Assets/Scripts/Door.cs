using UnityEngine;

public class Door : MonoBehaviour
{
    public string nextSceneName;
    public bool startLocked;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (string.IsNullOrEmpty(nextSceneName)) return;

        // Locked doors only open when boss chase is active
        if (startLocked && (GameManager.Instance == null || !GameManager.Instance.bossChaseActive))
            return;

        GameManager.Instance.LoadScene(nextSceneName);
    }
}
