using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RingContainerHandler : MonoBehaviour
{
    public Transform childObject; 

    void Update()
    {
        if (childObject != null)
        {
            transform.position = childObject.position; // Lock parent to child's position
        }
    }
}
