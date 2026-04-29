using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InsertionPointHandler : MonoBehaviour
{
    public Recorder recorder;
    private GameObject syringe;
    public GameObject syringeLeft;
    public GameObject syringeRight;
    public Material successMaterial;

    void Start()
    {
        recorder = FindObjectOfType<Recorder>();
        if (recorder.leftHanded)
            syringe = syringeLeft;
        else
            syringe = syringeRight;

    }

    private void OnTriggerEnter(Collider other)
    {
       
        if(other.gameObject.tag == "Pointer" && syringe.activeSelf)
        {
            GetComponent<Renderer>().material = successMaterial;
            //Invoke("StopDisplayAngle", 2f);

        }
    }
}
