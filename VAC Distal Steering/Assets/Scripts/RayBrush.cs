using UnityEngine;
using System.Collections.Generic;

public class RayBrush : MonoBehaviour
{
    [Header("Controller and Layers")]
    public Transform controller;
    public Transform controller_right;
    public Transform controller_left;
    public LineRenderer linePrefab;
    public LayerMask boardLayer;
    public float rayLength = 5f;

    [Header("Stroke Offset")]
    public float strokeSurfaceOffset = 0.00001f;

    [Header("Cursor Settings")]
    public GameObject cursorPrefab;
    public float cursorScale = 0.01f;

    [Header("Ray Visual Settings")]
    public LineRenderer rayLine;         // visible beam
    public float rayStartWidth = 0.002f; // fixed start width
    public Color rayColor = Color.white;

    [Header("Stroke Visual Size")]
    public Transform headTransform;      // XR camera / center eye
    public float referenceDistance = 1f; // distance where baseStrokeWidth looks "correct"
    public float baseStrokeWidth = 0.002f;

    // Runtime
    private GameObject cursorInstance;
    private LineRenderer currentLine;
    private readonly List<Vector3> currentStroke = new();
    private readonly List<float> currentDeviationList = new();
    private readonly List<List<Vector3>> allStrokes = new();
    private GameManager gameManager;

    private bool hasStartedStroke = false;
    public GameObject board;
    private bool triggerSteering = false;

    [Header("Linear Gate")]
    public float gateTol = 0.0001f; // meters tolerance
    private bool gateArmed = false;

    public List<Vector3> LastStroke { get; private set; } = new List<Vector3>();

    public List<float> LastDeviationList { get; private set; } = new List<float>();

    // Progress state
    private bool hasPrevP = false;
    private float prevP = 0f;
    // Sine progress state
    private bool hasPrevPSine = false;
    private float prevPSine = 0f;

    // Start-zone hysteresis + lap completion
    private bool leftStartZone = false;   // have we been outside the start zone since last crossing?
    private float forwardAccum = 0f;       // unwrapped forward distance since start


    [SerializeField] bool drawStrokeGizmos = false;
    [SerializeField] float gizmoPointSize = 0.006f;

    [SerializeField] bool debugSine = true;
    [SerializeField] int debugEveryNFrames = 15;

    private SineBandGenerator sineGen;

    private bool hasPrevXSine = false;
    private float prevXSine = 0f;


    // call this when needed
    private bool TryGetSineGen()
    {
        if (sineGen != null) return true;
        if (gameManager == null || gameManager.Sine_Path == null) return false;
        sineGen = gameManager.Sine_Path.GetComponentInChildren<SineBandGenerator>(true);
        return sineGen != null;
    }

    private void GetSineProgressLateral(Vector3 surfacePos, out float p, out float lat)
    {
        p = 0f; lat = 0f;
        if (!TryGetSineGen()) return;

        // keep generator direction in sync with current trial direction (0 LR, 1 RL)
        sineGen.direction = gameManager.CurrentDirection;

        sineGen.EvalWorld(surfacePos, out p, out lat);
    }


    void OnDrawGizmos()
    {
        if (!drawStrokeGizmos) return;
        if (currentStroke == null || currentStroke.Count == 0) return;

        // Draw points
        Gizmos.color = Color.magenta;
        foreach (var p in currentStroke)
        {
            Gizmos.DrawSphere(p, gizmoPointSize);
        }

        // Draw connecting lines
        Gizmos.color = Color.yellow;
        for (int i = 1; i < currentStroke.Count; i++)
        {
            Gizmos.DrawLine(currentStroke[i - 1], currentStroke[i]);
        }
    }

