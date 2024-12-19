using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SteeringStatus : MonoBehaviour
{
    public GameManager gameManager;
    private void OnTriggerEnter(Collider other)
    {
        if (other.transform.parent == transform.parent)
        {
            return;
        }
        if (other.gameObject.CompareTag("StartPoint"))
        {
            gameManager.OnStartTraversing();
        }
        if (other.gameObject.CompareTag("EndPoint"))
        {
            gameManager.OnFinishTraversing();
        }

        

     
            //Debug.Log("otherr collider " + other.gameObject.tag);
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
