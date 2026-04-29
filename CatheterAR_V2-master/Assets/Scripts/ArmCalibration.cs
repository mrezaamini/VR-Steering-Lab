//using System.Collections;
//using System.Collections.Generic;
//using TMPro;
//using Unity.Android.Types;
//using UnityEngine;

//public class ArmCalibration : MonoBehaviour
//{
//    // Start is called before the first frame update
//    private int numBalls;

//    [SerializeField] GameObject calibrationBall;
//    [SerializeField] GameObject insertionZonePrefab;

//    public GameObject pointerPosition;
//    public GameObject pointerPositionRight;
//    public GameObject pointerPositionLeft;

//    public GameObject syringe;
//    public GameObject syringeRight;
//    public GameObject syringeLeft;

//    public GameObject controllerVisual;
//    public GameObject controllerVisualRight;
//    public GameObject controllerVisualLeft;

//    public OVRInput.Controller controller;
//    public GameObject insertionZone;

//    public float triggerVal;
//    public float startTime;

//    public GameObject[] allBalls;

//    public ControllerSyringe controllerSyringe;

//    public GameObject armPrefab;

//    public Transform HMDCamera;

//    public BoxCollider selectionCollider;
//    public BoxCollider recorderCollider;

//    public Recorder recorder;
//    public SelectionCollider sCollider;

//    public GameObject left;
//    public GameObject right;

//    public TMP_Text status;


//    void Start()
//    {
//        recorder = FindObjectOfType<Recorder>();
//        if (recorder.leftHanded)
//        {
//            triggerVal = OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger, OVRInput.Controller.LTouch);
//            pointerPosition = pointerPositionLeft;
//            syringe = syringeLeft;
//            controllerVisual = controllerVisualLeft;
//            right.SetActive(false);
//            left.SetActive(true);
//        }
//        else
//        {
//            triggerVal = OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger, OVRInput.Controller.RTouch);
//            pointerPosition = pointerPositionRight;
//            syringe = syringeRight;
//            controllerVisual = controllerVisualRight;
//            right.SetActive(true);
//            left.SetActive(false);
//        }


//        numBalls = 0;
//        startTime = Time.time;
//        allBalls = new GameObject[6];
//        controllerSyringe = FindObjectOfType<ControllerSyringe>();

//    }

//    void Update()
//    {
//        if (recorder.leftHanded)
//        {
//            triggerVal = OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger, OVRInput.Controller.LTouch);
//        }
//        else
//        {
//            triggerVal = OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger, OVRInput.Controller.RTouch);
//        }

//        if (triggerVal == 1 && numBalls < 6 && Time.time - startTime > 0.5f)
//        {
//            startTime = Time.time;
//            InstantiateBalls();
//        }
//    }

//    public void InstantiateBalls()
//    {
//        GameObject ball = Instantiate(calibrationBall, pointerPosition.transform.position, Quaternion.identity);
//        allBalls[numBalls] = ball;
//        recorder.calibrationPoints[numBalls] = ball.transform;
//        numBalls++;
//        if(numBalls == 6)
//        {
//            Vector3 position = (allBalls[0].transform.position + allBalls[1].transform.position) / 2f;
//            GameObject armInstance = Instantiate(armPrefab, new Vector3(position.x - 0.0108273802f - 0.0058815106f, position.y + 0.172167627f - 0.007f, position.z + 0.17191037f - 0.04f), Quaternion.Euler(9.99981499f, 188.761063f, 359.4086f));
//            InstantiateInsertionZone();
//            recorder.sphereInsert = armInstance.GetComponentInChildren<SphereInsert>();
//            StartText("Training started");
//            Invoke("StopText", 4f);
//        }
//    }

//    public void InstantiateInsertionZone()
//    {
//        Vector3 insertPostion = (allBalls[2].transform.position + allBalls[3].transform.position) / 2f;

//        insertionZone = GameObject.FindGameObjectWithTag("InsertionZone");
//        recorder.target = insertionZone.transform;
//        selectionCollider.transform.position = new Vector3(insertionZone.transform.position.x, insertionZone.transform.position.y, insertionZone.transform.position.z + 0.501f);
//        recorderCollider.transform.position = new Vector3(insertionZone.transform.position.x, insertionZone.transform.position.y, insertionZone.transform.position.z);
//        recorderCollider.transform.LookAt(HMDCamera);
//        numBalls++;
//        controllerSyringe.SetSyringe(pointerPosition, syringe, controllerVisual, recorder.leftHanded);
//    }

//    public void StartText(string text)
//    {
//        status.text = text;
//    }

//    public void StopText()
//    {
//        status.text = string.Empty;
//    }
//}

using TMPro;
using UnityEngine;

public class ArmCalibration : MonoBehaviour
{
    private int numBalls;
    public bool CalibrationDone;
    [SerializeField] GameObject calibrationBall;
    [SerializeField] GameObject insertionZonePrefab;

    public GameObject pointerPosition;
    public GameObject pointerPositionRight;
    public GameObject pointerPositionLeft;

    public GameObject syringe;
    public GameObject syringeRight;
    public GameObject syringeLeft;

    public Transform insertionZone;   // the InsertionZone child
   