    void Start()
    {
        gameManager = GetComponent<GameManager>();

        if (controller == null)
        {
            controller = controller_right;
            if (gameManager != null && !gameManager.getRightHand()) controller = controller_left;
        }

        if (!headTransform && Camera.main != null)
            headTransform = Camera.main.transform;

        // Cursor
        if (cursorPrefab != null)
        {
            cursorInstance = Instantiate(cursorPrefab);
            cursorInstance.transform.localScale = Vector3.one * cursorScale;
            cursorInstance.SetActive(true);
        }

        // Ray line
        if (rayLine == null)
        {
            var go = new GameObject("ControllerRay");
            go.transform.SetParent(controller, false);
            go.layer = controller.gameObject.layer;

            rayLine = go.AddComponent<LineRenderer>();
            rayLine.useWorldSpace = true;

            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            rayLine.material = new Material(shader);

            rayLine.startWidth = rayStartWidth;
            rayLine.endWidth = rayStartWidth;
            rayLine.positionCount = 2;
        }

        //board = GameObject.FindWithTag("Board");
        if (board == null)
            board = GameObject.FindWithTag("Board");
    }

    void Update()
    {
        if (board == null || gameManager == null) return;

        

        bool isSteeringNow = (hasStartedStroke && gameManager.isSteering);

        Ray ray = new Ray(controller.position, controller.forward);
        RaycastHit hit;
        Vector3 cursorPos;
        

        // Plane fallback (for cursor when no hit)
        Plane boardPlane = new Plane(board.transform.up, board.transform.position);
        Debug.DrawRay(ray.origin, ray.direction * rayLength, Color.magenta, 0.02f);

        if (Physics.Raycast(ray, out hit, rayLength, boardLayer))
        {
            
            Vector3 surfacePos = hit.point + hit.normal * strokeSurfaceOffset;
            cursorPos = surfacePos;

            Transform bt = gameManager.boardTransform;

            // Physical sizes for this trial (already depth-scaled in GameManager)
            float L = gameManager.CurrentLengthP;
            float W = gameManager.CurrentWidthP;

            int pathType = gameManager.CurrentPathType;   // 1=linear, 2=circle, 3=sine (later)
            int dir = gameManager.CurrentDirection;       // 0/1 (for linear LR/RL; for circle CW/CCW)

            

            if (pathType == 1)
            {
                HandleLinear(surfacePos, bt, L, W, dir);
            }
            else if (pathType == 2)
            {
                HandleCircle(surfacePos, bt, L, W, dir);
            }
            else if(pathType == 3)
            {
                HandleSine(surfacePos, bt, L, W, dir);
            }
            else
            {
                Debug.LogError("[Raybrush] Path Type Unknown!");
            }
        }
        else
        {

            // If we lose intersection during steering -> fail (same rule as your linear)
            if (isSteeringNow)
            {
                EndStroke();
                gameManager.EndTrial(false);
                gateArmed = false;
                return;
            }

            float distance;
            if (boardPlane.Raycast(ray, out distance))
                cursorPos = ray.GetPoint(distance);
            else
                cursorPos = controller.position + controller.forward * rayLength;
        }

        // Cursor update
        if (cursorInstance)
        {
            cursorInstance.SetActive(true);
            cursorInstance.transform.position = cursorPos;
            cursorInstance.transform.rotation = Quaternion.LookRotation(board.transform.forward);
        }

        // Ray visual update
        if (rayLine)
        {
            rayLine.enabled = true;
            rayLine.SetPosition(0, controller.position);
            rayLine.SetPosition(1, cursorPos);

            float cursorRadius = cursorScale * 0.5f;
            float endWidth = cursorRadius / 3f;

            rayLine.startWidth = rayStartWidth;
            rayLine.endWidth = endWidth;
        }
    }

