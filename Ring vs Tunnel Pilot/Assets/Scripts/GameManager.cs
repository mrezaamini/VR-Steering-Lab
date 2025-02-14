using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class GameManager : MonoBehaviour // GAME MANAGER FOR PLACEMENT PILOT STUDY, REMEMBER TO PUT THE UPDATED VERSION BACK INTO THE SOURCE FILE!!!!!!
{
    [Header("Participant Info")]
    public int participantID; // starts from 0
    public bool isMale; // true: long, false: short for shoulder breadth
    public bool isRightHanded;

    [Header("Ring and Wire")]
    public GameObject wirePrefab;
    [SerializeField] private List<GameObject> ringPrefabs;

    [Header("Ball and Tunnel")]
    public GameObject ballPrefab;
    [SerializeField] private GameObject tunnelPrefab;

    [Header("UI")]
    public AudioClip success_sound;



    private Vector3 targetPosition;
    public DebugText debugText;
    private string trackingOutputFile;
    
    //current task info 
    private float trialW;
    private float trialL;
    private Quaternion trialR;
    private bool trialTask; // true is ring and wire, false is ball and tunnel
    private int tryCounter; // tries until successful steering. starts from 0
    private int trialRep; // to indicate which repetition this current trial is. starts from 0
    public int currentTrial = 0;
    private GameObject currentPath;
    private GameObject currentTarget;


    //private GameObject currentPath;
    //private GameObject currentTarget;
    private Vector3 currentTarget_position_init = new Vector3(0.0f, 0.0f, 0.0f); // used for traversing status (start, end) calculations (PLAN B, since fast movement may not be catched by the collider itself. As no rotation is included in this user study, this approach can be useful)
    private bool isSteering = false;



    // experiment conditions
    private List<(bool, float, float)> exp_conditions = new List<(bool, float, float)> // task dificulty (true: ring, false: tunnel), L, W
    {
        (true, 0.20f, 0.02f),
        (true, 0.20f, 0.04f),
        (true, 0.20f, 0.08f),
        (true, 0.40f, 0.02f),
        (true, 0.40f, 0.04f),
        (true, 0.40f, 0.08f),
        (false, 0.20f, 0.02f),
        (false, 0.20f, 0.04f),
        (false, 0.20f, 0.08f),
        (false, 0.40f, 0.02f),
        (false, 0.40f, 0.04f),
        (false, 0.40f, 0.08f)
    };

    private List<Quaternion> pathRotations = new List<Quaternion> { 
         // z-plane
        Quaternion.Euler(0, 0, 0),
        //Quaternion.Euler(0, 0, 45),
        //Quaternion.Euler(0, 0, 90),
        //Quaternion.Euler(0, 0, 135),
        //Quaternion.Euler(0, 0, 180),
        //Quaternion.Euler(0, 0, 225),
        //Quaternion.Euler(0, 0, 270),
        //Quaternion.Euler(0, 0, 315),
        //// x-plane
        //Quaternion.Euler(45, 0, 0),
        //Quaternion.Euler(90, 0, 0),
        //Quaternion.Euler(135, 0, 0),
        //Quaternion.Euler(225, 0, 0),
        //Quaternion.Euler(270, 0, 0),
        //Quaternion.Euler(315, 0, 0),
        //// y-plane
        //Quaternion.Euler(0, 45, 90),
        //Quaternion.Euler(0, 135, 90),
        //Quaternion.Euler(0, 225, 90),
        //Quaternion.Euler(0, 315, 90),
        //// 3d-diagonal up
        //Quaternion.Euler(0, 45, 45),
        //Quaternion.Euler(0, 135, 45),
        //Quaternion.Euler(0, 225, 45),
        //Quaternion.Euler(0, 315, 45),
        //// 3d-diagonal down
        //Quaternion.Euler(0, 45, 135),
        //Quaternion.Euler(0, 135, 135),
        //Quaternion.Euler(0, 225, 135),
        //Quaternion.Euler(0, 315, 135)
    };

    // target placement attributes
    private float offset_lateral = 0.0f;
    private float offset_depth = 0.35f;
    private float offset_height = 0.15f;
    private float mainHand; // multiplier of the lateral offset to adjust dominant hand position
    private Vector3 scenePosition;

    private GameObject mainCamera;
    private bool calibrationStatus = false; //should be false at start
    public GameObject startButton;


    private List<(bool, Vector2, Quaternion)> participantTrials; // task type (true: ring, false: tunnel), ID, rotation

    //hand materials
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

        participantTrials = GenerateParticipantTrial(participantID);
        Debug.Log("trialCount: " + participantTrials.Count);
        startExperiment();
    }

    // Update is called once per frame
    void Update()
    {
        if (!calibrationStatus)
        {
            return;
        }

        if (!isSteering & currentTarget != null)
        {
            float forward_displacement = Mathf.Abs(Vector3.Dot(currentTarget.transform.position - currentTarget_position_init, currentPath.transform.up)); // calculating displacement for starting traverse status
            // Check if movement along the forward vector is greater than 0.2 units
            if (forward_displacement > 0.025f) //0.2 is the init offset in NextTrial(), and 0.005 for the target thickness/2
            {
                OnStartTraversing();
            }
        }

        if (isSteering)
        {
            float forward_displacement = Mathf.Abs(Vector3.Dot(currentTarget.transform.position - currentTarget_position_init, currentPath.transform.up)); // calculating displacement for ending traverse status
            // Check if movement along the forward vector is greater than 0.2 units
            if (forward_displacement > currentPath.transform.localScale.y * 2 + 0.005f) //*2 since the prefab is already x2 long (see create wire in NextTrial()). 0.005 is because of the ring thickness (=1) and ring plane is considered as the center of it 
            {
                EndTrial();
            }
            else
            {
                OnTraversingTracking();
            }
            
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



    public void NextTrial()
    {
        if (currentTrial >= participantTrials.Count)
        {
            if (trialRep == 0) // 2 rep per condition per participant
            {
                trialRep++;
                currentTrial = 0;
            }
            else
            {
                Debug.Log("All trials completed for participant.");
                return;
            }
           
        }

        //Decompose trial condition
        (bool task_type, Vector2 id, Quaternion rotation) = participantTrials[currentTrial];
        float len = id.x;
        float width = id.y;

        // adjusting center of placements
        targetPosition = new Vector3(scenePosition.x + (mainHand * offset_lateral), scenePosition.y - offset_height, scenePosition.z);

        // update debug text
        debugText.updateText("length: " + len);

        // create path
        if (task_type)
        {
            //instantiate wire
            currentPath = Instantiate(wirePrefab, targetPosition, rotation);
            currentPath.transform.localScale = new Vector3(0.01f, len / 2, 0.01f); // len/2 because the prefab is already 2 units long
        }
        else
        {
            //instantiate tunnel
            currentPath = Instantiate(tunnelPrefab, targetPosition, rotation);
            currentPath.transform.localScale = new Vector3(width, len / 2, width); // len/2 because the prefab is already 2 units long
        }

        //create target
        Vector3 pathForward = currentPath.transform.up;
        float target_offset_init = len / 2 + 0.02f;
        Vector3 target_instant_position = targetPosition - target_offset_init * pathForward;
        currentTarget_position_init = target_instant_position; // saving ring start point for PLAN B of traverse status calculation

        if (task_type)
        {
            currentTarget = Instantiate(SelectRingPrefab(width), target_instant_position, rotation);
        }
        else
        {
            currentTarget = Instantiate(ballPrefab, target_instant_position, rotation);
        }
        currentTarget.transform.forward = currentPath.transform.up; // to overcome problem regarding orientation of the ring-to be prependicular to wire

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
        trialTask = task_type;
        tryCounter = 0;

        //Rigidbody ring_rb = currentTarget.GetComponent<Rigidbody>();
        //ring_rb.constraints = RigidbodyConstraints.FreezeRotation;
    }

    GameObject SelectRingPrefab(float W)
    {
        GameObject selectedRingPrefab = null;

        switch (W)
        {
            case 0.02f:
                selectedRingPrefab = ringPrefabs[0];
                break;
            case 0.04f:
                selectedRingPrefab = ringPrefabs[1];
                break;
            case 0.08f:
                selectedRingPrefab = ringPrefabs[2];
                break;
            default:
                Debug.LogError("No ring prefab for W: " + W);
                break;
        }

        return selectedRingPrefab;
    }


    private Transform GetWireStartPoint()
    {
        Transform[] children = currentPath.GetComponentsInChildren<Transform>(true);
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
        Vector3 ringPlaneNormal = currentTarget.transform.forward;
        Vector3 ringCenter = currentTarget.transform.position;

        Vector3 wireRayStartPos = GetWireStartPoint().position;
        Ray wireRay = new Ray(wireRayStartPos, currentPath.transform.up);
        Plane ringPlane = new Plane(ringPlaneNormal, ringCenter);
        Vector3 intersectionPoint;
        if (ringPlane.Raycast(wireRay, out float intr))
        {
            intersectionPoint = wireRay.GetPoint(intr);
            Vector3 localIntersection = intersectionPoint - ringCenter;
            float x = Vector3.Dot(localIntersection, currentTarget.transform.right);
            float y = Vector3.Dot(localIntersection, currentTarget.transform.up);

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

        isSteering = false;
        // destroy previous trial objects
        if (currentTarget != null) Destroy(currentTarget);
        if (currentPath != null) Destroy(currentPath);

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

        isSteering = true;
        if (isRightHanded)
        {
            rightHandObject.GetComponent<SkinnedMeshRenderer>().material = invisibleHand_material;
        }
        else
        {
            leftHandObject.GetComponent<SkinnedMeshRenderer>().material = invisibleHand_material;
        }

    }


    public List<(bool, Vector2, Quaternion)> GenerateParticipantTrial(int PID)
    {
        int CB_start = PID % 12;

        List<(bool, Vector2, Quaternion)> trials = new List<(bool, Vector2, Quaternion)>(); // list of IDs and Rotations

        for (int i = CB_start; i < exp_conditions.Count; i++)
        {
            Vector2 index_diff = new Vector2(exp_conditions[i].Item2, exp_conditions[i].Item3);
            List<Quaternion> shuffled_rotations = ShuffleRotations(pathRotations);
            foreach (Quaternion rotation in shuffled_rotations)
            {
                trials.Add((exp_conditions[i].Item1, index_diff, rotation));
            }
        }
        for (int i = 0; i < CB_start; i++)
        {
            Vector2 index_diff = new Vector2(exp_conditions[i].Item2, exp_conditions[i].Item3);
            List<Quaternion> shuffled_rotations = ShuffleRotations(pathRotations);
            foreach (Quaternion rotation in shuffled_rotations)
            {
                trials.Add((exp_conditions[i].Item1, index_diff, rotation));
            }
        }

        trialRep = 0; // to indicate the repetition number
        currentTrial = 0; // starting point for trials list

        return trials;
    }

    private List<Quaternion> ShuffleRotations(List<Quaternion> rot_list) //Fisher-Yates shuffle
    {
        List<Quaternion> shuffledRot = new List<Quaternion>(rot_list);
        for (int i = shuffledRot.Count - 1; i > 0; i--)
        {
            int randIndex = Random.Range(0, i + 1);
            (shuffledRot[i], shuffledRot[randIndex]) = (shuffledRot[randIndex], shuffledRot[i]);
        }
        return shuffledRot;
    }

}
