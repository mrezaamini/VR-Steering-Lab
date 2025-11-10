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

    [Header("Steering")]
    public bool isSteering = false;

    [Header("Scene")]
    public Camera mainCamera;
    public float referenceDepth = 5f; // base depth for condition
    public Vector2 baseSize = new Vector2(1f, 1f); // base width/length at base depth

    //data tracking
    private string trackingOutputFile;
    private string steeringInfoOutputFile;
    private double SteeringTime_trial;
    private double errorTime_trial;
    private int errorNumber_trial;
    private Stopwatch steeringSW;
    private Stopwatch errorSW;

    //current trial info
    private bool trial_verification = false;
    private float trialW;
    private float trialL;
    private float trialVW;
    private float trialVL;
    private float trialD;
    private int trialRotation; //1: LR, 2:RL, 3:TD, 4:DT
    private int tryCounter; // tries until successful steering. starts from 0
    private int trialRep; // to indicate which repetition this current trial is. starts from 0
    public int currentTrial = 0;
    private Vector2? previous_track; //previous track point to calculate displacement and speed
    private double? previous_time;

    //Experiment conditions
    //TODO


    void Start()
    {
        //placement start, end, path and scaling
        if (!mainCamera) mainCamera = Camera.main; //make sure camera is attached

        steeringSW = new Stopwatch();
        errorSW = new Stopwatch();

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
    }

    private void OnSteeringTracking()
    {
        //get hit point relative x and y
        //if offset greater than path width => fail trial

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
        //  TODO: destroy previous trial objects
        //if (currentTarget != null) Destroy(currentTarget);
        //if (currentPath != null) Destroy(currentPath);

        if (trial_verification)
        {
            currentTrial++;
            success_sound.Play();
        }
        else
        {
            error_sound.Play();
        }

        //TODO: next trial:
        //NextTrial();

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
            case 1: return "LR";
            case 2: return "RL"; 
            case 3: return "TD"; 
            case 4: return "DT"; 
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
                writer.WriteLine("PID,isMale,rightHanded,width,length,visualWidth,visualLength,depth,rotation,trialRep,totalTime,isValid");
            }
        }
        else
        {
            UnityEngine.Debug.Log("WARNING: file already exists, overwritting!");
        }
    }


    private void WriteSteeringInfoData()
    {
        string newData = $"{participantID},{isMale},{isRightHanded},{trialW},{trialL},{trialVW},{trialVL},{trialD},{GetPathRotationName(trialRotation)},{trialRep},{SteeringTime_trial},{trial_verification}\n";
        using (StreamWriter writer = new StreamWriter(steeringInfoOutputFile, true))
        {
            writer.WriteLine(newData);
        }
    } 

    private void adjustPathSize() // to adjust path size based on the depth relative to the base condition
    {
        
    }

}