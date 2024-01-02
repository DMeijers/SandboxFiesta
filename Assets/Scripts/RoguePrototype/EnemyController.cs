using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyController : MonoBehaviour
{
    public float speed = 5f; // Adjust the speed as needed
    private Transform player;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
        if (player == null)
        {
            Debug.LogError("Player not found. Make sure the player has the 'Player' tag.");
        }

        SpawnAtRandomPosition();
    }

    void Update()
    {
        if (player != null)
        {
            MoveTowardsPlayer();
        }
    }

    void MoveTowardsPlayer()
    {
        transform.LookAt(player);
        transform.Translate(Vector3.forward * speed * Time.deltaTime);
    }

    void SpawnAtRandomPosition()
    {
        Vector3 randomPosition = new Vector3(Random.Range(-10f, 10f), 0f, Random.Range(-10f, 10f)); // Adjust the range based on your map size
        transform.position = randomPosition;
    }
}
