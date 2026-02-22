using UnityEngine;

public class PlayerSpawn : MonoBehaviour
{
    public Transform normalSpawn;
    public Transform chaseSpawn;
    public GameObject bossPrefab;

    void Awake()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        bool chase = GameManager.Instance != null && GameManager.Instance.bossChaseActive;
        Transform target = chase ? chaseSpawn : normalSpawn;

        if (target != null)
            player.transform.position = target.position;

        // Activate the boss during retreat
        if (chase && bossPrefab != null)
            bossPrefab.SetActive(true);
    }
}
