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
    private float[] startWidths = { 0.01f };
    private float[] endWidths = { 0.08f };
    private MovementDirection[] directions = { MovementDirection.LeftToRight,
                                                  MovementDirection.RightToLeft };
    private int repetitions = 2;
    public bool randomizeOrder = true;

    [Header("Participant")]
    public int participantID;
    public bool isRightHanded;
    [SerializeField] private Material invisibleHand_material;
    [SerializeField] private Material originalHand_material;
    [SerializeField] private GameObject rightHandObject;
    [SerializeField] private GameObject leftHandObject;


    [Header("Scene References")]
    public Transform ballTransform;
    public TunnelBuilder tunnelBuilder;
    public BallController ballController;
    public GameObject startButton;
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
    private int currentTrialIndex = -1;
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
    //TODO: check width and lateral freedom in generation: should be the condition-ball diameter
    // -------------------------------------------------------------------------
    // Unity lifecycle
    // -------------------------------------------------------------------------

    void Start()
    {
        mainCamera = Camera.main.gameObject;
        steeringSW = new Stopwatch();
        contactSW = new Stopwatch();
        BuildTrialList();
        
    }

    void Update()
    {
        if (!studyActive) return;


        if (!calibrationStatus) return;

        Vector3 ballPos = ballTransform.position;

        if (!isSteering)
        {
            
            float forwardDisplacement = Vector3.Dot(ballPos - ballStartPos, taskAxisDir);
            // Lateral distance from the task axis
            Vector3 closestOnAxis = ballStartPos + taskAxisDir * forwardDisplacement;
            float distToAxis = Vector3.Distance(ballPos, closestOnAxis);

            bool enteredPath = Mathf.Abs(forwardDisplacement - 0.05f) < 0.0001f;         // >=5 cm forward
            bool withinBoundary = distToAxis <= currentTunnel.startRadius + BALL_DIAMETER;

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
                UnityEngine.Debug.Log($"[Study] Trial {currentTrialIndex} — steering started.");
            }

            return; 
        }

        // Active steering 
        var (inside, t, radialDist, allowedRadius) = currentTunnel.Evaluate(ballPos);
        if (!inside)
        {
            currentTrial.resetCount++;
            ResetBallToStart();
            return;
        }

        RecordFrame(t, radialDist, allowedRadius, inside);

        currentTrial.travelledPath += Vector3.Distance(ballPos, prevBallPos);
        prevBallPos = ballPos;

        if (t >= 0.999f)
            CompleteTrial(success: true);
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
        currentTrialIndex++;

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

        UnityEngine.Debug.Log($"[Study] Trial {currentTrialIndex} — {cfg.ID}");
    }

    void ResetBallToStart()
    {
        ballController.TeleportTo(ballStartPos);
        prevBallPos = ballStartPos;
    }

    void CompleteTrial(bool success)
    {
        studyActive = false;
        currentTrial.endTime = Time.time;
        currentTrial.completed = success;

        ComputeSummaryStats(currentTrial);
        allTrials.Add(currentTrial);

        SaveTrialCSV(currentTrial);
        UnityEngine.Debug.Log($"[Study] Trial {currentTrial.trialIndex} complete. " +
                  $"Resets: {currentTrial.resetCount}  " +
                  $"Duration: {currentTrial.Duration:F2}s");

        Invoke(nameof(StartNextTrial), interTrialDelay);
    }

    void EndStudy()
    {
        SaveSummaryCSV();
        UnityEngine.Debug.Log("[Study] All trials finished.");
    }

    // -------------------------------------------------------------------------
    // Recording
    // -------------------------------------------------------------------------

    void RecordFrame(float t, float radialDist, float allowedRadius, bool inside)
    {
        currentTrial.frames.Add(new FrameData
        {
            timestamp = Time.time - currentTrial.startTime,
            deltaTime = Time.deltaTime,
            ballPosition = ballTransform.position,
            ballVelocity = ballController.Velocity,
            tunnelT = t,
            radialDistance = radialDist,
            allowedRadius = allowedRadius,
            normalizedOffset = allowedRadius > 0f ? radialDist / allowedRadius : 0f,
            isInsideTunnel = inside
        });
    }

    // -------------------------------------------------------------------------
    // Stats
    // -------------------------------------------------------------------------

    void ComputeSummaryStats(TrialData trial)
    {
        float sumOffset = 0f;

        foreach (var f in trial.frames)
        {
            sumOffset += f.normalizedOffset;
            if (f.radialDistance > trial.maxRadialDistance)
                trial.maxRadialDistance = f.radialDistance;
        }

        trial.avgNormalizedOffset = trial.frames.Count > 0
            ? sumOffset / trial.frames.Count
            : 0f;
    }

    // -------------------------------------------------------------------------
    // CSV export
    // -------------------------------------------------------------------------

    void SaveTrialCSV(TrialData trial)
    {
        string path = Path.Combine(Application.persistentDataPath,
                                   $"trial_{trial.trialIndex:00}_{trial.config.ID}.csv");

        using var sw = new StreamWriter(path);
        sw.WriteLine("timestamp,deltaTime," +
                     "posX,posY,posZ," +
                     "velX,velY,velZ," +
                     "tunnelT,radialDist,allowedRadius,normalizedOffset,inside");

        foreach (var f in trial.frames)
            sw.WriteLine(
                $"{f.timestamp:F4},{f.deltaTime:F4}," +
                $"{f.ballPosition.x:F4},{f.ballPosition.y:F4},{f.ballPosition.z:F4}," +
                $"{f.ballVelocity.x:F4},{f.ballVelocity.y:F4},{f.ballVelocity.z:F4}," +
                $"{f.tunnelT:F4},{f.radialDistance:F4},{f.allowedRadius:F4}," +
                $"{f.normalizedOffset:F4},{f.isInsideTunnel}");
    }

    void SaveSummaryCSV()
    {
        string path = Path.Combine(Application.persistentDataPath, "study_summary.csv");

        using var sw = new StreamWriter(path);
        sw.WriteLine("trialIndex,trialID," +
                     "pathLength,startWidth,endWidth,direction," +
                     "duration,completed,resets," +
                     "avgNormOffset,maxRadialDist,travelledPath,frameCount");

        foreach (var t in allTrials)
            sw.WriteLine(
                $"{t.trialIndex},{t.config.ID}," +
                $"{t.config.pathLength},{t.config.startWidth}," +
                $"{t.config.endWidth},{t.config.direction}," +
                $"{t.Duration:F3},{t.completed},{t.resetCount}," +
                $"{t.avgNormalizedOffset:F4},{t.maxRadialDistance:F4}," +
                $"{t.travelledPath:F4},{t.FrameCount}");
    }
}