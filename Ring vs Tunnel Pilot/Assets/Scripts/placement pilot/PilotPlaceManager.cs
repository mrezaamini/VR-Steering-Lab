using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class PilotPlaceManager : MonoBehaviour
{
    [Header("Participant Info")]
    public int participantID;
    public bool shoulderBreadth; // true: long, false: short
    public bool isRightHanded;

    [Header("Game Objects")]

    public GameObject wirePrefab;
    [SerializeField] private List<GameObject> ringPrefabs;
    private Vector3 targetPosition;

    


    private string trackingOutputFile;
    private float trialW;
    private float trialL;
    private Quaternion trialR;

    public int currentTrial = 0;

    [SerializeField] private List<GameObject> wires;

    private GameObject currentWire;
    private GameObject currentRing;

    private bool isTraversingWire = false;

    private List<Vector2> indexOfDiffs = new List<Vector2> // L (wire), W (ring diameter), wire diameter is fixed to 0.01 m
    {
        new Vector2(0.20f, 0.04f),
        new Vector2(0.20f, 0.08f),
        new Vector2(0.30f, 0.04f),
        new Vector2(0.30f, 0.08f)
    };

    private List<Quaternion> wireRotations = new List<Quaternion> { 
        // main axes
        Quaternion.Euler(0, 0, 0),
        Quaternion.Euler(0, 0, 90),
        Quaternion.Euler(0, 0, 180),
        Quaternion.Euler(0, 0, 270),
        Quaternion.Euler(90, 0, 0),
        Quaternion.Euler(270, 0, 0),
    };

    private List<float> lateralOffsets = new List<float> { -0.2f, 0.0f, 0.2f };
    private Vector3 scenePosition;

    //private GameObject sphere;
    private GameObject mainCamera;

    private bool calibrationStatus = true; //IT SHOULD BE FALSE
    public GameObject startButton;


    private List<(Vector2, Quaternion)> participantTrials;

    // Start is called before the first frame update
    void Start()
    {
        mainCamera = Camera.main.gameObject;
        //sphere = GameObject.CreatePrimitive(PrimitiveType.Cube);
        //sphere.transform.localScale = new Vector3(0.01f, 0.01f, 0.25f);
        participantTrials = GenerateParticipantTrial();

    }

    // Update is called once per frame
    void Update()
    {
        if (!calibrationStatus)
        {
            return;
        }
        if (isTraversingWire)
        {
            OnTraversingTracking();
        }

    }

    public void startExperiment()
    {
        Invoke("CalbrationSetup", 0.5f);
        
    }

    void CalbrationSetup()
    {
        startButton.SetActive(false);
        Vector3 cameraForward = mainCamera.transform.forward;
        Vector3 startPos = new Vector3(0f, mainCamera.transform.position.y, mainCamera.transform.position.z);
        Vector3 scenePosition = startPos + cameraForward * 0.4f;
        Debug.Log(startPos);
        calibrationStatus = true;
        //sphere.transform.position = scenePosition;
        targetPosition = scenePosition;
    }

    public List<(Vector2, Quaternion)> GenerateParticipantTrial() 
    {
        
        List<(Vector2, Quaternion)> trials = new List<(Vector2, Quaternion)>(); // list of IDs and Rotations
        foreach (Vector2 id in indexOfDiffs)
        {
            foreach (Quaternion rotation in wireRotations)
            {
                trials.Add((id, rotation));
            }
        }

        return trials;
    }


    public void NextTrial()
    {
        if (currentTrial >= participantTrials.Count)
        {
            Debug.Log("All trials completed for participant.");
            return;
        }

        //Decompose trial condition
        (Vector2 id, Quaternion rotation) = participantTrials[currentTrial];
        float len = id.x;
        float width = id.y;

        // create wire
        currentWire = Instantiate(wirePrefab, targetPosition, rotation);
        currentWire.transform.localScale = new Vector3(0.01f, len, 0.01f);

        //create ring
        Vector3 wireForward = currentWire.transform.up;
        float ringOffset = len + 0.05f;
        Vector3 ringPosition = targetPosition - ringOffset * wireForward;
        currentRing = Instantiate(SelectRingPrefab(width), ringPosition, rotation);
        currentRing.transform.forward = currentWire.transform.up; // to overcome problem regarding orientation of the ring-to be prependicular to wire
        Debug.Log($"Trial {currentTrial + 1} started: L = {len}, W = {width}, Rotation = {rotation.eulerAngles}");

        //saving tracking information as output for each trial
        string trackingOutputPath = Path.Combine(Application.dataPath, "CapturedData");
        string trackingOutputName = $"P{participantID}_T{currentTrial + 1}_wireTrack.csv";
        trackingOutputFile = Path.Combine(trackingOutputPath, trackingOutputName);
        if (!Directory.Exists(trackingOutputPath))
        {
            Debug.Log("Directory Not Found!! created new one");
            Directory.CreateDirectory(trackingOutputPath);
        }
        if (!File.Exists(trackingOutputFile))
        {
            File.WriteAllText(trackingOutputFile, "PID,rightHanded,width,length,rotationX,rotationY,rotationZ,PositionX,PositionY\n");
        }
        else
        {
            Debug.Log("WARNING: file already exists, overwritting!");
        }
        //update trial info for saving tracking info in output file
        trialL = len;
        trialR = rotation;
        trialW = width;
    }

    GameObject SelectRingPrefab(float W)
    {
        GameObject selectedRingPrefab = null;

        switch (W)
        {
            case 0.04f:
                selectedRingPrefab = ringPrefabs[0];
                break;
            case 0.08f:
                selectedRingPrefab = ringPrefabs[1];
                break;
            default:
                Debug.LogError("No ring prefab for W: " + W);
                break;
        }

        return selectedRingPrefab;
    }


    private Transform GetWireStartPoint()
    {
        Transform[] children = currentWire.GetComponentsInChildren<Transform>(true);
        Transform startPoint = null;

        foreach (Transform child in children)
        {
            if (child.CompareTag("StartPoint"))
            {
                startPoint = child;
                break;
            }
        }
        if (startPoint == null)
        {
            Debug.Log("No child with the specified tag found.");
        }
        return startPoint;
    }


    private void OnTraversingTracking()
    {
        Vector3 ringPlaneNormal = currentRing.transform.forward;
        Vector3 ringCenter = currentRing.transform.position;

        Vector3 wireRayStartPos = GetWireStartPoint().position;
        Ray wireRay = new Ray(wireRayStartPos, currentWire.transform.up);
        Plane ringPlane = new Plane(ringPlaneNormal, ringCenter);
        Vector3 intersectionPoint;
        if (ringPlane.Raycast(wireRay, out float intr))
        {
            intersectionPoint = wireRay.GetPoint(intr);
            Vector3 localIntersection = intersectionPoint - ringCenter;
            float x = Vector3.Dot(localIntersection, currentRing.transform.right);
            float y = Vector3.Dot(localIntersection, currentRing.transform.up);

            //debugText.updateText("x: " + x + " / y: " + y);
            //saving tracking info to file
            SaveWireTrack(x, y);
        }
        else
        {
            //debugText.updateText("no intersection found");
        }

    }

    private void SaveWireTrack(float x, float y) // add gender (shoulder breadth) to info trackings
    {
        string newData = $"{participantID},{isRightHanded},{trialW},{trialL},{trialR.x},{trialR.y},{trialR.z},{x},{y}\n";
        File.AppendAllText(trackingOutputFile, newData);
    }

}
