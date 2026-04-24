using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System.Diagnostics;

public class GameManager : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // Inspector
    // -------------------------------------------------------------------------
    private float[] pathLengths = { 0.2f, 0.4f };
    private float[] startWidths = { 0.02f };
    private float[] endWidths = { 0.08f };
    private MovementDirection[] directions = { MovementDirection.LeftToRight,
                                                  MovementDirection.RightToLeft };
    private int repetitions = 2;
    public bool randomizeOrder = true;

    [Header("Participant")]
    public int participantID;
    public bool isRightHanded;

    [Header("Objects")]
    [SerializeField] private Material invisibleHand_material;
    [SerializeField] private Material originalHand_material;
    [SerializeField] private GameObject rightHandObject;
    [SerializeField] private GameObject leftHandObject;
    [SerializeField] private Material error_tunnel_material;


    [Header("Scene References")]
    public Transform ballTransform;
    public GameObject cursor;
    public TunnelBuilder tunnelBuilder;
    public BallController ballController;
    public GameObject startButton;
    public AudioClip success_sound;
    public AudioClip error_sound;
    private GameObject mainCamera;
    private bool calibrationStatus = false; //should be false at start
    private Vector3 scenePosition;
    private float offset_depth = 0.35f;
    private float offset_height = 0.15f;

    [Header("Pause Between Trials (seconds)")]
    public float interTrialDelay = 1f;


    // -------------------------------------------------------------------------
    // Runtime state
    // -------------------------------------------------------------------------

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
    private float BALL_DIAMETER = 0.01f; // for calculating the lateral freedom

    private Stopwatch steeringSW;
    private Stopwatch contactSW;

    public DebugText UI_Debug;
    private string summaryPath;
    private bool boundaryContactFlag;
    
    // -------------------------------------------------------------------------
    // Unity lifecycle
    // -------------------------------------------------------------------------
    void Awake()
    {
        // Hide ball immediately at runtime instead of deactivating in editor
        ballController.HideBall();
    }

    void Start()
    {
        mainCamera = Camera.main.gameObject;
        steeringSW = new Stopwatch();
        contactSW = new Stopwatch();
        InitialiseSummaryCSV();
        BuildTrialList();

        
    }

    void Update()
    {
        if (!calibrationStatus) return;
       
        if (!studyActive) return;

        Vector3 ballPos = ballTransform.position;
        
        if (!isSteering)
        {
            
            float forwardDisplacement = Vector3.Dot(ballPos - ballStartPos, taskAxisDir);
            // Lateral distance from the task axis
            Vector3 closestOnAxis = ballStartPos + taskAxisDir * forwardDisplacement;
            float distToAxis = Vector3.Distance(ballPos, closestOnAxis);

            bool enteredPath = forwardDisplacement >= 0.05f;         // >=5 cm forward
            bool withinBoundary = distToAxis <= currentTunnel.startRadius;

            UI_Debug.updateText("enteredPath:"+enteredPath+" withinBoundary:"+withinBoundary+" dis:"+ forwardDisplacement+ " distToAx:"+distToAxis);

            if (enteredPath && withinBoundary)
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
                prevBallPos = ballPos;
                UnityEngine.Debug.Log($"[Study] Trial {currentTrialIndex} — steering started.");
            }

            return; 
        }

        // Active steering 
        //inside, tClamped, l, radialDist,vOffset, dOffset, allowedRadius
        var (inside, t, progress, radialDist, verticalOffset, depthOffset, allowedRadius) = currentTunnel.Evaluate(ballPos);
        UI_Debug.updateText("inside:"+inside+" t:"+t+" distance:"+radialDist+" allowed:"+allowedRadius);
        if (!inside)
        {
            //currentTrial.resetCount++;
            //ResetBallToStart();
            CompleteTrial(false);
            return;
        }

        RecordFrame(t, progress, radialDist, verticalOffset, depthOffset, allowedRadius, inside);

        if(!boundaryContactFlag && radialDist >= allowedRadius-0.005) //ball radius threshold
        {
            //contact with boundary
            boundaryContactFlag = true;
            Renderer tunnel_renderer = tunnelBuilder.currentTunnelGO.GetComponent<Renderer>();
            tunnel_renderer.material = error_tunnel_material;
            currentTrial.HitNumber++;
            contactSW.Restart();
        }

        if (boundaryContactFlag && radialDist < allowedRadius - 0.005)
        {
            //back inside
            boundaryContactFlag = false;
            Renderer tunnel_renderer = tunnelBuilder.currentTunnelGO.GetComponent<Renderer>();
            tunnel_renderer.material = tunnelBuilder.tunnelMaterial;
            contactSW.Stop(); // to make sure it is stopped at the end
            currentTrial.hitTime += contactSW.Elapsed.TotalMilliseconds;
            contactSW.Reset();
        }
        

        currentTrial.travelledPath += Vector3.Distance(ballPos, prevBallPos);
        prevBallPos = ballPos;

        if (t >= 0.999f)
            CompleteTrial(true);
    }

    public void startExperiment()
    {
        Invoke("CalbrationSetup", 0.5f);

    }

    void CalbrationSetup()
    {
       startButton.SetActive(false);
       Vector3 cameraForward = mainCamera.transform.forward;
       cameraForward.y = 0f;
       cameraForward = cameraForward.normalized;
       Vector3 startPos = new Vector3(0f, mainCamera.transform.position.y, mainCamera.transform.position.z);
       scenePosition = startPos + cameraForward*0.35f + Vector3.down*0.15f;
       
        calibrationStatus = true;
        currentTrialIndex++;
        StartNextTrial();
    }

    // -------------------------------------------------------------------------
    // Trial list
    // -------------------------------------------------------------------------

    void BuildTrialList()
    {
        int idx = 0;
        for (int rep = 0; rep < repetitions; rep++)
            foreach (var length in pathLengths)
                foreach (var sw in startWidths)
                    foreach (var ew in endWidths)
                        foreach (var dir in directions)
                        {
                            trialList.Add(new TrialConfig
                            {
                                trialIndex = idx++,
                                pathLength = length,
                                startWidth = sw,
                                endWidth = ew,
                                direction = dir
                            });
                        }

        if (randomizeOrder)
            trialList.Shuffle();

        // Re-stamp indices to reflect final presentation order
        for (int i = 0; i < trialList.Count; i++)
            trialList[i].trialIndex = i;

        UnityEngine.Debug.Log($"[Study] {trialList.Count} trials generated.");
    }

    // -------------------------------------------------------------------------
    // Trial lifecycle
    // -------------------------------------------------------------------------

    void StartNextTrial()
    { 
        if (currentTrialIndex >= trialList.Count)
        {
            EndStudy();
            return;
        }

        var cfg = trialList[currentTrialIndex];
        UnityEngine.Debug.Log("CFG: start:" + cfg.startWidth + " end:" + cfg.endWidth + " len:" + cfg.pathLength);

        Vector3 axisDir = cfg.direction == MovementDirection.LeftToRight
                          ? Vector3.right
                          : Vector3.left;

        Vector3 startPt = scenePosition - axisDir * (cfg.pathLength * 0.5f);
        Vector3 endPt = scenePosition + axisDir * (cfg.pathLength * 0.5f);

        currentTunnel = tunnelBuilder.BuildTunnel(cfg, startPt, endPt);
        

        currentTrial = new TrialData
        {
            trialIndex = currentTrialIndex,
            startTime = Time.time,
            config = cfg
        };

        taskAxisDir = (endPt - startPt).normalized; // forward direction of the task
        ballStartPos = startPt - taskAxisDir * 0.05f; // 5 cm before start cap
        ResetBallToStart();
        
        isSteering = false;
        studyActive = true;
        boundaryContactFlag = false;

        UnityEngine.Debug.Log($"[Study] Trial {currentTrialIndex} — {cfg.ID}");
    }

    void ResetBallToStart()
    {
        cursor.SetActive(true);
        ballController.TeleportTo(ballStartPos);

        //prevBallPos = ballStartPos;
    }

    void CompleteTrial(bool success)
    {
        studyActive = false;
        isSteering = false;
        steeringSW.Stop();
        currentTrial.Duration = steeringSW.Elapsed.TotalMilliseconds;
        steeringSW.Reset();
        contactSW.Stop(); // to make sure it is stopped at the end
        currentTrial.hitTime += contactSW.Elapsed.TotalMilliseconds;
        contactSW.Reset();
        currentTrial.endTime = Time.time;
        currentTrial.completed = success;
        cursor.SetActive(false);

        if (isRightHanded)
        {
            rightHandObject.GetComponent<SkinnedMeshRenderer>().material = originalHand_material;
        }
        else
        {
            leftHandObject.GetComponent<SkinnedMeshRenderer>().material = originalHand_material;
        }

        if (success)
        {
            AudioSource.PlayClipAtPoint(success_sound, Camera.main.transform.position);
            currentTrialIndex++;
        }
        else
            AudioSource.PlayClipAtPoint(error_sound, Camera.main.transform.position);

        ComputeSummaryStats(currentTrial);
        //allTrials.Add(currentTrial);
        AppendTrialToSummaryCSV(currentTrial);

        SaveTrialCSV(currentTrial);
        UnityEngine.Debug.Log($"[Study] Trial {currentTrial.trialIndex} complete. " +
                  $"Resets: {currentTrial.resetCount}  " +
                  $"Duration: {currentTrial.Duration:F2}s");
        

        Invoke(nameof(StartNextTrial), interTrialDelay);
    }

    void EndStudy()
    {
        //SaveSummaryCSV();
        UnityEngine.Debug.Log("[Study] All trials finished.");
    }

    // -------------------------------------------------------------------------
    // Recording
    // -------------------------------------------------------------------------
  
    void RecordFrame(float t, float progressL, float radialDist, float lat, float depth, float allowedRadius, bool inside)
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
        }) ;
    }

    // -------------------------------------------------------------------------
    // Stats
    // -------------------------------------------------------------------------

    void ComputeSummaryStats(TrialData trial)
    {
        if (trial.FrameCount == 0) return;
        int n = trial.FrameCount;
        float totalTravel = 0f;
        //float sumOffset = 0f;

        //foreach (var f in trial.frames)
        //{
        //    sumOffset += f.normalizedOffset;
        //    if (f.radialDistance > trial.maxRadialDistance)
        //        trial.maxRadialDistance = f.radialDistance;
        //}

        //trial.avgNormalizedOffset = trial.frames.Count > 0
        //    ? sumOffset / trial.frames.Count
        //    : 0f;
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

    // -------------------------------------------------------------------------
    // CSV export
    // -------------------------------------------------------------------------

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
            $"{participantID}_trial_{trial.trialIndex:00}_{trial.config.ID}.csv"
        );
        string cond_name = "narrow";
        if (trial.config.startWidth < trial.config.endWidth) cond_name = "wide";
        
        using var sw = new StreamWriter(path);
        sw.WriteLine("paricipantID,index,startW,endW,length,condition,direction,timestamp,deltaTime,speed,distance," +
                     "posX,posY,posZ," +
                     "tunnelT,tunnelL,latOffset,normLatO,depthOffset,normDepthO,radialDist,allowedRadius,normalizedOffset(rad),inside"); //tunnel t is normalized tunnel l which is progress in m

        foreach (var f in trial.frames)
        {
                sw.WriteLine(
                $"{participantID},{trial.config.trialIndex},{trial.config.startWidth},{trial.config.endWidth},{trial.config.pathLength},{cond_name},{trial.config.direction}," +
                $"{f.timestamp},{f.deltaTime},{f.speed},{f.distanceTravelled}," +
                $"{f.ballPosition.x},{f.ballPosition.y},{f.ballPosition.z}," +
                $"{f.tunnelT},{f.tunnelL},{f.ballLateralOffset},{f.normLatOffset},{f.ballDepthOffset},{f.normDepthOffset},{f.radialDistance},{f.allowedRadius}," +
                $"{f.normalizedOffset},{f.isInsideTunnel}");
        }
            
    }

    //void SaveSummaryCSV()
    //{
    //    string path = Path.Combine(Application.persistentDataPath, "study_summary.csv");

    //    using var sw = new StreamWriter(path);
    //    sw.WriteLine("trialIndex,trialID," +
    //                 "pathLength,startWidth,endWidth,direction," +
    //                 "duration,completed,resets," +
    //                 "avgNormOffset,maxRadialDist,travelledPath,frameCount");

    //    foreach (var t in allTrials)
    //        sw.WriteLine(
    //            $"{t.trialIndex},{t.config.ID}," +
    //            $"{t.config.pathLength},{t.config.startWidth}," +
    //            $"{t.config.endWidth},{t.config.direction}," +
    //            $"{t.Duration:F3},{t.completed},{t.resetCount}," +
    //            $"{t.avgNormalizedOffset:F4},{t.maxRadialDistance:F4}," +
    //            $"{t.travelledPath:F4},{t.FrameCount}");
    //}


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
            $"{participantID}_study_summary.csv"
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

    void AppendTrialToSummaryCSV(TrialData trial)
    {
        string cond_name = "narrow";
        if (trial.config.startWidth < trial.config.endWidth) cond_name = "wide";

        using var sw = new StreamWriter(summaryPath, append: true); // append mode
        sw.WriteLine(
            $"{participantID},{isRightHanded},{trial.trialIndex},{trial.config.ID}," +
            $"{trial.config.pathLength},{trial.config.startWidth}," +
            $"{trial.config.endWidth},{trial.config.direction},{cond_name}," +
            $"{trial.Duration},{trial.HitNumber},{trial.hitTime},{trial.Duration-trial.hitTime},{trial.speed},{trial.sdSpeed}," +
            $"{trial.latOffset},{trial.sdLatOffset},{trial.n_latOffset},{trial.sd_nLatOffset}," +
            $"{trial.depthOffset},{trial.sdDepthOffset},{trial.n_depthOffset},{trial.sd_nDepthOffset}," +
            $"{trial.radialOffset},{trial.sdRadialOffset},{trial.n_radialOffset},{trial.sd_nRadOffset}," +
            $"{trial.effectiveAmplitude},{trial.completed},{trial.sdBivariate},{trial.sdBivariate_norm}," +
            $"{trial.FrameCount}");
    }
}