    // =========================
    // LINEAR (existing behavior)
    // =========================
    private void HandleLinear(Vector3 surfacePos, Transform bt, float L, float W, int dir)
    {
        // Your axes setup
        Vector3 progressAxis = bt.right.normalized;
        Vector3 lateralAxis = bt.forward.normalized;
        Vector3 origin = bt.position;

        float p = Vector3.Dot(surfacePos - origin, progressAxis);
        float latO = Vector3.Dot(surfacePos - origin, lateralAxis);

        float halfL = L * 0.5f;
        float halfW = W * 0.5f;

        // Interpret direction: 0=LR, 1=RL (adjust if you use opposite)
        bool isRL = (dir == 1);

        // ---- START ----
        if (!hasStartedStroke && !triggerSteering)
        {
            bool insideWidth = Mathf.Abs(latO) <= halfW;

            // Arm outside start side
            if ((isRL && p > halfL + gateTol) || (!isRL && p < -halfL - gateTol))
                gateArmed = true;

            // Start when crossing into path from start side (still inside width)
            if (gateArmed && insideWidth &&
                ((isRL && p <= halfL - gateTol) || (!isRL && p >= -halfL + gateTol)))
            {
                triggerSteering = true;
                
                StartStroke();
            }

            if (!insideWidth) gateArmed = false;
        }

        // ---- DURING ----
        if (hasStartedStroke && gameManager.isSteering)
        {
            if (Mathf.Abs(latO) > halfW)
            {
                EndStroke();
                gameManager.EndTrial(false);
                gateArmed = false;
                return;
            }

            if ((isRL && p <= -halfL + gateTol) || (!isRL && p >= halfL - gateTol))
            {
                EndStroke();
                gameManager.EndTrial(true);
                gateArmed = false;
                return;
            }

            if (currentStroke.Count == 0 || Vector3.Distance(currentStroke[^1], surfacePos) > 0.01f)
            {
                gameManager.lockPosRot = true;
                AddPoint(surfacePos);
            }
                
        }
    }

    // =========================
    // CIRCLE
    // Start: inside width, must first move "away" from gate then come back near gate
    // =========================
    private void HandleCircle(Vector3 surfacePos, Transform bt, float L, float W, int dir)
    {
        
        GetCircleProgressLateral(surfacePos, bt, L, dir, out float p, out float latO);

        float halfW = W * 0.5f;

        // ---- Tolerances ----
        float startTol = Mathf.Max(0.02f * L, 0.01f);   // 2% or 1cm
        float endTol = Mathf.Max(0.02f * L, 0.01f);

        //float startTol = 0.1f;


        // Hysteresis around start zone: you must LEAVE a slightly bigger zone before you can trigger again.
        // preventin jitter
        float leaveTol = startTol * 2f;                 // e.g., leave beyond 2*startTol

        bool insideWidth = Mathf.Abs(latO) <= halfW;

        // ---- Wrapped delta progress (dp) ----
        float dp = 0f;
        bool movingForward = false;
        bool haveDp = false;

        float prevP_local = prevP; // capture previous before updating


        //if (Time.frameCount % 15 == 0)
        //{
           
        //    Debug.Log($"[Circle] p={p:F3}/{L:F3} latO={latO:F4} |latO|<={halfW:F4}? {insideWidth} W={W:F3}");
        //}

        if (hasPrevP)
        {
            dp = p - prevP_local;

            // wrap correction: keep dp in (-L/2, +L/2]
            if (dp > L * 0.5f) dp -= L;
            if (dp < -L * 0.5f) dp += L;

            // ignore tiny jitter
            movingForward = dp > 0.0005f; // 0.5mm
            haveDp = true;
        }

        prevP = p;
        hasPrevP = true;

        bool inStartZone = (p <= startTol);
        bool outStartZone = (p >= leaveTol);

        // Track whether they've left the start region (hysteresis)
        if (outStartZone) leftStartZone = true;

        // "Passed start in correct direction" event:
        // Only count it if:
        // - they were outside start zone before (leftStartZone == true)
        // - now they are in the start zone
        // - and motion is forward (according to dp)
        bool crossedStartForward = leftStartZone && inStartZone && haveDp && movingForward;

        // --------------------
        // START
        // --------------------
        if (!hasStartedStroke && !triggerSteering)
        {
            // Do not start unless they're valid (within width)
            if (!insideWidth)
            {
                // If they leave the corridor while waiting, reset hysteresis so they must leave+re-enter cleanly.
                leftStartZone = false;
                return;
            }

           

            // Start ONLY on a forward crossing of the start gate (with hysteresis)
            if (crossedStartForward)
            {
                triggerSteering = true;
                gameManager.lockPosRot = true;

                forwardAccum = 0f;       // start counting forward distance from the real start crossing
                leftStartZone = false;   // so we don't instantly re-trigger at the seam
                gameManager.lockPosRot = true;
                StartStroke();
            }

            return;
        }

        // --------------------
        // DURING
        // --------------------
        if (hasStartedStroke && gameManager.isSteering)
        {
            // Fail if outside corridor
            if (!insideWidth)
            {
                EndStroke();
                gameManager.EndTrial(false);
                leftStartZone = false;
                return;
            }

            // Accumulate only forward movement (unwrapped)
            if (haveDp && dp > 0f)
                forwardAccum += dp;

            // Finish rule:
            // 1) they must have accumulated ~one circumference worth of forward travel
            // 2) then they must pass the start gate forward again
            bool enoughProgress = forwardAccum >= (L - endTol);

            if (enoughProgress && crossedStartForward)
            {
                EndStroke();
                gameManager.EndTrial(true);
                leftStartZone = false;
                return;
            }

            // Record points (within width is enough; no extra center constraint)
            if (currentStroke.Count == 0 || Vector3.Distance(currentStroke[^1], surfacePos) > 0.01f)
            {
                AddPoint(surfacePos);
                AddDeviation(latO);
            }
                
        }
    }

