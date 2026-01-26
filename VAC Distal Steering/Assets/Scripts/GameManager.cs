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

    [Header("UI")]
    public AudioSource success_sound;
    public AudioSource error_sound;
    public CanvasPopup popupScript;

    [Header("Steering")]
    public bool isSteering = false;
    public bool lockPosRot = false;

    [Header("Scene")]
    public Camera mainCamera;
    public GameObject Linear_Path;
    public VisualSizeHandler LP_VShandler;

    [Header("Drawing")]
    public RayBrush rayBrush;        // assign in Inspector
    public Transform boardTransform; // the board plane transform

    // Stroke output file
    private string strokeOutputFile;

    //data tracking
    private string trackingOutputFile;
    private string steeringInfoOutputFile;
    private double SteeringTime_trial;
    private double errorTime_trial;
    private int errorNumber_trial;
    private Stopwatch steeringSW;
    private Stopwatch errorSW;

    //current trial info
    private GameObject trial_path;
    private int trial_path_type; // 1 : line, 2: circle, 3: sine
    private bool trial_verification = false;
    private float trialW;
    private float trialL;
    private float trialW_A;
    private float trialL_A;
    private float trialD_A;
    private float trialD;
    public int trialRotation; 
    private int tryCounter; // tries until successful steering. starts from 0
    private int trialRep; // to indicate which repetition this current trial is. starts from 0
    public int currentTrial = 0;
    private Vector2? previous_track; //previous track point to calculate displacement and speed
    private double? previous_time;


    //current trial stroke info
    private float trialLateralSD = float.NaN;
    private float trialLateralMean = float.NaN;
    private int trialStrokeN = 0;
    private float trialTotalDistance = float.NaN;

    //Experiment conditions
    //TODO
    public float BASE_DEPTH = 1f;
    public float CurrentWidthP;
    public float CurrentLengthP;

    private List<(int, Vector3, int, int)> participantTrials;
    List<Vector2> path_geometries = new List<Vector2>() // W L at reference depth is 1f!
    {
        new Vector3(0.0349f,0.443388f),
        //new Vector3(0.0349f,0.535898f),
        new Vector3(0.0349f,0.630596f),
        new Vector3(0.0349f,0.932615f),
        new Vector3(0.0524f,0.443388f),
        //new Vector3(0.0524f,0.535898f),
        new Vector3(0.0524f,0.630596f),
        new Vector3(0.0524f,0.932615f),
        new Vector3(0.0786f,0.443388f),
        //new Vector3(0.0786f,0.535898f),
        new Vector3(0.0786f,0.630596f),
        new Vector3(0.0786f,0.932615f),
        new Vector3(0.1048f,0.443388f),
        //new Vector3(0.1048f,0.535898f),
        new Vector3(0.1048f,0.630596f),
        new Vector3(0.1048f,0.932615f)
    };

    List<float> depth_list = new List<float>()
    {
        0.6666f,
        0.8f,
        1f,
        1.3333f,
        2f,
        4f
    };

    public float getCurrentDepth()
    {
        return trialD;
    }

    public float getCurrentWidth()
    {
        return GetAngularWidth(trialW);
    }

    public float getCurrentLen()
    {
        return GetAngularLength(trialL);
    }

    public float getCurrentRep()
    {
        return trialRep;
    }

    public float getCurrentTrial()
    {
        return currentTrial;
    }

    public int getTrialCount()
    {
        return participantTrials.Count;
    }

    public float getPID()
    {
        return participantID;
    }

    public bool getRightHand()
    {
        return isRightHanded;
    }

    void Start()
    {
        //placement start, end, path and scaling
        if (!mainCamera) mainCamera = Camera.main; //make sure camera is attached

        participantTrials = GenerateParticipantTrial(participantID);

        LP_VShandler.referenceDistance = BASE_DEPTH;

        steeringSW = new Stopwatch();
        errorSW = new Stopwatch();

        // Setup global stroke CSV file
        string outDir = Path.Combine(Application.dataPath, "CapturedData");
        if (!Directory.Exists(outDir))
            Directory.CreateDirectory(outDir);

        strokeOutputFile = Path.Combine(outDir, $"{participantID}_strokes.csv");

        if (!File.Exists(strokeOutputFile))
        {
            using (var writer = new StreamWriter(strokeOutputFile, true))
            {
                writer.WriteLine("PID,Width,Length,Depth,Width_P,Length_P,Rotation,Rep,PointIndex,BoardX,BoardY,BoardZ,WorldX,WorldY,WorldZ");
            }
        }

        SetupSteeringInfoOutput();

        NextTrial();

    }

    // Update is called once per frame
    void Update()
    {
        if (!isSteering)
        {
            //check for hit and if hit start steering
            //OnStartTraversing();

        }
        else
        {
            OnSteeringTracking();

            //if hit final, finish steering
            //trial_verification = true;
            //EndTrial();
        }
        if (trial_path_type == 1)
            LP_VShandler.lockPositionAndRotation = lockPosRot;
    }


    public void NextTrial()
    {
        if(currentTrial >= participantTrials.Count)
        {
            //if (trialRep < 4) // 5 Rep per participant
            //{
            //    trialRep++;
            //    currentTrial = 0;
            //}
            //else
            //{
            //    UnityEngine.Debug.Log("All trials completed for participant.");
            //    return;
            //}
            UnityEngine.Debug.Log("All trials completed for participant.");
            return;
        }

        (int path_type, Vector3 path_geo, int trialRepNum,int path_dir) = participantTrials[currentTrial];
        float base_width = path_geo.x;
        float base_len = path_geo.y;
        float desired_depth = path_geo.z;

        switch (path_type)
        {
            case 1:
                LP_VShandler.baseSize.x = base_len;
                LP_VShandler.baseSize.y = base_width;
                LP_VShandler.desiredDistance = desired_depth;
                LP_VShandler.pathDirection = path_dir;
                LP_VShandler.referenceDistance = 1;
                trial_path_type = 1;
                trial_path = Linear_Path;
                // = true;
                break;
            default: UnityEngine.Debug.Log("Invalid Path Type!"); break;
        }

        trial_path.SetActive(true);

   

        //update trial info:
        trial_verification = false;
        trialL = base_len;
        trialW = base_width;
        trialD = desired_depth;
        trialRotation = path_dir;
        trialRep = trialRepNum;

        CurrentWidthP = trialW * (trialD / BASE_DEPTH);
        CurrentLengthP = trialL * (trialD / BASE_DEPTH);

        trialTotalDistance = 0;
        trialStrokeN = 0;
        trialLateralMean = float.NaN;
        trialLateralSD = float.NaN;
    }



    public List<(int, Vector3, int, int)> GenerateParticipantTrial(int PID) // path type, geo, trialrep,direction
    {
        List<(int, Vector3, int, int)> trials = new List<(int, Vector3, int, int)>();
        System.Random rng = new System.Random(PID);
        List<(Vector3 position, int repetition)> path_conditions =
        combine_path_geo(path_geometries, depth_list, PID);

        foreach (var (position, repetition) in path_conditions)
        {
            int first = rng.Next(2);   // 0 or 1
            int second = 1 - first;    // ensures both values appear

            trials.Add((1, position, repetition, first));
            trials.Add((1, position, repetition, second));
        }

        return trials;
    }

    //Rest from before
    private void OnSteeringTracking()
    {
        //get hit point relative x and y
        //if offset greater than path width => fail trial

    }

    private List<(Vector3,int)> combine_path_geo(List<Vector2> path_lw, List<float> depths, int PID)
    {
        List<(Vector3 position, int repetition)> result = new();
        if (depths == null || depths.Count == 0) return result;

        int n = depths.Count;
        int start = PID % n;


        for (int di = 0; di < n; di++)
        {
            float f = depths[(start + di) % n];

            for (int ci = 0; ci < 5; ci++)
            {
                foreach (var v2 in path_lw)
                {
                    result.Add((
                        new Vector3(v2.x, v2.y, f), //x,y,depth,rep#
                        ci
                    ));
                }
            }
        }

        return result;
    }

    private void SaveWireTrack(float x, float y) // write tracking information to file: position x, position y, speed
    {
        double input_time = steeringSW.Elapsed.TotalMilliseconds;
        double elapsed_time = 0.0d;
        double movement_speed = 0.0d;
        double visual_speed = 0.0d;
        Vector2 displacement_position = new Vector2(x, y);
        Vector2 movement_vector = Vector2.zero;
        if (previous_track.HasValue)
        {
            movement_vector = displacement_position - previous_track.Value;
        }
        previous_track = new Vector3(x, y);

        if (previous_time.HasValue)
        {
            elapsed_time = input_time - previous_time.Value;
            movement_speed = movement_vector.magnitude * 1000 / elapsed_time; //actual speed
            //TODO: update visual speed
            visual_speed = 0;
        }
        previous_time = input_time;

        string newData = $"{participantID},{trialW},{trialL},{trialD},{GetPathRotationName(trialRotation)},{trialRep},{x},{y},{movement_vector.magnitude},{movement_speed},{visual_speed},{input_time}\n";



        using (StreamWriter writer = new StreamWriter(trackingOutputFile, true))
        {
            writer.WriteLine(newData);
        }
        //debugText.updateText("X: "+x+" Y: "+y);
    }

    private void OnDestroy()
    {
        StreamWriter writer = new StreamWriter(trackingOutputFile, true);
        writer.Close();
    }


    public void EndTrial(bool trial_veri) // to end a trial and move to the next one
    {
        if (trial_veri)
        {
            // === SAVE STROKE FOR THIS TRIAL ===
            if (rayBrush != null && rayBrush.LastStroke != null && rayBrush.LastStroke.Count > 1)
            {
                SaveStrokeForCurrentTrial(
                    trialRep,
                    trialW,
                    trialL,
                    trialD,
                    trialRotation,
                    rayBrush.LastStroke
                );
            }

            currentTrial++;
            trial_verification = true;
            success_sound.Play();
        }
        else
        {
            error_sound.Play();
            popupScript.ShowPopup();
        }

        trial_path.SetActive(false);
        if (trial_path_type == 1)
        {
            lockPosRot = false;
        }

       



        //stoping and saving timers
        steeringSW.Stop();
        SteeringTime_trial = steeringSW.Elapsed.TotalMilliseconds;
        steeringSW.Reset();

        WriteSteeringInfoData();

        //errorSW.Stop(); // to make sure it is stopped at the end
        //errorTime_trial += errorSW.Elapsed.TotalMilliseconds;
        //errorSW.Reset();

        //WriteSteeringInfoData();

        isSteering = false;
        CurrentWidthP = 0;
        CurrentLengthP = 0;
        //  TODO: destroy previous trial objects
        //if (currentTarget != null) Destroy(currentTarget);
        //if (currentPath != null) Destroy(currentPath);


        NextTrial();

    }

    public void OnStartTraversing()
    {
        isSteering = true;
        steeringSW.Restart();
    }

    private string GetPathRotationName(int trial_rot)
    {
        switch (trial_rot)
        {
            case 0: return "LR";
            case 1: return "RL"; 
            case 2: return "TD"; 
            case 3: return "DT"; 
            default: return "N/A"; 
        }
    }

    private void SetupSteeringTrackOutput(float trial_w, float trial_l, float trial_d, int trial_rot, int trial_repetition)
    {
        // TODO: Find condition ID
        //int conditionIndex = exp_conditions.IndexOf((trial_task_type, trial_l, trial_w, trial_exec));
        //if (conditionIndex == -1)
        //{
        //    UnityEngine.Debug.LogError("Invalid trial conditions provided.");
        //    return;
        //}
        int conditionIndex = 0; //TODO: remove when condition index calculator is added
        string conditionString = $"C{conditionIndex}";

        // Find rotation ID
        string rotationString = GetPathRotationName(trial_rot);

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

        using (StreamWriter writer = new StreamWriter(trackingOutputFile, false))
        {
            writer.WriteLine("PID,width,length,depth,rotation,trialRep,PositionX,PositionY,Movement,Speed,visualSpeed,Timestamp");
        }
    }


    private void SaveStrokeForCurrentTrial(
    int rep,
    float width,
    float length,
    float depth,
    int rotation,
    List<Vector3> worldPoints)
    {
        if (boardTransform == null)
        {
            UnityEngine.Debug.LogWarning("Board transform not set; saving world coords only.");
        }
        List<float> lateral = new List<float>();
        float totalDist = 0f;
        Vector2? prev = null;
        using (var writer = new StreamWriter(strokeOutputFile, true))
        {
            for (int i = 0; i < worldPoints.Count; i++)
            {
                Vector3 wp = worldPoints[i];

                // Project into board's local space (so X/Y are in board coordinates)
                float bx = wp.x;
                float by = wp.y;
                float bz = wp.z;

                //if (boardTransform != null)
                //{
                //    Vector3 local = boardTransform.InverseTransformPoint(wp);
                //    if (rotation == 1)
                //    {
                //        local.x = -local.x;
                //        //local.z = -local.z; //rotate based on direction
                //    }

                //    float mx = local.x * boardTransform.lossyScale.x;
                //    float mz = local.z * boardTransform.lossyScale.z;
                //    mx *= 10f;
                //    mz *= 10f;

                //    //bx = local.x;
                //    bx = mx;
                //    by = local.y;
                //    //bz = local.z;
                //    bz = mz;

                //    float lateralVal = mz;  // <-- lateral freedom axes
                //    lateral.Add(lateralVal);
                //}
                Vector3 b = WorldToBoardMeters_DirInvariant(boardTransform, Camera.main.transform, wp, boardTransform.right);


                // If you want LR and RL to have the same progress direction:
                if (rotation == 1) //RL
                {
                    b.x = -b.x;     // flip only task axis
                }
                bx = b.x;  
                by = b.y;  
                bz = -b.z;

                Vector2 curr = new Vector2(bx, bz); 
                if (prev.HasValue)
                {
                    totalDist += Vector2.Distance(prev.Value, curr);
                }
                prev = curr;

               
                float lateralVal = bz; 
                lateral.Add(lateralVal);
                float w_p = trialW * (trialD / BASE_DEPTH);
                float l_p = trialL * (trialD / BASE_DEPTH);
                writer.WriteLine(
                    $"{participantID}," +
                    $"{width}," +
                    $"{length}," +
                    $"{depth}," +
                    $"{w_p}," +
                    $"{l_p}," +
                    $"{GetPathRotationName(rotation)}," +
                    $"{rep}," +
                    $"{i}," +
                    $"{bx},{by},{bz}," +
                    $"{wp.x},{wp.y},{wp.z}"
                );
            }
        }
        trialTotalDistance = totalDist;
        trialStrokeN = lateral.Count;
        if (trialStrokeN > 1)
        {
            float sum = 0f, sumSq = 0f;
            foreach (var v in lateral) { sum += v; sumSq += v * v; }

            float mean = sum / trialStrokeN;
            float var = (sumSq - trialStrokeN * mean * mean) / (trialStrokeN - 1); // sample variance
            if (var < 0f) var = 0f;

            trialLateralMean = mean;
            trialLateralSD = Mathf.Sqrt(var);
        }
        else
        {
            trialLateralMean = float.NaN;
            trialLateralSD = float.NaN;
        }
    }

    private static Vector3 WorldToBoardMeters_DirInvariant(
    Transform board,
    Transform head,          // pass Camera.main.transform or your headTransform
    Vector3 worldPoint,
    Vector3 taskAxisWorld    // pass board.right if red == board.right
)
    {
        Vector3 rel = worldPoint - board.position;

        // 1) Task axis (red)
        Vector3 T = taskAxisWorld.normalized;

        // 2) Normal axis (green): choose whichever of forward/backward points toward the user
        Vector3 toHead = (head.position - board.position).normalized;
        Vector3 N = board.forward.normalized;
        if (Vector3.Dot(N, toHead) < 0f) N = -N;  // ensure N points to the user

        // 3) Up axis on the board (blue), consistent across LR/RL
        // Try U = cross(N,T). If that ends up inverted relative to board.up, flip it.
        Vector3 U = Vector3.Cross(N, T).normalized;

        // Optional: keep U roughly aligned with board.up for intuitive "up"
        if (Vector3.Dot(U, board.up) < 0f) U = -U;

        float bx = Vector3.Dot(rel, T); // along red (task)
        float by = Vector3.Dot(rel, U); // along blue (on-plane up/down)
        float bz = Vector3.Dot(rel, N); // along green (off-plane, should be ~offset)

        return new Vector3(bx, by, bz);
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
                writer.WriteLine("PID,isMale,rightHanded,Width_B,Length_B,Depth,Dir,PathType,Width_P,Length_P,Width_A,Length_A,Rep,isValid,MovementTime,StrokeN,MeanX,SDx,MeanX_A,SDx_A,Ae,Ae_A");
            }
        }
        else
        {
            UnityEngine.Debug.Log("WARNING: file already exists, overwritting!");
        }
    }


    private void WriteSteeringInfoData()
    {
        //calculate actual size of the path
        

        float meanA = MetersToVisualAngleDeg(trialLateralMean, trialD);
        float sdA = MetersToVisualAngleDeg(trialLateralSD, trialD);

        float Ae_A = MetersToVisualAngleDeg(trialTotalDistance, trialD);

        if(Ae_A < GetAngularLength(trialL))
        {
            Ae_A = GetAngularLength(trialL);
        }
        if (trialTotalDistance < CurrentLengthP)
        {
            trialTotalDistance = CurrentLengthP;
        }


        //calculate angular size of the path
        string newData = $"{participantID},{isMale},{isRightHanded},{trialW},{trialL},{trialD},{GetPathRotationName(trialRotation)},{trial_path_type},{CurrentWidthP},{CurrentLengthP},{GetAngularWidth(trialW)},{GetAngularLength(trialL)},{trialRep},{trial_verification},{SteeringTime_trial},{trialStrokeN},{trialLateralMean},{trialLateralSD},{meanA},{sdA},{trialTotalDistance},{Ae_A}";
        using (StreamWriter writer = new StreamWriter(steeringInfoOutputFile, true))
        {
            writer.WriteLine(newData);
        }
    } 

    private float GetAngularWidth(float w)
    {
        switch (w)
        {
            case 0.0349f: return 2f;
            case 0.0524f: return 3f;
            case 0.0786f: return 4.5f;
            case 0.1048f: return 6f;
            default: return float.NaN;

        }
    }

    private float GetAngularLength(float w)
    {
        switch (w)
        {
            case 0.443388f: return 25f;
            case 0.535898f: return 30f;
            case 0.630596f: return 35f;
            case 0.932615f: return 50f;
            default: return float.NaN;

        }
    }

    private static float MetersToVisualAngleDeg(float sizeMeters, float distanceMeters)
    {
        if (float.IsNaN(sizeMeters) || distanceMeters <= 0f) return float.NaN;
        return 2f * Mathf.Atan(sizeMeters / (2f * distanceMeters)) * Mathf.Rad2Deg;
    }

    private void adjustPathSize() // to adjust path size based on the depth relative to the base condition
    {
        
    }

}