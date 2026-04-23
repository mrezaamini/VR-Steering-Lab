using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VisualGuideTriggers : MonoBehaviour
{
    public bool collided;
    public Recorder recorder;
    private void OnTriggerEnter(Collider other)
    {
        if(other.gameObject.tag == "Pointer")
        {
            collided = true;
        }
    }
}