    // Circle metric: progress along circumference, lateral = radial deviation from centerline
    private void GetCircleProgressLateral(
    Vector3 worldPoint,
    Transform bt,
    float L_phys,     // circumference (meters)
    int dir,          // 0=CW, 1=CCW
    out float progress,
    out float lateral)
    {
       
        Vector3 origin = bt.position;
        Debug.DrawRay(origin, Vector3.up * 0.2f, Color.green, 0.02f);
        Debug.DrawRay(origin, Vector3.right * 0.2f, Color.red, 0.02f);
        // For a whiteboard (vertical plane), the in-plane axes are RIGHT (horizontal) and UP (vertical)
        //Vector3 xAxis = bt.right.normalized; // horizontal on board
        //Vector3 yAxis = bt.up.normalized;    // vertical on board

        Debug.DrawLine(origin, origin + Vector3.right * 0.25f, Color.red, 0.02f);   // X
        Debug.DrawLine(origin, origin + Vector3.up * 0.25f, Color.green, 0.02f); // Y

        Vector3 xAxis = Vector3.right;
        Vector3 yAxis = Vector3.up;


        // Coordinates in board plane
        float x = Vector3.Dot(worldPoint - origin, xAxis);
        float y = Vector3.Dot(worldPoint - origin, yAxis);

        float R = L_phys / (2f * Mathf.PI);
        float r = Mathf.Sqrt(x * x + y * y);

        // radial deviation from ideal ring radius
        lateral = (r - R);

        // angle around the ring in the board plane
        float theta = Mathf.Atan2(y, x);
        if (theta < 0f) theta += 2f * Mathf.PI;

        const float startTheta = 0.5f * Mathf.PI; // 12 o'clock
        float dTheta;

        if (Time.frameCount % 30 == 0)
            Debug.Log($"startTheta(rad)={startTheta:F4} deg={startTheta * Mathf.Rad2Deg:F1}");

        Vector3 startDir = Mathf.Cos(startTheta) * xAxis + Mathf.Sin(startTheta) * yAxis;
        Debug.DrawLine(origin, origin + startDir * 0.3f, Color.yellow, 0.02f);

        bool isCCW = (dir == 1);

        if (isCCW)
        {
            dTheta = theta - startTheta;
            if (dTheta < 0f) dTheta += 2f * Mathf.PI;
        }
        else // CW
        {
            dTheta = startTheta - theta;
            if (dTheta < 0f) dTheta += 2f * Mathf.PI;
        }

        // arc-length progress in meters [0, L)
        progress = dTheta * R;


        //if (Time.frameCount % 30 == 0)
        //{
        //    Debug.Log(
        //        $"[CircleDBG] L={L_phys:F4} R={(L_phys / (2f * Mathf.PI)):F4} " +
        //        $"origin={origin} " +
        //        $"x={x:F4} y={y:F4} r={r:F4} lat={lateral:F4} " +
        //        $"theta={theta:F4} dTheta={dTheta:F4} prog={progress:F4} " +
        //        $"bt.right={xAxis} bt.up={yAxis}"
        //    );
        //}
    }

