using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WireTouchAlert : MonoBehaviour
{

    public GameManager gameManager;
    private string ring_boundary_tag = "RingBound";
    public AudioClip error_sound;

    void Start()
    {
        if (gameManager == null)
        {
            gameManager = FindObjectOfType<GameManager>(); // always find the game manager object first
            if (gameManager == null)
            {
                Debug.LogError("GameManager reference not found!");
            }
        }
    }


    void Update()
    {
        
    }

    private void OnTriggerEnter(Collider other)
    {
        
        if (other.CompareTag(ring_boundary_tag))
        {
            Debug.Log("HITT" + ring_boundary_tag);
            
            // play beep sound
            AudioSource.PlayClipAtPoint(error_sound, Camera.main.transform.position);
            // change color to red
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(ring_boundary_tag))
        {
            Debug.Log("Capsule collider exited a trigger with tag: " + ring_boundary_tag);
            //change the color back to normal
            // count as one error, right?
        }
    }


}
