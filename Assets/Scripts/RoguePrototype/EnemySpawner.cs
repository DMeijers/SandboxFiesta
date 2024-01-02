using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public GameObject enemyPrefab; // Reference to your enemy prefab
    public int numberOfEnemies = 5; // Adjust the number of enemies to spawn
    public float spawnInterval = 3f; // Adjust the interval between spawns
    public float spawnRadius = 5f; // Adjust the radius of the spawn circle


    void Start()
    {
        InvokeRepeating("SpawnEnemies", 0f, spawnInterval);
    }

    void SpawnEnemies()
    {
        for (int i = 0; i < numberOfEnemies; i++)
        {
            Vector2 randomCirclePoint = Random.insideUnitCircle * spawnRadius;
            Vector3 randomPosition = new Vector3(randomCirclePoint.x, 0f, randomCirclePoint.y);
            Instantiate(enemyPrefab, transform.position + randomPosition, Quaternion.identity);
        }
    }

    // Draw a wireframe circle representing the spawn area in the Unity Editor
    void OnDrawGizmos()
    {
                Gizmos.color = new Color(0f, 1f, 0f, 1f); // Green color with 50% transparency

        Gizmos.DrawWireSphere(transform.position, spawnRadius);
    }
}
