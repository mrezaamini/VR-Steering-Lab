using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

public class ActivateVisualGuides : MonoBehaviour
{
    // Start is called before the first frame update

    public Recorder recorder;
    public GameObject[] visualGuides;
    private void Start()
    {
        recorder = FindObjectOfType<Recorder>();
        //recorder.SetVisualGuides();
    }

    public void SetActivation(bool activated)
    {
        if (activated)
        {
            foreach(GameObject guide in visualGuides)
            {
                guide.GetComponent<MeshRenderer>().enabled = true;
            }
        }
        else
        {
            foreach (GameObject guide in visualGuides)
            {
                guide.GetComponent<MeshRenderer>().enabled = false;
            }
        }
    }
}
