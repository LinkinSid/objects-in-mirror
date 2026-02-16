using UnityEngine;
using System.Collections.Generic;

public class ObstacleScript : MonoBehaviour
{
    public GameObject obstaclePrefab;
    public BoxCollider2D backgroundCollider;
    public int obstacleCount = 5;
    public float minSpacing = 2.5f;
    public float edgeSpacing = 1.5f;

    void Start()
    {
        Vector2 center = (Vector2)backgroundCollider.transform.position + backgroundCollider.offset;
        Vector2 size = backgroundCollider.size;

        Sprite sprite = obstaclePrefab.GetComponent<SpriteRenderer>().sprite;
        float padding = sprite.rect.width / sprite.pixelsPerUnit / 2f;

        float inset = padding + edgeSpacing;
        float minX = center.x - size.x / 2f + inset;
        float maxX = center.x + size.x / 2f - inset;
        float minY = center.y - size.y / 2f + inset;
        float maxY = center.y + size.y / 2f - inset;

        List<Vector2> placed = new List<Vector2>();

        for (int i = 0; i < obstacleCount; i++)
        {
            bool found = false;
            for (int attempt = 0; attempt < 100; attempt++)
            {
                Vector2 candidate = new Vector2(Random.Range(minX, maxX), Random.Range(minY, maxY));

                bool tooClose = false;
                foreach (Vector2 existing in placed)
                {
                    if (Vector2.Distance(candidate, existing) < minSpacing)
                    {
                        tooClose = true;
                        break;
                    }
                }

                if (!tooClose)
                {
                    Instantiate(obstaclePrefab, new Vector3(candidate.x, candidate.y, 0f), Quaternion.identity);
                    placed.Add(candidate);
                    found = true;
                    break;
                }
            }

            if (!found)
                Debug.LogWarning("Could not place obstacle " + i + " with enough spacing");
        }
    }

    void Update()
    {

    }
}
