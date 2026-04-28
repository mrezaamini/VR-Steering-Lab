using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System.Diagnostics;
using System;
using TMPro;
using UnityEngine.Rendering;
public class Recorder : MonoBehaviour
{
    public Transform camera;
    public Transform controller;

    public Transform controllerLeft;
    public Transform controllerRight;

    public Transform pointer;
    public Transform pointerLeft;
    public Transform pointerRight;

    private int repetitions = 5;
    public bool randomizeOrder = true;


    //public Transform target;

    public Transform[] calibrationPoints;

    public GameObject syringe;
    public GameObject syringeRight;
    public GameObject syringeLeft;

    private string FilePathSummary;
    private string FilePathAll;
    private string FilePathCollider;

    public bool isInside;

    public int clickCounter = 0;

    public int totalError;

    public bool assistanceActivated;

    private float previousTime = 0;

    public bool inCollider;

    public Canvas text;
    public TMP_Text status;


    public Transform selectionCollider;
    public Transform recorderCollider;

    public SphereInsert sphereInsert;

    public string ParticipantNumber;
    public bool leftHanded;
    public bool isNurse;

    private bool experiementDone;
    private bool training;
    //public ActivateVisualGuides activateVisualGuides;

    private Stopwatch steeringSW;
    private string summaryPath;
    private float[] pathLengths = {0.1f, 0.2f};
    private float[] startWidths = { 0.04f };
    private float[] endWidths = {0.01f};

    private List<TrialConfig> trialList = new List<TrialConfig>();
    private int currentTrialIndex = 0;
    private TrialData currentTrial;
    private TunnelSegment currentTunnel;
    private List<TrialData> allTrials = new List<TrialData>();

    private Vector3 ballStartPos;
    private Vector3 prevBallPos;
    private bool studyActive;
    private bool isSteering;
    private Vector3 taskAxisDir;

    public ArmCalibration armCalibration;
    public TunnelBuilder tunnelBuilder;

    public GameObject insertPoint;
    private Transform ballTransform;

    public AudioClip success_sound;
    public AudioClip error_sound;
    public float interTrialDelay = 1f;

    // Start is called before the first frame update
    void Start()
    {
        if (leftHanded)
        {
            controller = controllerLeft;
            syringe = syringeLeft;
            pointer = pointerLeft;
        }
        else
        {
            controller = controllerRight;
            syringe = syringeRight;
            pointer = pointerRight;
        }
            
        assistanceActivated = true;

        steeringSW = new Stopwatch();
        InitialiseSummaryCSV();
        BuildTrialList();



        calibrationPoints = new Transform[6];
        string FileName = ParticipantNumber + "_" + DateTime.Now.ToString("h-mm-ss") + ".txt";
        FilePathAll = Application.dataPath + "/DataCollection/All/All_" + FileName;
        FilePathSummary = Application.dataPath + "/DataCollection/Summary/Summary_" + FileName;
        FilePathCollider = Application.dataPath + "/DataCollection/Collider/ColliderArea_" + FileName;

        string message = "ParticipantNumber; " +
            "Nurse; " +
            "Click Counter; " +
            "Left Handed; " +
            "Assistance Activated; " +
            "Controller Position; " +
            "Pointer Position; " +
            "Camera Position; " +
            "Target Position; " +
            "Calibration Point Position 1; " +
            "Calibration Point Position 2; " +
            "Calibration Point Position 3; " +
            "Calibration Point Position 4; " +
            "Calibration Point Position 5; " +
            "Calibration Point Position 6; " +
            "Syringe angle x; " +
            "Syringe angle y; " +
            "Syringe angle z; " +
            "Angle Offset X ; " +
            "Angle Offset Y ; " +
            "Distance b/w Syringe and Insert position; " +
            "Is Inside; " +
            "Selection Collider; " +
            "Recorder Collider; " +
            "Total Time; " +
            "Time Seconds; " +
            "Time Milliseconds; ";

        using (StreamWriter sw = File.AppendText(FilePathSummary))
        {
            sw.WriteLine(message);
        }

        message = "ParticipantNumber; " +
            "Nurse; " +
            "Click Counter; " +
            "Assistance Activated; " +
            "Controller Position; " +
            "Pointer Position; " +
            "Target Position; " +
            "Is Inside; " +
            "Total Time; ";
        using (StreamWriter sw = File.AppendText(FilePathAll))
        {
            sw.WriteLine(message);
        }

        message = "ParticipantNumber; " +
            "Nurse; " +
            "Click Counter; " +
            "Assistance Activated; " +
            "Controller Position; " +
            "Pointer Position; " +
            "Target Position; " +
            "Syringe angle x; " +
            "Syringe anfle y; " +
            "Syringe angle z; " +
            "Accuracy x; " +
            "Accuracy y; " +
            "isInside; " +
            "Distance b/w Syringe and Insert position; " +
            "Total Time; ";

        // distance between syringe and target
        using (StreamWriter sw = File.AppendText(FilePathCollider))
        {
            sw.WriteLine(message);
        }
    }

