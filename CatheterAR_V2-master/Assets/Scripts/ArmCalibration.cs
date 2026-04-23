using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Android.Types;
using UnityEngine;

public class ArmCalibration : MonoBehaviour
{
    // Start is called before the first frame update
    private int numBalls;

    [SerializeField] GameObject calibrationBall;
    [SerializeField] GameObject insertionZonePrefab;

    public GameObject pointerPosition;
    public GameObject pointerPositionRight;
    public GameObject pointerPositionLeft;

    public GameObject syringe;
    public GameObject syringeRight;
    public GameObject syringeLeft;

    public GameObject controllerVisual;
    public GameObject controllerVisualRight;
    public GameObject controllerVisualLeft;

    public OVRInput.Controller controller;
    public GameObject insertionZone;

    public float triggerVal;
    public float startTime;

    public GameObject[] allBalls;

    public ControllerSyringe controllerSyringe;

    public GameObject armPrefab;

    public Transform HMDCamera;

    public BoxCollider selectionCollider;
    public BoxCollider recorderCollider;

    public Recorder recorder;
    public SelectionCollider sCollider;

    public GameObject left;
    public GameObject right;

    public TMP_Text status;


    void Start()
    {
        recorder = FindObjectOfType<Recorder>();
        if (recorder.leftHanded)
        {
            triggerVal = OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger, OVRInput.Controller.LTouch);
            pointerPosition = pointerPositionLeft;
            syringe = syringeLeft;
            controllerVisual = controllerVisualLeft;
            right.SetActive(false);
            left.SetActive(true);
        }
        else
        {
            triggerVal = OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger, OVRInput.Controller.RTouch);
            pointerPosition = pointerPositionRight;
            syringe = syringeRight;
            controllerVisual = controllerVisualRight;
            right.SetActive(true);
            left.SetActive(false);
        }
            

        numBalls = 0;
        startTime = Time.time;
        allBalls = new GameObject[6];
        controllerSyringe = FindObjectOfType<ControllerSyringe>();
        
    }

    void Update()
    {
        if (recorder.leftHanded)
        {
            triggerVal = OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger, OVRInput.Controller.LTouch);
        }
        else
        {
            triggerVal = OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger, OVRInput.Controller.RTouch);
        }

        if (triggerVal == 1 && numBalls < 6 && Time.time - startTime > 0.5f)
        {
            startTime = Time.time;
            InstantiateBalls();
        }
    }

    public void InstantiateBalls()
    {
        GameObject ball = Instantiate(calibrationBall, pointerPosition.transform.position, Quaternion.identity);
        allBalls[numBalls] = ball;
        recorder.calibrationPoints[numBalls] = ball.transform;
        numBalls++;
        if(numBalls == 6)
        {
            Vector3 position = (allBalls[0].transform.position + allBalls[1].transform.position) / 2f;
            GameObject armInstance = Instantiate(armPrefab, new Vector3(position.x - 0.0108273802f - 0.0058815106f, position.y + 0.172167627f - 0.007f, position.z + 0.17191037f - 0.04f), Quaternion.Euler(9.99981499f, 188.761063f, 359.4086f));
            InstantiateInsertionZone();
            recorder.sphereInsert = armInstance.GetComponentInChildren<SphereInsert>();
            StartText("Training started");
            Invoke("StopText", 4f);
        }
    }

    public void InstantiateInsertionZone()
    {
        Vector3 insertPostion = (allBalls[2].transform.position + allBalls[3].transform.position) / 2f;
        
        insertionZone = GameObject.FindGameObjectWithTag("InsertionZone");
        recorder.target = insertionZone.transform;
        selectionCollider.transform.position = new Vector3(insertionZone.transform.position.x, insertionZone.transform.position.y, insertionZone.transform.position.z + 0.501f);
        recorderCollider.transform.position = new Vector3(insertionZone.transform.position.x, insertionZone.transform.position.y, insertionZone.transform.position.z);
        recorderCollider.transform.LookAt(HMDCamera);
        numBalls++;
        controllerSyringe.SetSyringe(pointerPosition, syringe, controllerVisual, recorder.leftHanded);
    }

    public void StartText(string text)
    {
        status.text = text;
    }

    public void StopText()
    {
        status.text = string.Empty;
    }
}
