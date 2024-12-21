using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

        

     
            //Debug.Log("otherr collider " + other.gameObject.name);
            //Debug.Log("thiss collider " + gameObject.tag);
            //if (other.gameObject.CompareTag("InsideRing"))
            //{
            //    gameManager.OnStartTraversing();
            //}
            //else if (other.gameObject.CompareTag("EndPoint"))
            //{
            //    gameManager.OnFinishTraversing();
            //}

    }
}