    // Kept (may be useful later)
    private void GetBoardLocalProgressLateral(Vector3 worldPoint, out float progressX, out float lateralZ)
    {
        Vector3 local = board.transform.InverseTransformPoint(worldPoint);
        progressX = local.x;
        lateralZ = local.z;
    }

    // =========================
    // Stroke methods
    // =========================
    void StartStroke()
    {
        
        hasStartedStroke = true;
        gameManager.OnStartTraversing();

        currentLine = Instantiate(linePrefab);
        currentLine.useWorldSpace = true;

        currentStroke.Clear();
        LastStroke.Clear();
        currentDeviationList.Clear();
        LastDeviationList.Clear();

        float dist = GetHeadToBoardDistance();
        if (referenceDistance <= 1e-4f) referenceDistance = 1.0f;

        float worldStrokeWidth = baseStrokeWidth * (dist / referenceDistance);
        currentLine.startWidth = worldStrokeWidth;
        currentLine.endWidth = worldStrokeWidth;
    }

    void AddPoint(Vector3 point)
    {
        currentStroke.Add(point);
        currentLine.positionCount = currentStroke.Count;
        currentLine.SetPositions(currentStroke.ToArray());
    }

    void AddDeviation(float newVal)
    {
        currentDeviationList.Add(newVal);
    }

    void EndStroke()
    {
        hasStartedStroke = false;
        gameManager.isSteering = false;
        triggerSteering = false;
        gateArmed = false;
        hasPrevXSine = false;
        hasPrevPSine = false; 

        if (currentStroke.Count > 1)
        {
            allStrokes.Add(new List<Vector3>(currentStroke));
            LastStroke = new List<Vector3>(currentStroke);
            LastDeviationList = new List<float>(currentDeviationList);
        }

        if (currentLine != null)
            Destroy(currentLine.gameObject);

        currentLine = null;
    }

    float GetHeadToBoardDistance()
    {
        if (!headTransform || !board) return referenceDistance;

        //Plane boardPlane = new Plane(board.transform.forward, board.transform.position); // switch with next if bug
        Plane boardPlane = new Plane(board.transform.up, board.transform.position);
        Ray ray = new Ray(headTransform.position, board.transform.position - headTransform.position);

        float dist;
        if (boardPlane.Raycast(ray, out dist))
            return Mathf.Max(0.01f, dist);

        return Vector3.Distance(headTransform.position, board.transform.position);
    }

