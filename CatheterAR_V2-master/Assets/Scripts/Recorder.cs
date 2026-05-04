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
    public Material pointerMaterial;

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

    public int ParticipantNumber;
    public bool leftHanded;
    public bool isNurse;

    private bool experiementDone;
    private bool training;
    //public ActivateVisualGuides activateVisualGuides;

    private Stopwatch steeringSW;
    private string summaryPath;
    private float[] pathLengths = { 0.1f, 0.2f };
    private float[] startWidths = { 0.02f, 0.04f, 0.08f };
    private float[] endWidths = { 0.01f };

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
    public Transform ballTransform;

    public AudioClip success_sound;
    public AudioClip error_sound;
    public float interTrialDelay = 1f;
    private bool pointer_reset = false;
    public Material error_tunnel_material;



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

    }

    public void StartText(string text)
    {
        status.text = text;
    }

    public void StopText()
    {
        status.text = string.Empty;
    }


    public void SetParticipantNumber(int participantNumber)
    {
        this.ParticipantNumber = participantNumber;
    }

    public void SetAssistance(bool assistance)
    {
        assistanceActivated = assistance;
    }



    public Vector2 AngleOffset()
    {
        // x is up(+) and down (-). y is left(+) and right (-)
        Vector3 syringeForward = syringe.transform.forward;
        Vector3 targetForward = armCalibration.insertionZone.forward;

        float x = Vector3.SignedAngle(Vector3.ProjectOnPlane(targetForward, Vector3.right),
                                       Vector3.ProjectOnPlane(syringeForward, Vector3.right),
                                       Vector3.right);

        float y = Vector3.SignedAngle(Vector3.ProjectOnPlane(targetForward, Vector3.up),
                                       Vector3.ProjectOnPlane(syringeForward, Vector3.up),
                                       Vector3.up);
        return new Vector2(x, y);
    }

    //public void summaryWriter()
    //{
    //    clickCounter++;
    //    Vector2 offset = AngleOffset();
    //    if (!training)
    //    {
    //        string message = ParticipantNumber + ";" +
    //        isNurse + ";" +
    //        clickCounter + ";" +
    //        leftHanded + ";" +
    //        assistanceActivated + ";" +
    //        controller.position + ";" +
    //        pointer.position + ";" +
    //        camera.position + ";" +
    //        //target.position + ";" +
    //        calibrationPoints[0].position.ToString() + ";" +
    //        calibrationPoints[1].position.ToString() + ";" +
    //        calibrationPoints[2].position.ToString() + ";" +
    //        calibrationPoints[3].position.ToString() + ";" +
    //        calibrationPoints[4].position.ToString() + ";" +
    //        calibrationPoints[5].position.ToString() + ";" +
    //        syringe.transform.eulerAngles.x + ";" +
    //        syringe.transform.eulerAngles.y + ";" + 
    //        syringe.transform.eulerAngles.z + ";" +
    //        offset.x + ";" + 
    //        offset.y + ";" +
    //        sphereInsert.distance + ";" +
    //        isInside + ";" +
    //        selectionCollider.position + ";" +
    //        recorderCollider.position + ";" +
    //        Time.time + ";" +
    //        (Time.time - previousTime) + ";" +
    //        (Time.time - previousTime) * 1000;

    //        previousTime = Time.time;

    //        using (StreamWriter sw = File.AppendText(FilePathSummary))
    //        {
    //            sw.WriteLine(message);
    //        }
    //    }
    //    if(clickCounter == 0)
    //    {
    //        StartText("Training session");
    //        Invoke("StopText", 4f);
    //    }
    //    if(clickCounter == 10)
    //    {
    //        training = false;
    //        //activateVisualGuides.SetActivation(assistanceActivated);
    //        StartText("Experiement starting: Trial 1 :: visual guides: " + assistanceActivated);
    //        Invoke("StopText", 4f);
    //    }

    //    if(clickCounter == 32)
    //    {
    //        experiementDone = true;
    //        text.enabled = true;
    //        Time.timeScale = 0;
    //    }
    //}

    // Update is called once per frame
    void Update()
    {
        if (!armCalibration.CalibrationDone)
        {
            StartText("Calbration");
            return;
        }
        if (!studyActive) return;

        Vector3 ballPos = pointer.position;
        var (inside, t, progress, radialDist, allowedRadius) = currentTunnel.Evaluate(ballPos);


        if (!isSteering)
        {
            if (!pointer_reset && !inside)
            {
                Vector3 axis = currentTunnel.endPoint - currentTunnel.startPoint;
                float axisLen = axis.magnitude;
                Vector3 axisDir = axis / axisLen;

                float l = Vector3.Dot(ballPos - currentTunnel.startPoint, axisDir);
                float ballProg = l / axisLen;

                if (ballProg < -0.05f)
                {
                    pointer_reset = true;
                    StartText("Insert!");
                }
            }

            if (pointer_reset && inside && t >= 0)
            {
                isSteering = true;
                steeringSW.Restart();
                prevBallPos = ballPos;
                UnityEngine.Debug.Log($"[Study] Trial {currentTrialIndex} — steering started.");
                StartText("Inserting...");
            }

            return;
        }

        if (!inside)
        {
            StartText("Try Again!");
            CompleteTrial(false);
            return;
        }

        RecordFrame(t, progress, radialDist, currentTunnel.WorldToTunnelLocal(ballPos, armCalibration.insertionZone).x, currentTunnel.WorldToTunnelLocal(ballPos, armCalibration.insertionZone).y, allowedRadius, inside);
        currentTrial.travelledPath += Vector3.Distance(ballPos, prevBallPos);
        prevBallPos = ballPos;
        if (t >= 0.999f)
            CompleteTrial(true);

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

    void RecordFrame(float t, float progressL, float radialDist, float lat, float depth, float allowedRadius, bool inside) //lat is up and down, depth is left right (if looking from front)
    {
        float dist = Vector3.Distance(ballTransform.position, prevBallPos);
        float speed = Time.deltaTime > 0f ? dist / Time.deltaTime : 0f;
        currentTrial.frames.Add(new FrameData
        {
            timestamp = Time.time - currentTrial.startTime,
            deltaTime = Time.deltaTime,
            ballPosition = ballTransform.position,
            tunnelT = t, //normalized progress
            tunnelL = progressL, //actual length progress
            radialDistance = radialDist,
            allowedRadius = allowedRadius,
            ballLateralOffset = lat,
            ballDepthOffset = depth,
            normalizedOffset = allowedRadius > 0f ? radialDist / allowedRadius : 0f,
            normDepthOffset = allowedRadius > 0f ? depth / allowedRadius : 0f,
            normLatOffset = allowedRadius > 0f ? lat / allowedRadius : 0f,
            isInsideTunnel = inside,
            speed = speed,
            distanceTravelled = dist
        });
    }

    void ComputeSummaryStats(TrialData trial)
    {
        if (trial.FrameCount == 0) return;
        int n = trial.FrameCount;
        float totalTravel = 0f;

        float sumSpeed = 0f;
        float sumLatO = 0f;
        float sumDepthO = 0f;
        float sumRadO = 0f;
        float sumLatO_norm = 0f;
        float sumDepthO_norm = 0f;
        float sumRadO_norm = 0f;


        foreach (var f in trial.frames)
        {
            sumSpeed += f.speed;
            sumLatO += f.ballLateralOffset;
            sumDepthO += f.ballDepthOffset;
            sumRadO += f.radialDistance;
            totalTravel += f.distanceTravelled;
            sumLatO_norm += f.normLatOffset;
            sumDepthO_norm += f.normDepthOffset;
            sumRadO_norm += f.normalizedOffset;
        }

        trial.speed = sumSpeed / n;
        trial.latOffset = sumLatO / n;
        trial.depthOffset = sumDepthO / n;
        trial.radialOffset = sumRadO / n;
        trial.n_depthOffset = sumDepthO_norm / n;
        trial.n_latOffset = sumLatO_norm / n;
        trial.n_radialOffset = sumRadO_norm / n;
        // standard deviations

        float sumSqSpeed = 0f;
        float sumSqLatO = 0f;
        float sumSqDepthO = 0f;
        float sumSqBivar = 0f;
        float sumSqRadO = 0f;
        float sumSqLatO_norm = 0f;
        float sumSqDepthO_norm = 0f;
        float sumSqBivar_norm = 0f;
        float sumSqRadO_norm = 0f;


        foreach (var f in trial.frames)
        {
            float dSpeed = f.speed - trial.speed;
            float dLat = f.ballLateralOffset - trial.latOffset;
            float dDepth = f.ballDepthOffset - trial.depthOffset;
            float dRad = f.radialDistance - trial.radialOffset;
            float dLat_norm = f.normLatOffset - trial.n_latOffset;
            float dDepth_norm = f.normDepthOffset - trial.n_depthOffset;
            float dRad_norm = f.normalizedOffset - trial.n_radialOffset;

            sumSqSpeed += dSpeed * dSpeed;
            sumSqLatO += dLat * dLat;
            sumSqDepthO += dDepth * dDepth;
            sumSqRadO += dRad * dRad;
            sumSqLatO_norm += dLat_norm * dLat_norm;
            sumSqDepthO_norm += dDepth_norm * dDepth_norm;
            sumSqRadO_norm += dRad_norm * dRad_norm;

            // bivariate
            sumSqBivar += dLat * dLat + dDepth * dDepth;
            sumSqBivar_norm += dLat_norm * dLat_norm + dDepth_norm * dDepth_norm;
        }

        // Sample standard deviation (n - 1)
        float denom = n > 1 ? n - 1 : 1;
        trial.sdSpeed = Mathf.Sqrt(sumSqSpeed / denom);
        trial.sdLatOffset = Mathf.Sqrt(sumSqLatO / denom);
        trial.sdDepthOffset = Mathf.Sqrt(sumSqDepthO / denom);
        trial.sdBivariate = Mathf.Sqrt(sumSqBivar / denom);
        trial.sdRadialOffset = Mathf.Sqrt(sumSqRadO / denom);
        trial.effectiveAmplitude = totalTravel;
        trial.sd_nDepthOffset = Mathf.Sqrt(sumSqDepthO_norm / denom);
        trial.sd_nLatOffset = Mathf.Sqrt(sumSqLatO_norm / denom);
        trial.sd_nRadOffset = Mathf.Sqrt(sumSqRadO_norm / denom);
        trial.sdBivariate_norm = Mathf.Sqrt(sumSqBivar_norm / denom);


    }

    void SaveTrialCSV(TrialData trial)
    {
        string folderPath = Path.Combine(Application.dataPath, "Captured Data");

        // Create folder if it doesn't exist
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        string path = Path.Combine(
            folderPath,
            $"{ParticipantNumber}_trial_{trial.trialIndex:00}_{trial.config.ID}.csv"
        );
        string cond_name = "narrow";
        if (trial.config.startWidth < trial.config.endWidth) cond_name = "wide";

        using var sw = new StreamWriter(path);
        sw.WriteLine("paricipantID,index,startW,endW,length,condition,direction,timestamp,deltaTime,speed,distance," +
                     "posX,posY,posZ,localX,localY,localZ," +
                     "tunnelT,tunnelL,latOffset,normLatO,depthOffset,normDepthO,radialDist,allowedRadius,normalizedOffset(rad),inside"); //tunnel t is normalized tunnel l which is progress in m

        foreach (var f in trial.frames)
        {
            sw.WriteLine(
            $"{ParticipantNumber},{trial.config.trialIndex},{trial.config.startWidth},{trial.config.endWidth},{trial.config.pathLength},{cond_name},{trial.config.direction}," +
            $"{f.timestamp},{f.deltaTime},{f.speed},{f.distanceTravelled}," +
            $"{f.ballPosition.x},{f.ballPosition.y},{f.ballPosition.z},{currentTunnel.WorldToTunnelLocal(f.ballPosition, armCalibration.insertionZone).x},{currentTunnel.WorldToTunnelLocal(f.ballPosition, armCalibration.insertionZone).y},{currentTunnel.WorldToTunnelLocal(f.ballPosition, armCalibration.insertionZone).z}," +
            $"{f.tunnelT},{f.tunnelL},{f.ballLateralOffset},{f.normLatOffset},{f.ballDepthOffset},{f.normDepthOffset},{f.radialDistance},{f.allowedRadius}," +
            $"{f.normalizedOffset},{f.isInsideTunnel}");
        }

    }

    void AppendTrialToSummaryCSV(TrialData trial)
    {
        string cond_name = "narrow";
        if (trial.config.startWidth < trial.config.endWidth) cond_name = "wide";

        Vector2 angleOffset = AngleOffset();

        using var sw = new StreamWriter(summaryPath, append: true); // append mode
        sw.WriteLine(
            $"{ParticipantNumber},{leftHanded},{trial.trialIndex},{trial.config.ID}," +
            $"{trial.config.pathLength},{trial.config.startWidth}," +
            $"{trial.config.endWidth},{trial.config.direction},{cond_name}," +
            $"{trial.Duration},{trial.HitNumber},{trial.hitTime},{trial.Duration - trial.hitTime},{trial.speed},{trial.sdSpeed}," +
            $"{trial.latOffset},{trial.sdLatOffset},{trial.n_latOffset},{trial.sd_nLatOffset}," +
            $"{trial.depthOffset},{trial.sdDepthOffset},{trial.n_depthOffset},{trial.sd_nDepthOffset}," +
            $"{trial.radialOffset},{trial.sdRadialOffset},{trial.n_radialOffset},{trial.sd_nRadOffset}," +
            $"{trial.effectiveAmplitude},{trial.completed},{trial.sdBivariate},{trial.sdBivariate_norm}," +
            $"{trial.FrameCount},{angleOffset.x},{angleOffset.y}");
    }

    void CompleteTrial(bool success)
    {
        pointer_reset = false;
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
        {
            AudioSource.PlayClipAtPoint(error_sound, Camera.main.transform.position);
            Renderer tunnel_renderer = tunnelBuilder.currentTunnelGO.GetComponent<Renderer>();
            tunnel_renderer.material = error_tunnel_material;
        }

        ComputeSummaryStats(currentTrial);
        AppendTrialToSummaryCSV(currentTrial);

        SaveTrialCSV(currentTrial);
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
        sw.WriteLine("participantID,leftHanded,index,ID," +
                     "pathLength,startWidth,endWidth,direction,condition," +
                     "time,hitNum,hitTime,pureTime,speed(mean),speed(sd)," +
                     "latOffset(mean),latOffset(sd),normLatOffset(mean),normLatOffset(sd)," +
                     "depthOffset(mean),depthOffset(sd),normDepthOffset(mean),normDepthOffset(sd)," +
                     "radialOffset(mean),radialOffset(sd),normRadialOffset(mean),normRadialOffset(sd)," +
                     "Ae,success,SDbivariate,normSDbivariate,frameCount,X_angleOffset,Y_angleOffset");
    }

    void BuildTrialList()
    {
        List<Vector3> trial_temp = new List<Vector3>();

        foreach (var length in pathLengths)
            foreach (var sw in startWidths)
                foreach (var ew in endWidths)
                {
                    trial_temp.Add(new Vector3(length, sw, ew));
                }
        int cb_index = 0;
        if (randomizeOrder)
        {
            //trialList.Shuffle();
            cb_index = ParticipantNumber % 6;
        }
        int idx = 0;

        for(int i = 0; i < repetitions; i++)
        {
            for(int j = cb_index; j < trial_temp.Count; j++)
            {
                trialList.Add(new TrialConfig
                {
                    trialIndex = idx++,
                    pathLength = trial_temp[j].x,
                    startWidth = trial_temp[j].y,
                    endWidth = trial_temp[j].z
                }) ;
            }
            for(int j = 0; j < cb_index; j++)
            {
                trialList.Add(new TrialConfig
                {
                    trialIndex = idx++,
                    pathLength = trial_temp[j].x,
                    startWidth = trial_temp[j].y,
                    endWidth = trial_temp[j].z
                });
            }

        }

        // Re-stamp indices to reflect final presentation order
        //for (int i = 0; i < trialList.Count; i++)
        //    trialList[i].trialIndex = i;

        UnityEngine.Debug.Log($"[Study] {trialList.Count} trials generated.");
        foreach(var ti in trialList)
        {
            UnityEngine.Debug.Log($"Trial {ti.trialIndex} : sw-{ti.startWidth}, ew-{ti.endWidth}, l-{ti.pathLength}");
        }
    }



    void EndStudy()
    {
        studyActive = false;
        UnityEngine.Debug.Log("[Study] All trials finished.");
        StartText("Done!");
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
        insertPoint.GetComponent<Renderer>().material = pointerMaterial;
    }

}
