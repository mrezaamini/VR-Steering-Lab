using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HitHandler : MonoBehaviour
{
    public GameManager gameManager;
    // Start is called before the first frame update
    void Start()
    {
        if (gameManager == null)
        {
            gameManager = FindObjectOfType<GameManager>();
            if (gameManager == null)
            {
                Debug.LogError("GameManager reference not found!");
            }
        }
    }

    // Update is called once per frame
    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.CompareTag("SteeringPath"))
        {
            gameManager.OnHitBoundaries();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("SteeringPath"))
        {
            gameManager.OnCorrectHit();
        }
    }
}
