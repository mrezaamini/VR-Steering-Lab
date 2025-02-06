using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class GameManager : MonoBehaviour // GAME MANAGER FOR PLACEMENT PILOT STUDY, REMEMBER TO PUT THE UPDATED VERSION BACK INTO THE SOURCE FILE!!!!!!
{
    [Header("Participant Info")]
    public int participantID;
    public bool isMale; // true: long, false: short for shoulder breadth
    public bool isRightHanded;

    [Header("Game Objects")]

    public GameObject wirePrefab;
    [SerializeField] private List<GameObject> ringPrefabs;
    private Vector3 targetPosition;
    public AudioClip success_sound;


    public DebugText debugText;



    private string trackingOutputFile;
    private float trialW;
    private float trialL;
    private Quaternion trialR;

    public int currentTrial = 0;

    [SerializeField] private List<GameObject> wires;

    private GameObject currentWire;
    private GameObject currentRing;
    private Vector3 currentRing_position_init = new Vector3(0.0f, 0.0f, 0.0f); // used for traversing status (start, end) calculations (PLAN B, since fast movement may not be catched by the collider itself. As no rotation is included in this user study, this approach can be useful)

    private bool isTraversingWire = false;

    private List<Vector2> indexOfDiffs = new List<Vector2> // L (wire), W (ring diameter), wire diameter is fixed to 0.01 m
    {
       new Vector2(0.30f, 0.04f),
        new Vector2(0.35f, 0.04f),
        new Vector2(0.40f, 0.04f),
        new Vector2(0.45f, 0.04f)
    };

    private List<Quaternion> wireRotations = new List<Quaternion> { 
        // main axes
        Quaternion.Euler(0, 0, 0),
        Quaternion.Euler(0, 0, 90),
        //Quaternion.Euler(0, 0, 180),
        //Quaternion.Euler(0, 0, 270),
        Quaternion.Euler(90, 0, 0),
        //Quaternion.Euler(270, 0, 0),
    };

    // target placement attributes
    private float offset_lateral = 0.0f;
    private float offset_depth = 0.35f;
    private float offset_height = 0.2f;
    private float mainHand; // multiplier of the lateral offset to adjust dominant hand position
    private Vector3 scenePosition;

    float heightOffset = 0.2f;

    //private GameObject sphere;
    private GameObject mainCamera;

    private bool calibrationStatus = false; //should be false at start
    public GameObject startButton;


    private List<(Vector2, Quaternion)> participantTrials;

    //hand material swap
    [SerializeField] private Material invisibleHand_material;
    [SerializeField] private Material originalHand_material;
    [SerializeField] private GameObject rightHandObject;
    [SerializeField] private GameObject leftHandObject;
    private SkinnedMeshRenderer mainHand_skin;



    // Start is called before the first frame update
    void Start()
    {
        mainCamera = Camera.main.gameObject;
        mainHand = 1f;
        if (!isRightHanded)
        {
            mainHand = -1f;
        }

        participantTrials = GenerateParticipantTrial();

        //ONLY FOR HOME DEV PURPOSES
        //startExperiment();
    }

    // Update is called once per frame
    void Update()
    {
        if (!calibrationStatus)
        {
            return;
        }

        if (!isTraversingWire & currentRing != null)
        {
            float forward_displacement = Mathf.Abs(Vector3.Dot(currentRing.transform.position - currentRing_position_init, currentWire.transform.up)); // calculating displacement for starting traverse status
            // Check if movement along the forward vector is greater than 0.2 units
            if (forward_displacement > 0.025f) //0.2 is the init offset in NextTrial(), and 0.005 for the ring thickness/2
            {
                OnStartTraversing();
            }
        }

        if (isTraversingWire)
        {
            float forward_displacement = Mathf.Abs(Vector3.Dot(currentRing.transform.position - currentRing_position_init, currentWire.transform.up)); // calculating displacement for ending traverse status
            // Check if movement along the forward vector is greater than 0.2 units
            if (forward_displacement > currentWire.transform.localScale.y * 2 + 0.005f) //*2 since the prefab is already x2 long (see create wire in NextTrial()). 0.005 is because of the ring thickness (=1) and ring plane is considered as the center of it 
            {
                EndTrial();
            }
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
        scenePosition = startPos + cameraForward * offset_depth;
        Debug.Log(startPos);
        calibrationStatus = true;
        NextTrial();
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

        // adjusting center of placements
        targetPosition = new Vector3(scenePosition.x + (mainHand * offset_lateral), scenePosition.y - offset_height, scenePosition.z);

        // update debug text
        debugText.updateText("length: " + len);

        // create wire
        currentWire = Instantiate(wirePrefab, targetPosition, rotation);
        currentWire.transform.localScale = new Vector3(0.01f, len / 2, 0.01f); // len/2 because the prefab is already 2 units long

        //create ring
        Vector3 wireForward = currentWire.transform.up;
        float ringOffset = len / 2 + 0.02f;
        Vector3 ringPosition = targetPosition - ringOffset * wireForward;
        currentRing_position_init = ringPosition; // saving ring start point for PLAN B of traverse status calculation
        currentRing = Instantiate(SelectRingPrefab(width), ringPosition, rotation);
        currentRing.transform.forward = currentWire.transform.up; // to overcome problem regarding orientation of the ring-to be prependicular to wire
        Debug.Log($"Trial {currentTrial + 1} started: P= {mainHand} L = {len}, W = {width}, Rotation = {rotation.eulerAngles}");

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
        //Rigidbody ring_rb = currentRing.GetComponent<Rigidbody>();
        //ring_rb.constraints = RigidbodyConstraints.FreezeRotation;
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

    public void EndTrial() // to end a trial and move to the next one
    {

        isTraversingWire = false;
        // destroy previous trial objects
        if (currentRing != null) Destroy(currentRing);
        if (currentWire != null) Destroy(currentWire);

        Debug.Log("OBJ deleted");

        currentTrial++;

        if (isRightHanded)
        {
            rightHandObject.GetComponent<SkinnedMeshRenderer>().material = originalHand_material;
        }
        else
        {
            leftHandObject.GetComponent<SkinnedMeshRenderer>().material = originalHand_material;
        }

        //play success sound
        AudioSource.PlayClipAtPoint(success_sound, Camera.main.transform.position);

        NextTrial();

    }

    public void OnStartTraversing()
    {
        Debug.Log("traversing started");

        isTraversingWire = true;
        if (isRightHanded)
        {
            rightHandObject.GetComponent<SkinnedMeshRenderer>().material = invisibleHand_material;
        }
        else
        {
            leftHandObject.GetComponent<SkinnedMeshRenderer>().material = invisibleHand_material;
        }

    }

}
