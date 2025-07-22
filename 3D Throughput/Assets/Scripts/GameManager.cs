using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System.Diagnostics;

public class GameManager : MonoBehaviour // GAME MANAGER FOR PLACEMENT PILOT STUDY, REMEMBER TO PUT THE UPDATED VERSION BACK INTO THE SOURCE FILE!!!!!!
{
    [Header("Participant Info")]
    public int participantID; // starts from 0
    public bool isMale; // true: long, false: short for shoulder breadth
    public bool isRightHanded;

    [Header("Ring and Wire")]
    public GameObject wirePrefab;
    [SerializeField] private List<GameObject> ringPrefabs;
    [SerializeField] private Material wire_normal_mat;
    [SerializeField] private Material wire_error_mat;

    [Header("Ball and Tunnel")]
    [SerializeField] private List<GameObject> ballPrefabs;
    [SerializeField] private List<GameObject> tunnelPrefabs;
    [SerializeField] private Material tun_normal_mat;
    [SerializeField] private Material tun_error_mat;

    [Header("UI")]
    public AudioClip success_sound;
    public AudioClip error_sound;



    private Vector3 targetPosition;
    public DebugText debugText;
    public StateText stateText;
    public CanvasPopup popupScript;

    // for saving data
    private string trackingOutputFile;
    private string steeringInfoOutputFile;
    private double SteeringTime_trial;
    private double errorTime_trial;
    private int errorNumber_trial;
    private Stopwatch steeringSW;
    private Stopwatch errorSW;

    //current task info 
    private bool trial_verification = false;
    private float trialW;
    private float trialL;
    private Quaternion trialR;
    private bool trialTask; // true is ring and wire, false is ball and tunnel
    private int tryCounter; // tries until successful steering. starts from 0
    private int trialRep; // to indicate which repetition this current trial is. starts from 0
    public int currentTrial = 0;
    private GameObject currentPath;
    private GameObject currentTarget;
    private int trialExecType; // 0: fast, 1: as fast and accurate, 1: accurate
    private Vector3? previous_track;
    private double? previous_time;


    //private GameObject currentPath;
    //private GameObject currentTarget;
    private Vector3 currentTarget_position_init = new Vector3(0.0f, 0.0f, 0.0f); // used for traversing status (start, end) calculations (PLAN B, since fast movement may not be catched by the collider itself. As no rotation is included in this user study, this approach can be useful)
    private bool isSteering = false;



    // experiment conditions
    private List<(bool, float, float, int)> exp_conditions = new List<(bool, float, float, int)> // task dificulty (true: ring, false: tunnel), L, W, execution types (0: fast, 1: fast and accurate, 2: accurate)
    {
        //(true, 0.25f, 0.02f, 0),
        //(true, 0.25f, 0.04f, 0),
        (true, 0.25f, 0.08f, 0),
        //(true, 0.40f, 0.02f, 0),
        //(true, 0.40f, 0.04f, 0),
        (true, 0.40f, 0.08f, 0),
        //(true, 0.25f, 0.02f, 1),
        //(true, 0.25f, 0.04f, 1),
        //(true, 0.25f, 0.08f, 1),
        //(true, 0.40f, 0.02f, 1),
        //(true, 0.40f, 0.04f, 1),
        //(true, 0.40f, 0.08f, 1),
        //(true, 0.25f, 0.02f, 2),
        //(true, 0.25f, 0.04f, 2),
        //(true, 0.25f, 0.08f, 2),
        //(true, 0.40f, 0.02f, 2),
        //(true, 0.40f, 0.04f, 2),
        //(true, 0.40f, 0.08f, 2)

    };

    private List<Quaternion> pathRotations = new List<Quaternion> { 
        // main axial rotations
        //Quaternion.Euler(0, 0, 0),
        //Quaternion.Euler(0, 0, 90),
        //Quaternion.Euler(0, 0, 180),
        //Quaternion.Euler(0, 0, 270),
        //Quaternion.Euler(90, 0, 0),
        //Quaternion.Euler(270, 0, 0),
        //Quaternion.Euler(45, 0, 0),
        //Quaternion.Euler(-45, 0, 0),
        //Quaternion.Euler(135, 0, 0), // in depth diagonal
        //Quaternion.Euler(-135, 0, 0),
        //Quaternion.Euler(0, 0, -45),
        //Quaternion.Euler(0, 0, 45),
        //Quaternion.Euler(0, 0, -135),
        //Quaternion.Euler(0, 0, 135),
        //Quaternion.Euler(0, 45, 90),
        //Quaternion.Euler(0, 135, 90),
        //Quaternion.Euler(0, -45, 90),
        //Quaternion.Euler(0, -135, 90),

        Quaternion.Euler(0, 0, 0),
        Quaternion.Euler(0, 0, 90)
    };

