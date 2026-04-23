using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RecorderCollider : MonoBehaviour
{
    public Recorder recorder;
    public GameObject syringe;
    public GameObject syringeRight;
    public GameObject syringeLeft;
    public SphereInsert sphereInsert;
    // Start is called before the first frame update
    void Start()
    {
       // sphereInsert = GetComponent<SphereInsert>();
        recorder = FindObjectOfType<Recorder>();
        if (recorder.leftHanded)
            syringe = syringeLeft;
        else
            syringe = syringeRight;
    }


    private void OnTriggerEnter(Collider other)
    {
        if(sphereInsert == null)
        {
            sphereInsert = FindObjectOfType<SphereInsert>();
        }
        if (other.gameObject.tag == "Pointer")
        {
            recorder.inCollider = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if(other.gameObject.tag == "Pointer")
        {
            recorder.inCollider = false;
            syringe.SetActive(true);
            sphereInsert.ResetVisualGuides();

        }
    }
}