    public Transform armRoot;

    public GameObject controllerVisual;
    public GameObject controllerVisualRight;
    public GameObject controllerVisualLeft;


    public float triggerVal;
    public float startTime;

    public GameObject[] allBalls;

    public ControllerSyringe controllerSyringe;
    public GameObject armPrefab;

    public Transform HMDCamera;

    public BoxCollider selectionCollider;
    public BoxCollider recorderCollider;

    public Recorder recorder;

    public GameObject left;
    public GameObject right;

    public TMP_Text status;
    void Start()
    {
        recorder = FindObjectOfType<Recorder>();
        CalibrationDone = false;

        if (recorder.leftHanded)
        {
            pointerPosition = pointerPositionLeft;
            syringe = syringeLeft;
            controllerVisual = controllerVisualLeft;
            right.SetActive(false);
            left.SetActive(true);
        }
        else
        {
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
            triggerVal = OVRInput.Get(
                OVRInput.Axis1D.PrimaryIndexTrigger,
                OVRInput.Controller.LTouch
            );
        }
        else
        {
            triggerVal = OVRInput.Get(
                OVRInput.Axis1D.PrimaryIndexTrigger,
                OVRInput.Controller.RTouch
            );
        }

        if (triggerVal == 1 && numBalls < 6 && Time.time - startTime > 0.5f)
        {
            startTime = Time.time;
            InstantiateBalls();
        }
        
    }

    public void InstantiateBalls()
    {
        GameObject ball = Instantiate(
            calibrationBall,
            pointerPosition.transform.position,
            Quaternion.identity
        );

        allBalls[numBalls] = ball;
        recorder.calibrationPoints[numBalls] = ball.transform;

        numBalls++;

        if (numBalls == 6)
        {
            GenerateArmModel();
        }
    }

    private void GenerateArmModel()
    {
        GameObject armInstance = Instantiate(
            armPrefab,
            Vector3.zero,
            Quaternion.identity
        );

        AlignArmToCalibration(armInstance);

        

        InstantiateInsertionZone(armInstance.transform);

        //recorder.sphereInsert = armInstance.GetComponentInChildren<SphereInsert>();
        CalibrationDone = true;
        recorder.StartNextTrial();
        StartText("Training started");
        Invoke(nameof(StopText), 4f);

    }

    

    private void AlignArmToCalibration(GameObject armInstance)
    {
        Transform armpitRef = armInstance.transform.Find("ArmpitRef");
        Transform wristRef = armInstance.transform.Find("WristRef");
        Transform elbowRef = armInstance.transform.Find("ElbowRef");

        Vector3 targetArmpit =
            (allBalls[0].transform.position + allBalls[1].transform.position) / 2f;

        Vector3 targetWrist =
            (allBalls[4].transform.position + allBalls[5].transform.position) / 2f;

        Vector3 targetElbow =
            (allBalls[2].transform.position + allBalls[3].transform.position) / 2f;

        // Move armpit into place
        armInstance.transform.position += targetArmpit - armpitRef.position;

        // Prefab directions
        Vector3 prefabForward =
            (wristRef.position - armpitRef.position).normalized;

        Vector3 prefabUp =
            (elbowRef.position - armpitRef.position).normalized;

        // Real directions
        Vector3 targetForward =
            (targetWrist - targetArmpit).normalized;

        Vector3 targetUp =
            (targetElbow - targetArmpit).normalized;

        // Construct rotations
        Quaternion prefabRotation =
            Quaternion.LookRotation(prefabForward, prefabUp);

        Quaternion targetRotation =
            Quaternion.LookRotation(targetForward, targetUp);

        // Final alignment
        Quaternion delta =
            targetRotation * Quaternion.Inverse(prefabRotation);

        Vector3 pivot = armpitRef.position;

        armInstance.transform.position =
            pivot + delta * (armInstance.transform.position - pivot);

        armInstance.transform.rotation =
            delta * armInstance.transform.rotation;

        // Final snap correction
        armInstance.transform.position += targetArmpit - armpitRef.position;
    }

    public void InstantiateInsertionZone(Transform armInstance)
    {
        //Vector3 wristCenter =
        //    (allBalls[2].transform.position + allBalls[3].transform.position) / 2f;

        //insertionZone = GameObject.FindGameObjectWithTag("InsertionZone");

        //if (insertionZone != null)
        //{
        //    insertionZone.transform.position = wristCenter;

        //    recorder.target = insertionZone.transform;

        //    selectionCollider.transform.position = new Vector3(
        //        insertionZone.transform.position.x,
        //        insertionZone.transform.position.y,
        //        insertionZone.transform.position.z + 0.501f
        //    );

        //    recorderCollider.transform.position = insertionZone.transform.position;
        //    recorderCollider.transform.LookAt(HMDCamera);
        //}

        //set the guidance 
        insertionZone = armInstance.Find("InsertionZone");
        

        if (insertionZone == null)
            Debug.LogError("[ArmTunnelSetup] InsertionZone not found on arm prefab.");
        
        controllerSyringe.SetSyringe(
            pointerPosition,
            syringe,
            controllerVisual,
            recorder.leftHanded
        );

        recorder.ballTransform = recorder.pointer;
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