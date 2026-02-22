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
        {
            player.transform.position = target.position;

            // Snap camera so it doesn't lag at the old position
            Camera cam = Camera.main;
            if (cam != null)
            {
                Vector3 camPos = cam.transform.position;
                cam.transform.position = new Vector3(target.position.x, target.position.y, camPos.z);
            }
        }

        // Activate the boss during retreat
        if (chase && bossPrefab != null)
            bossPrefab.SetActive(true);
    }
}
