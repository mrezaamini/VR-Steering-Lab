using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class SelectionCollider : MonoBehaviour
{
    // Start is called before the first frame update

    public Recorder recorder;
    public SphereInsert sphereInsert;
    public GameObject syringe;
    public GameObject syringeRight;
    public GameObject syringeLeft;

    public TMP_Text text;

    public string angleX;
    public string angleY;
    public AudioSource audioSuccess;
    public AudioSource audioFailure;

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
        sphereInsert = FindObjectOfType<SphereInsert>();
        if(other.gameObject.tag == "Pointer" && syringe.activeSelf)
        {
            if(sphereInsert != null)
            {
                syringe.SetActive(false);
                recorder.isInside = sphereInsert.inside;
                recorder.summaryWriter();
                if(recorder.isInside)
                    audioSuccess.Play(1);
                else 
                    audioFailure.Play(1);
                DisplayAngle();
                Invoke("StopDisplayAngle", 2f);
            }
        }
    }

    private void DisplayAngle()
    {
        angleX = syringe.transform.eulerAngles.x.ToString();
        angleY = syringe.transform .eulerAngles.y.ToString();

        text.text = "Angle: " + "x: " + angleX + ", y: " + angleY;
    }

    private void StopDisplayAngle()
    {
        text.text = "";
    }
}
