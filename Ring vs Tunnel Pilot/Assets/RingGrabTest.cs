using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RingGrabTest : MonoBehaviour
{
    public Transform target;
    Rigidbody rb;
    void Start()
    {
        rb = GetComponent<Rigidbody>(); 
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if(rb == null)
        {
            Debug.Log("Fuck");
        }
        rb.MovePosition(target.transform.position);
       
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        Debug.Log(rb.velocity);
    }
}