    private void HandleSine(Vector3 surfacePos, Transform bt, float L, float W, int dir)
    {
        if (!TryGetSineGen()) return;

        // Keep generator direction synced (doesn't affect cap colors; GameManager handles that)
        sineGen.direction = dir;

        // Convert hit point into sine generator local space
        Vector3 lp = sineGen.transform.InverseTransformPoint(surfacePos);

        float x = lp.x; // progress axis in generator local space
        float halfW = W * 0.5f;

        // Use EvalWorld for lateral deviation from centerline (best metric)
        sineGen.EvalWorld(surfacePos, out float p, out float latO);
        bool insideWidth = Mathf.Abs(latO) <= halfW;

        // ---- Gate geometry in local-X ----
        float Lx = sineGen.Lx; // projected X span used by generator
        float startX = (dir == 0) ? (-Lx * 0.5f) : (Lx * 0.5f);  // LR start left, RL start right
        float endX = (dir == 0) ? (Lx * 0.5f) : (-Lx * 0.5f); // opposite side

        // Direction sign: LR means moving +X, RL means moving -X
        float forwardSign = (dir == 0) ? 1f : -1f;

        // “Distance from start gate along forward direction”
        // outside-start: uStart < 0
        float uStart = forwardSign * (x - startX);

        // Remaining distance to end along forward direction
        float remainingToEnd = forwardSign * (endX - x);

        // Tolerances (in meters along X)
        float startTolX = Mathf.Max(0.02f * Lx, 0.01f); // 2% of Lx or 1cm
        float endTolX = Mathf.Max(0.02f * Lx, 0.01f);
        float armTolX = startTolX; // how far outside you must be to arm

        // Forward motion check using x (more stable than p for gating)
        float dx = 0f;
        bool movingForward = false;
        if (hasPrevXSine)
        {
            dx = x - prevXSine;
            movingForward = (forwardSign * dx) > 0.0005f; // 0.5mm
        }

        if (debugSine && Time.frameCount % debugEveryNFrames == 0)
        {
            Debug.Log(
                $"[SINE DBG] dir={(dir == 0 ? "LR" : "RL")} x={x:F4} startX={startX:F4} endX={endX:F4} " +
                $"uStart={uStart:F4} remEnd={remainingToEnd:F4} | " +
                $"p={p:F4}/{L:F4} lat={latO:F4} halfW={halfW:F4} inside={insideWidth} | " +
                $"dx={dx:F5} forward={movingForward} armed={gateArmed} started={hasStartedStroke}"
            );
        }

        prevXSine = x;
        hasPrevXSine = true;

        // --------------------
        // START: enter from correct side
        // --------------------
        if (!hasStartedStroke && !triggerSteering)
        {
            if (!insideWidth)
            {
                gateArmed = false;
                return;
            }

            // Arm when sufficiently OUTSIDE start (behind the gate)
            if (uStart < -armTolX) gateArmed = true;

            // Start when armed AND cross into corridor past the start gate (uStart >= 0)
            if (gateArmed && movingForward && uStart >= 0f && uStart <= startTolX)
            {
                triggerSteering = true;
                gameManager.lockPosRot = true; // freeze path
                StartStroke();
                gateArmed = false;
            }

            return;
        }

        // --------------------
        // DURING
        // --------------------
        if (hasStartedStroke && gameManager.isSteering)
        {
            if (!insideWidth)
            {
                EndStroke();
                gameManager.EndTrial(false);
                return;
            }

            // Finish when you reach the end side (remaining distance small)
            if (movingForward && remainingToEnd <= endTolX)
            {
                EndStroke();
                gameManager.EndTrial(true);
                return;
            }

            if (currentStroke.Count == 0 || Vector3.Distance(currentStroke[^1], surfacePos) > 0.01f)
            {
                AddPoint(surfacePos);
                AddDeviation(latO); // centerline-based deviation
            }
        }
    }

    public void SaveStrokes()
    {
        var json = JsonUtility.ToJson(new StrokeContainer(allStrokes));
        System.IO.File.WriteAllText(Application.persistentDataPath + "/strokes.json", json);
        Debug.Log("Saved strokes to " + Application.persistentDataPath + "/strokes.json");
    }

    [System.Serializable]
    public class StrokeContainer
    {
        public List<List<Vector3>> strokes;
        public StrokeContainer(List<List<Vector3>> s) => strokes = s;
    }
}