    // target placement attributes
    private float offset_lateral = 0.0f;
    private float offset_depth = 0.35f;
    private float offset_height = 0.15f;
    private float mainHand; // multiplier of the lateral offset to adjust dominant hand position
    private Vector3 scenePosition;
    private float target_placement_offset = 0.02f;

    private GameObject mainCamera;
    private bool calibrationStatus = false; //should be false at start
    public GameObject startButton;


    private List<(bool, Vector2, Quaternion, int)> participantTrials; // task type (true: ring, false: tunnel), ID, rotation, task execution type

    //hand materials
    [SerializeField] private Material invisibleHand_material;
    [SerializeField] private Material originalHand_material;
    [SerializeField] private GameObject rightHandObject;
    [SerializeField] private GameObject leftHandObject;
    private SkinnedMeshRenderer mainHand_skin;


    // check valid trial
    private bool in_valid_zone = false;

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
        steeringSW = new Stopwatch();
        errorSW = new Stopwatch();

        //HOME TEST
        //startExperiment();
    }

    // Update is called once per frame
    void Update()
    {
        //debugText.updateText("stat: "+in_valid_zone+ " "+isSteering);
        if (!calibrationStatus)
        {
            return;
        }

        if (!isSteering & currentTarget != null)
        {
            
            float forward_displacement = Mathf.Abs(Vector3.Dot(currentTarget.transform.GetChild(0).transform.position - currentTarget_position_init, currentPath.transform.up)); // calculating displacement for starting traverse status
            // Check if movement along the forward vector is greater than 0.2 units
            if (forward_displacement > 0.0195f) //0.2 is the init offset in NextTrial(), and 0.005 for the target thickness/2
            {
                OnStartTraversing();
            }

            float centeral_displacement = Mathf.Abs(Vector3.Dot(currentTarget.transform.GetChild(0).transform.position - currentTarget_position_init, currentPath.transform.up));
        }

        if (isSteering)
        {
            OnTraversingTracking();
            float forward_displacement = Mathf.Abs(Vector3.Dot(currentTarget.transform.GetChild(0).transform.position - currentTarget_position_init, currentPath.transform.up));
            // Check if movement along the forward vector is greater than 0.2 units
            if (forward_displacement > currentPath.transform.localScale.y * 2 + 0.02f) //*2 since the prefab is already x2 long (see create wire in NextTrial()). 0.005 is because of the ring thickness (=1) and ring plane is considered as the center of it 
            {
                trial_verification = true;
                EndTrial();
            }
            else
            {
                //OnTraversingTracking();
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
        //HOME TEST
        //only for home test ::
        //scenePosition = new Vector3(1f, 1f, 1f);
        //////
        SetupSteeringInfoOutput();
        calibrationStatus = true;
        NextTrial();
    }



    public void NextTrial()
    {
        if (currentTrial >= participantTrials.Count)
        {
            if (trialRep < 2) // 3 rep per condition per participant
            {
                trialRep++;
                currentTrial = 0;
            }
            else
            {
                UnityEngine.Debug.Log("All trials completed for participant.");
                return;
            }
           
        }

        //Decompose trial condition
        (bool task_type, Vector2 id, Quaternion rotation, int execType) = participantTrials[currentTrial];
        float len = id.x;
        float width = id.y;

        // adjusting center of placements
        targetPosition = new Vector3(scenePosition.x + (mainHand * offset_lateral), scenePosition.y - offset_height, scenePosition.z);

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
            currentPath = Instantiate(SelectTunPrefab(width), targetPosition, rotation);
            currentPath.transform.localScale = new Vector3(width+0.01f, len / 2, width + 0.01f); // len/2 because the prefab is already 2 units long. +0.01f to adjust tunnel width based on the ball thickness
        }

        //create target
        Vector3 pathForward = currentPath.transform.up;
        float target_offset_init = len / 2 + target_placement_offset;
        Vector3 target_instant_position = targetPosition - target_offset_init * pathForward;
        currentTarget_position_init = target_instant_position; // saving ring start point for PLAN B of traverse status calculation

        if (task_type)
        {
            currentTarget = Instantiate(SelectRingPrefab(width), target_instant_position, rotation);
        }
        else
        {
            currentTarget = Instantiate(SelectBallPrefab(width), target_instant_position, rotation);
        }
        currentTarget.transform.forward = currentPath.transform.up; // to overcome problem regarding orientation of the ring-to be prependicular to wire

        //saving tracking information as output for each trial
        SetupSteeringTrackOutput(width, len, rotation, task_type, trialRep,execType);
        //update trial info for saving tracking info in output file
        trial_verification = false;
        trialL = len;
        trialR = rotation;
        trialW = width;
        trialTask = task_type;
        SteeringTime_trial = 0f;
        errorNumber_trial = 0;
        errorTime_trial = 0f;
        in_valid_zone = false;
        //tryCounter = 0;
        trialExecType = execType;
        previous_track = null;
        previous_time = null;

        switch (trialExecType)
        {
            case 0:
                debugText.updateText("Fast");
                break;
            case 1:
                debugText.updateText("Fast & Accurate");
                break;
            case 2:
                debugText.updateText("Accurate");
                break;
            default:
                debugText.updateText("Unknown Execution Type");
                break;
        }


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
                UnityEngine.Debug.LogError("No ring prefab for W: " + W);
                break;
        }

        return selectedRingPrefab;
    }

    GameObject SelectBallPrefab(float W)
    {
        GameObject selectedBallPrefab = null;

        switch (W)
        {
            case 0.02f:
                selectedBallPrefab = ballPrefabs[0];
                break;
            case 0.04f:
                selectedBallPrefab = ballPrefabs[1];
                break;
            case 0.08f:
                selectedBallPrefab = ballPrefabs[2];
                break;
            default:
                UnityEngine.Debug.LogError("No ball prefab for W: " + W);
                break;
        }

        return selectedBallPrefab;
    }

    GameObject SelectTunPrefab(float W)
    {
        GameObject selectedTunPrefab = null;

        switch (W)
        {
            case 0.02f:
                selectedTunPrefab = tunnelPrefabs[0];
                break;
            case 0.04f:
                selectedTunPrefab = tunnelPrefabs[1];
                break;
            case 0.08f:
                selectedTunPrefab = tunnelPrefabs[2];
                break;
            default:
                UnityEngine.Debug.LogError("No ball prefab for W: " + W);
                break;
        }

        return selectedTunPrefab;
    }


    private void OnTraversingTracking()
    {
        Vector3 ringPlaneNormal = currentTarget.transform.GetChild(0).transform.forward;
        Vector3 ringCenter = currentTarget.transform.GetChild(0).transform.position;
        
        Vector3 wireRayStartPos = currentTarget_position_init;

        Ray wireRay = new Ray(wireRayStartPos, currentPath.transform.up);
        Plane ringPlane = new Plane(ringPlaneNormal, ringCenter);
        Vector3 intersectionPoint;
        float targetSpeed = currentTarget.transform.GetChild(0).GetComponent<Rigidbody>().velocity.magnitude;
        if (ringPlane.Raycast(wireRay, out float intr))
        {
            intersectionPoint = wireRay.GetPoint(intr);
            Vector3 localIntersection = intersectionPoint - ringCenter;
            float x = Vector3.Dot(localIntersection, currentTarget.transform.GetChild(0).transform.right);
            float y = Vector3.Dot(localIntersection, currentTarget.transform.GetChild(0).transform.up);

            Vector3 wireDirection = currentPath.transform.up.normalized;
            float z = Vector3.Dot(ringCenter - currentPath.transform.position, wireDirection) + trialL/2; //make it greater than 0
            
            SaveWireTrack(x, y, z);

            // validating trial
            float point_dis = Mathf.Sqrt((x * x) + (y * y));
            if (point_dis > (trialW / 2) + 0.01)
            {
                FailTrial();
            }
            //debug only
            //else
            //{
            //    stateText.updateText("In!! ");
            //}
        

        }
        else
        {
             debugText.updateText("no intersection found");
        }                                          
                     
    }


    private void FailTrial()
    {
        //stateText.updateText("OUT!!");
        //remove track file: Done (streamwriter false on creation)
        //summary file add column
        // trial number update
        //reset trial and run it (end trial?) and one back
        //================================================================

        //update text
        popupScript.ShowPopup();
        EndTrial();

    }

    private void SaveWireTrack(float x, float y, float z) // write tracking information to file: position x, position y, speed
    {
        double input_time = steeringSW.Elapsed.TotalMilliseconds;
        double elapsed_time = 0.0d;
        double movement_speed = 0.0d;
        Vector3 displacement_position = new Vector3(x, y, z);
        Vector3 movement_vector = Vector3.zero;
        if (previous_track.HasValue)
        {
            movement_vector = displacement_position - previous_track.Value;
        }
        previous_track = new Vector3(x, y, z);

        if (previous_time.HasValue)
        {
            elapsed_time = input_time - previous_time.Value;
            movement_speed = movement_vector.magnitude*1000 / elapsed_time;
        }
        previous_time = input_time;
        
        string newData = $"{participantID},{trialTask},{isRightHanded},{trialW},{trialL},{GetPathRotationID(trialR)},{trialExecType},{trialRep},{x},{y},{z},{movement_vector.magnitude},{movement_speed},{input_time}\n";

        

        using (StreamWriter writer = new StreamWriter(trackingOutputFile, true))
        {
            writer.WriteLine(newData);
        }
    }

    private void OnDestroy()
    {
        StreamWriter writer = new StreamWriter(trackingOutputFile, true);
        writer.Close();
    }

    public void EndTrial() // to end a trial and move to the next one
    {
        //stoping and saving timers
        steeringSW.Stop();
        SteeringTime_trial = steeringSW.Elapsed.TotalMilliseconds;
        steeringSW.Reset();

        errorSW.Stop(); // to make sure it is stopped at the end
        errorTime_trial += errorSW.Elapsed.TotalMilliseconds;
        errorSW.Reset();

        WriteSteeringInfoData();

        isSteering = false;
        // destroy previous trial objects
        if (currentTarget != null) Destroy(currentTarget);
        if (currentPath != null) Destroy(currentPath);

        if (trial_verification)
        {
            currentTrial++;
            AudioSource.PlayClipAtPoint(success_sound, Camera.main.transform.position);
        }
        else
        {
            AudioSource.PlayClipAtPoint(error_sound, Camera.main.transform.position);
        }
        

        if (isRightHanded)
        {
            rightHandObject.GetComponent<SkinnedMeshRenderer>().material = originalHand_material;
        }
        else
        {
            leftHandObject.GetComponent<SkinnedMeshRenderer>().material = originalHand_material;
        }
        

        NextTrial();

    }

    public void OnStartTraversing()
    {

        isSteering = true;
        if (isRightHanded)
        {
            rightHandObject.GetComponent<SkinnedMeshRenderer>().material = invisibleHand_material;
        }
        else
        {
            leftHandObject.GetComponent<SkinnedMeshRenderer>().material = invisibleHand_material;
        }
        steeringSW.Restart();

    }


    public List<(bool, Vector2, Quaternion, int)> GenerateParticipantTrial(int PID) //udpated for new conditions, i.e., task execution type
    {
        int CB_start = PID % 3; // counter balancing execution strategies

        List<(bool, Vector2, Quaternion, int)> trials = new List<(bool, Vector2, Quaternion, int)>(); // list of IDs and Rotations

        for (int i = CB_start*6; i < exp_conditions.Count; i++)
        {
            Vector2 index_diff = new Vector2(exp_conditions[i].Item2, exp_conditions[i].Item3);
            List<Quaternion> shuffled_rotations = ShuffleRotations(pathRotations);
            //List<Quaternion> shuffled_rotations = pathRotations;
            int execType = exp_conditions[i].Item4;
            foreach (Quaternion rotation in shuffled_rotations)
            {
                trials.Add((exp_conditions[i].Item1, index_diff, rotation, execType));
            }
        }
        for (int i = 0; i < CB_start*6; i++)
        {
            Vector2 index_diff = new Vector2(exp_conditions[i].Item2, exp_conditions[i].Item3);
            List<Quaternion> shuffled_rotations = ShuffleRotations(pathRotations);
            int execType = exp_conditions[i].Item4;
            foreach (Quaternion rotation in shuffled_rotations)
            {
                trials.Add((exp_conditions[i].Item1, index_diff, rotation,execType));
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

    public void OnHitBoundaries()
    {
        if (isSteering)
        {
            errorNumber_trial++;
            AudioSource.PlayClipAtPoint(error_sound, Camera.main.transform.position);
            OnHitVisualFeedback(trialTask);
            errorSW.Restart();
        }
        in_valid_zone = false;
    }

    public void OnCorrectHit()
    {
        in_valid_zone = true;
        if (isSteering)
        {
            errorSW.Stop();
            errorTime_trial += errorSW.Elapsed.TotalMilliseconds;
            errorSW.Reset();
            OnCorrectVisualFeedback(trialTask);
        }
    }

    // to find the rotation ID (rot<index in list of rotations) for data naming
    private string GetPathRotationID(Quaternion trial_rot)
    {
        string rotationString = "UnknownRot";
        for (int i = 0; i < pathRotations.Count; i++)
        {
            if (Quaternion.Angle(trial_rot, pathRotations[i]) < 1f) // Allow small floating-point differences
            {
                rotationString = $"Rot{i}";
                break;
            }
        }
        return rotationString;
    }

    private void SetupSteeringTrackOutput(float trial_w, float trial_l, Quaternion trial_rot, bool trial_task_type, int trial_repetition, int trial_exec)
    {
        // Find condition ID
        int conditionIndex = exp_conditions.IndexOf((trial_task_type, trial_l, trial_w, trial_exec));
        if (conditionIndex == -1)
        {
            UnityEngine.Debug.LogError("Invalid trial conditions provided.");
            return;
        }
        string conditionString = $"C{conditionIndex}";

        // Find rotation ID
        string rotationString = GetPathRotationID(trial_rot);

        // Assign repetition string
        string repetitionString = $"Rep{trial_repetition}";

        // Construct and return the file identifier
        string trackingOutputPath = Path.Combine(Application.dataPath, "CapturedData");
        string trackingOutputName = $"{participantID}_{conditionString}_{rotationString}_{repetitionString}_Track.csv";
        trackingOutputFile = Path.Combine(trackingOutputPath, trackingOutputName);
        if (!Directory.Exists(trackingOutputPath))
        {
            UnityEngine.Debug.Log("Directory Not Found!! created new one");
            Directory.CreateDirectory(trackingOutputPath);
        }
        //if (!File.Exists(trackingOutputFile))
        //{
        //    using (StreamWriter writer = new StreamWriter(trackingOutputFile, true))
        //    {
        //        writer.WriteLine("PID,taskType,rightHanded,width,length,rotation,execType,trialRep,PositionX,PositionY,PositionZ,Movement,Speed,Timestamp");
        //    }
        //}
        //else
        //{
        //    UnityEngine.Debug.Log("WARNING: file already exists, overwritting!");
        //}

        using (StreamWriter writer = new StreamWriter(trackingOutputFile, false))
        {
            writer.WriteLine("PID,taskType,rightHanded,width,length,rotation,execType,trialRep,PositionX,PositionY,PositionZ,Movement,Speed,Timestamp");
        }
    }

    private void SetupSteeringInfoOutput()
    {
        string trackingOutputPath = Path.Combine(Application.dataPath, "CapturedData");
        string trackingOutputName = $"{participantID}_summary.csv";
        steeringInfoOutputFile = Path.Combine(trackingOutputPath, trackingOutputName);
        if (!Directory.Exists(trackingOutputPath))
        {
            UnityEngine.Debug.Log("Directory Not Found!! created new one");
            Directory.CreateDirectory(trackingOutputPath);
        }
        if (!File.Exists(steeringInfoOutputFile))
        {
            using (StreamWriter writer = new StreamWriter(steeringInfoOutputFile, true))
            {
                writer.WriteLine("PID,isMale,rightHanded,taskType,width,length,rotation,execType,trialRep,totalTime,errorTime,errorNumber,isValid");
            }
        }
        else
        {
            UnityEngine.Debug.Log("WARNING: file already exists, overwritting!");
        }
    }

    private void WriteSteeringInfoData()
    {
        string newData = $"{participantID},{isMale},{isRightHanded},{trialTask},{trialW},{trialL},{GetPathRotationID(trialR)},{trialExecType},{trialRep},{SteeringTime_trial},{errorTime_trial},{errorNumber_trial},{trial_verification}\n";
        using (StreamWriter writer = new StreamWriter(steeringInfoOutputFile, true))
        {
            writer.WriteLine(newData);
        }
    }

    private void OnHitVisualFeedback(bool path_type) // path type true for wire, false for tunnel
    {
        Renderer path_renderer = currentPath.GetComponent<Renderer>();
        if (path_type)
        {
            // change color of the wire to red
            path_renderer.material = wire_error_mat;
        }
        else
        {
            // change color of the tunnel to red
            path_renderer.material = tun_error_mat;
        }
    }

    private void OnCorrectVisualFeedback(bool path_type) // path type true for wire, false for tunnel
    {
        Renderer path_renderer = currentPath.GetComponent<Renderer>();
        if (path_type)
        {
            // change color of the wire to normal
            path_renderer.material = wire_normal_mat;
        }
        else
        {
            // change color of the tunnel to normal
            path_renderer.material = tun_normal_mat;
        }
    }

}