    public void StartText(string text)
    {
        status.text = text;
    }

    public void StopText()
    {
        status.text = string.Empty;
    }


    public void SetParticipantNumber(string participantNumber)
    {
        this.ParticipantNumber = participantNumber;
    }

    public void SetAssistance(bool assistance)
    {
        assistanceActivated = assistance;
    }

    //public void SetVisualGuides()
    //{
    //    activateVisualGuides = FindObjectOfType<ActivateVisualGuides>();
    //}

    public Vector2 AngleOffset()
    {
        float x = 0;
        float y = 0;

        if (syringe.transform.eulerAngles.x > 180f)
        {
            x = 396.15f - syringe.transform.eulerAngles.x;
        }
        else
            x = syringe.transform.eulerAngles.x - 36f;

        if (syringe.transform.eulerAngles.y > 180f)
        {
            y = syringe.transform.eulerAngles.y - 347.85f;
        }
        else
            y = syringe.transform.eulerAngles.y + 12.15f;
        
        return new Vector2(x, y);
    }

    public void summaryWriter()
    {
        clickCounter++;
        Vector2 offset = AngleOffset();
        if (!training)
        {
            string message = ParticipantNumber + ";" +
            isNurse + ";" +
            clickCounter + ";" +
            leftHanded + ";" +
            assistanceActivated + ";" +
            controller.position + ";" +
            pointer.position + ";" +
            camera.position + ";" +
            //target.position + ";" +
            calibrationPoints[0].position.ToString() + ";" +
            calibrationPoints[1].position.ToString() + ";" +
            calibrationPoints[2].position.ToString() + ";" +
            calibrationPoints[3].position.ToString() + ";" +
            calibrationPoints[4].position.ToString() + ";" +
            calibrationPoints[5].position.ToString() + ";" +
            syringe.transform.eulerAngles.x + ";" +
            syringe.transform.eulerAngles.y + ";" + 
            syringe.transform.eulerAngles.z + ";" +
            offset.x + ";" + 
            offset.y + ";" +
            sphereInsert.distance + ";" +
            isInside + ";" +
            selectionCollider.position + ";" +
            recorderCollider.position + ";" +
            Time.time + ";" +
            (Time.time - previousTime) + ";" +
            (Time.time - previousTime) * 1000;

            previousTime = Time.time;

            using (StreamWriter sw = File.AppendText(FilePathSummary))
            {
                sw.WriteLine(message);
            }
        }
        if(clickCounter == 0)
        {
            StartText("Training session");
            Invoke("StopText", 4f);
        }
        if(clickCounter == 10)
        {
            training = false;
            //activateVisualGuides.SetActivation(assistanceActivated);
            StartText("Experiement starting: Trial 1 :: visual guides: " + assistanceActivated);
            Invoke("StopText", 4f);
        }
        
        if(clickCounter == 32)
        {
            experiementDone = true;
            text.enabled = true;
            Time.timeScale = 0;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (!armCalibration.CalibrationDone) return;
        if (!studyActive) return;

        Vector3 ballPos = pointer.position;
        var (inside, t, progress, radialDist, verticalOffset, depthOffset, allowedRadius) = currentTunnel.Evaluate(ballPos);

        if (!isSteering)
        {
            
            if (inside && t>=0)
            {
                isSteering = true;
                steeringSW.Restart();
                prevBallPos = ballPos;
                UnityEngine.Debug.Log($"[Study] Trial {currentTrialIndex} — steering started.");
                StartText("Steering Started!");
            }

            return;
        }

        if (!inside)
        {
            //currentTrial.resetCount++;
            //ResetBallToStart();
            CompleteTrial(false);
            return;
        }



        //if (!experiementDone)
        //{
        //    string message = "";
        //    //if (target != null)
        //    //{
        //    //    message = ParticipantNumber + "; " +
        //    //    isNurse + ";" +
        //    //    clickCounter + ";" +
        //    //    assistanceActivated + ";" +
        //    //    controller.position + ";" +
        //    //    pointer.position + ";" +
        //    //    target.position + ";" +
        //    //    isInside + ";" +
        //    //    Time.time;
        //    //}
        //    //else
        //    //{
        //    //    message = ParticipantNumber + "; " +
        //    //    isNurse + ";" +
        //    //    clickCounter + ";" +
        //    //    assistanceActivated + ";" +
        //    //    controller.position + ";" +
        //    //    pointer.position + ";" +
        //    //    "TargetNotSetYet;" +
        //    //    isInside + ";" +
        //    //    Time.time;
        //    //}

        //    using (StreamWriter sw = File.AppendText(FilePathAll))
        //    {
        //        sw.WriteLine(message);
        //    }



        //    if (inCollider && sphereInsert != null)
        //    {
        //        Vector2 accuracy = AngleOffset();
        //        message = ParticipantNumber + "; " +
        //        isNurse + ";" +
        //        clickCounter + ";" +
        //        assistanceActivated + ";" +
        //        controller.position + ";" +
        //        pointer.position + ";" +
        //        //target.position + ";" +
        //        syringe.transform.eulerAngles.x + ";" +
        //        syringe.transform.eulerAngles.y + ";" +
        //        syringe.transform.eulerAngles.z + ";" +
        //        accuracy.x + ";" + 
        //        accuracy.y + ";" +
        //        isInside + ";" +
        //        sphereInsert.distance + ";" +
        //        Time.time;

        //        using (StreamWriter sw = File.AppendText(FilePathCollider))
        //        {
        //            sw.WriteLine(message);
        //        }
        //    }
        //}


    }

    void CompleteTrial(bool success)
    {
        studyActive = false;
        isSteering = false;
        steeringSW.Stop();
        currentTrial.Duration = steeringSW.Elapsed.TotalMilliseconds;
        steeringSW.Reset();
        currentTrial.endTime = Time.time;
        currentTrial.completed = success;
    
        if (success)
        {
            AudioSource.PlayClipAtPoint(success_sound, Camera.main.transform.position);
            currentTrialIndex++;
        }
        else
            AudioSource.PlayClipAtPoint(error_sound, Camera.main.transform.position);

        //TODO: ComputeSummaryStats(currentTrial);
        //allTrials.Add(currentTrial);
        //TODO: AppendTrialToSummaryCSV(currentTrial);

        //TODO: SaveTrialCSV(currentTrial);
        UnityEngine.Debug.Log($"[Study] Trial {currentTrial.trialIndex} complete. " +
                  $"Resets: {currentTrial.resetCount}  " +
                  $"Duration: {currentTrial.Duration:F2}s");


        Invoke(nameof(StartNextTrial), interTrialDelay);
    }
    void InitialiseSummaryCSV()
    {
        string folderPath = Path.Combine(Application.dataPath, "Captured Data");
        // Create folder if it doesn't exist
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        summaryPath = Path.Combine(
            folderPath,
            $"{ParticipantNumber}_study_summary.csv"
        );

        using var sw = new StreamWriter(summaryPath, append: false); // overwrite if exists
        sw.WriteLine("participantID,rightHanded,index,ID," +
                     "pathLength,startWidth,endWidth,direction,condition," +
                     "time,hitNum,hitTime,pureTime,speed(mean),speed(sd)," +
                     "latOffset(mean),latOffset(sd),normLatOffset(mean),normLatOffset(sd)," +
                     "depthOffset(mean),depthOffset(sd),normDepthOffset(mean),normDepthOffset(sd)," +
                     "radialOffset(mean),radialOffset(sd),normRadialOffset(mean),normRadialOffset(sd)," +
                     "Ae,success,SDbivariate,normSDbivariate,frameCount");
    }

    void BuildTrialList()
    {
        int idx = 0;
        for (int rep = 0; rep < repetitions; rep++)
            foreach (var length in pathLengths)
                foreach (var sw in startWidths)
                    foreach (var ew in endWidths)
                        {
                            trialList.Add(new TrialConfig
                            {
                                trialIndex = idx++,
                                pathLength = length,
                                startWidth = sw,
                                endWidth = ew
                            });
                        }

        if (randomizeOrder)
            trialList.Shuffle();

        // Re-stamp indices to reflect final presentation order
        for (int i = 0; i < trialList.Count; i++)
            trialList[i].trialIndex = i;

        UnityEngine.Debug.Log($"[Study] {trialList.Count} trials generated.");
    }
    void EndStudy()
    {
        studyActive = false;
        UnityEngine.Debug.Log("[Study] All trials finished.");
    }

    
    public void StartNextTrial()
    {
        if (currentTrialIndex >= trialList.Count)
        {
            EndStudy();
            return;
        }

        var cfg = trialList[currentTrialIndex];
        UnityEngine.Debug.Log("CFG: start:" + cfg.startWidth + " end:" + cfg.endWidth + " len:" + cfg.pathLength);

        Vector3 endPt = armCalibration.insertionZone.position;
        Vector3 tunnelDir = armCalibration.insertionZone.forward;

        Vector3 startPt = endPt - tunnelDir * cfg.pathLength;

        //Vector3 startPt = scenePosition - axisDir * (cfg.pathLength * 0.5f);
        //Vector3 endPt = scenePosition + axisDir * (cfg.pathLength * 0.5f);

        currentTunnel = tunnelBuilder.BuildTunnel(cfg, startPt, endPt);
        insertPoint.transform.position = endPt;


        currentTrial = new TrialData
        {
            trialIndex = currentTrialIndex,
            startTime = Time.time,
            config = cfg
        };

        taskAxisDir = (endPt - startPt).normalized; // forward direction of the task
        ballStartPos = startPt - taskAxisDir * 0.05f; // 5 cm before start cap

        isSteering = false;
        studyActive = true;
    }

}
