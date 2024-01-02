using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerControllerRogue : MonoBehaviour
{
    public GameObject ballPrefab; // Reference to the ball prefab
    public float shootForce = 20f; // Adjust the force applied to the ball

    [Header("Key Bindings")]
    [SerializeField] KeyCode reloadKey = KeyCode.Backspace;
    [SerializeField] KeyCode ShootKey = KeyCode.Space;

    void Update()
    {

        if(Input.GetKeyDown(reloadKey)) {
            SceneManager.LoadScene("RoguePrototype");
        }
        if(Input.GetKeyDown(ShootKey)) {
            Shoot();
        }

    }

    void Shoot()
    {
        // Create a ray from the camera to the mouse cursor position
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            // Get the point where the ray hits the ground (or any surface)
            Vector3 targetPoint = hit.point;

            // Instantiate a new ball at the target point
            GameObject newBall = Instantiate(ballPrefab, transform.position, Quaternion.identity);

            // Calculate the direction from the player to the target point
            Vector3 shootDirection = (targetPoint - transform.position).normalized;

            // Get the rigidbody of the new ball and apply a force in the calculated direction
            Rigidbody ballRb = newBall.GetComponent<Rigidbody>();
            if (ballRb != null)
            {
                ballRb.AddForce(shootDirection * shootForce, ForceMode.Impulse);
            }
        }
    }
}
