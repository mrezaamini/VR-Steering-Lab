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
    private readonly List<List<Vector3>> allStrokes = new();
    private GameManager gameManager;

    private bool hasStartedStroke = false;
    public GameObject board;
    private bool triggerSteering = false;

    [Header("Linear Gate")]
    public float gateTol = 0.0001f; // meters tolerance
    private bool gateArmed = false;

    public List<Vector3> LastStroke { get; private set; } = new List<Vector3>();

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
            else
            {
                // Sine later — for now just don’t start steering
                // Debug.LogWarning($"PathType {pathType} not implemented in RayBrush yet.");
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
                gameManager.lockPosRot = true;
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
                AddPoint(surfacePos);
        }
    }

    // =========================
    // CIRCLE (temporary simple version)
    // Start: inside width, must first move "away" from gate then come back near gate.
    // End: progress reaches ~L
    // (We’ll replace with your 12 o’clock red/green gate crossing logic next.)
    // =========================
    private void HandleCircle(Vector3 surfacePos, Transform bt, float L, float W, int dir)
    {
        GetCircleProgressLateral(surfacePos, bt, L, dir, out float p, out float latO);

        float halfW = W * 0.5f;

        float startTol = Mathf.Max(0.02f * L, 0.01f); // 2% circumference or 1cm
        float awayTol = Mathf.Max(0.10f * L, 0.05f); // 10% or 5cm

        bool insideWidth = Mathf.Abs(latO) <= halfW;

        // ---- START ----
        if (!hasStartedStroke && !triggerSteering)
        {
            if (insideWidth && p > awayTol) gateArmed = true;

            if (gateArmed && insideWidth && p <= startTol)
            {
                triggerSteering = true;
                gameManager.lockPosRot = true;
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

            float endTol = Mathf.Max(0.02f * L, 0.01f);
            if (p >= L - endTol)
            {
                EndStroke();
                gameManager.EndTrial(true);
                gateArmed = false;
                return;
            }

            if (currentStroke.Count == 0 || Vector3.Distance(currentStroke[^1], surfacePos) > 0.01f)
                AddPoint(surfacePos);
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
        Vector3 xAxis = bt.right.normalized;
        Vector3 zAxis = bt.forward.normalized;

        float x = Vector3.Dot(worldPoint - origin, xAxis);
        float z = Vector3.Dot(worldPoint - origin, zAxis);

        float R = L_phys / (2f * Mathf.PI);
        float r = Mathf.Sqrt(x * x + z * z);
        lateral = (r - R);

        float theta = Mathf.Atan2(z, x);
        if (theta < 0f) theta += 2f * Mathf.PI;

        float startTheta = 0f; // +X axis
        float dTheta;

        bool isCCW = (dir == 1);

        if (isCCW)
        {
            dTheta = theta - startTheta;
            if (dTheta < 0f) dTheta += 2f * Mathf.PI;
        }
        else
        {
            dTheta = startTheta - theta;
            if (dTheta < 0f) dTheta += 2f * Mathf.PI;
        }

        progress = dTheta * R;
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

    void EndStroke()
    {
        hasStartedStroke = false;
        gameManager.isSteering = false;
        triggerSteering = false;
        gateArmed = false;

        if (currentStroke.Count > 1)
        {
            allStrokes.Add(new List<Vector3>(currentStroke));
            LastStroke = new List<Vector3>(currentStroke);
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
