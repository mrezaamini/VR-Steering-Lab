using Oculus.Interaction;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEditor.Search;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class SphereInsert : MonoBehaviour
{
    // Start is called before the first frame update

    public Material blue;
    public Material green;
    public Material orange;
    public Material red;

    public MeshRenderer meshRenderer;
    public bool inside;

    public Recorder recorder;

    public GameObject[] visualGuides;
    public VisualGuideTriggers[] visualTrigger;

    public GameObject pointer;
    public TMP_Text debugText;
    public MeshRenderer pointerCollider;

    public float distance;
    private void Start()
    {
        pointer = GameObject.FindGameObjectWithTag("Pointer");
        pointerCollider = pointer.GetComponent<MeshRenderer>();
        inside = false;

        recorder = FindObjectOfType<Recorder>();
        if(recorder.assistanceActivated == false)
        {
            foreach (GameObject guides in visualGuides)
            {
                guides.SetActive(false);
            }
        }
    }

    public void ResetVisualGuides()
    {
        for(int i = 0; i < visualGuides.Length; i++)
        {
            visualTrigger[i].collided = false;
            visualGuides[i].SetActive(true);
        }
    }

    private void Update()
    {
        for (int i = 2; i >= 0; i--)
        {
            if(i == 0 && inside)
            {
                visualGuides[0].SetActive(false);
            }
            if (visualTrigger[i].collided)
            {
                visualGuides[i + 1].SetActive(false);
            }
        }

        distance = Vector3.Distance(pointer.transform.position, this.transform.position);

        if (distance < 0.0045f)
        {
            recorder.isInside = true;
            inside = true;
            meshRenderer.material = green;
        }
        else
        {
            meshRenderer.material = orange;
            inside = false;
            recorder.isInside = false;
        }
    }
}
