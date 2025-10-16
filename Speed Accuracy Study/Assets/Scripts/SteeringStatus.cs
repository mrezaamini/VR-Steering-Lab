using System.Collections;
using System.Collections.Generic;
using UnityEngine;


// THIS CODE IS NOT EFFECTIVE CURRENTLY!! this code is for changing traverse status to start or finish for each trial based on the collision with
// start and end bound on the wire. However, now PLAN B is effective which does not work with with colliders, it works with distance calculation
// from the start point as the ring rotation is locked and wire is straight (for now) 
//
// if the use case is changed and working with colliders are needed REMEMBER to activate start and end bounds on wire prefab!! with this CODE on the ring inner surface colllider
public class SteeringStatus : MonoBehaviour 
{
    public GameManager gameManager;

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

    private void OnTriggerEnter(Collider other)
    {
        if (other.transform.parent == transform.parent)
        {
            return;
        }
        if (other.gameObject.CompareTag("StartPoint"))
        {
            Debug.Log("Startedddd, YAYYYY");
            gameManager.OnStartTraversing();
            
        }
        if (other.gameObject.CompareTag("EndPoint"))
        {
            Debug.Log("ENDEDD, YAYYYY");
            gameManager.EndTrial();
        }

    }
}
