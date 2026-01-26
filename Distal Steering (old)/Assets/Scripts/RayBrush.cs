using UnityEngine;
using System.Collections.Generic;

public class RayBrush : MonoBehaviour
{
    [Header("Controller and Layers")]
    public Transform controller;
    public LineRenderer linePrefab;
    public LayerMask boardLayer;
    public float rayLength = 5f;

    [Header("Stroke Offset")]
    public float strokeSurfaceOffset = 0.00001f;

    [Header("Cursor Settings")]
    public GameObject cursorPrefab;
    public float cursorScale = 0.01f;

    [Header("Ray Visual Settings")]
    public LineRenderer rayLine;        // assign a LineRenderer for the visible beam
    public float rayStartWidth = 0.002f; // fixed start width
    public Color rayColor = Color.white;

    [Header("Stroke Visual Size")]
    public Transform headTransform;          // XR camera / center eye
    public float referenceDistance = 1f;   // distance where baseStrokeWidth looks "correct"
    public float baseStrokeWidth = 0.002f;   // stroke thickness at referenceDistance

    private GameObject cursorInstance;
    private LineRenderer currentLine;
    private List<Vector3> currentStroke = new();
    private List<List<Vector3>> allStrokes = new();
    private GameManager gameManager;
    private bool hasStartedStroke = false;
    private GameObject board;
    private bool triggerSteering = false;

    [Header("Gate start/end using board-local coords")]
    public float startPlaneX = 0f;          // x=0 is the start
    public float startPlaneTolerance = 0.0001f; // meters, e.g., 1cm
    public float lateralMargin = 0.0001f;       // extra margin beyond W/2
    public int gateFrames = 1;                // 1 is fine with crossing

    private float? prevProgress = null;
    private int gateCount = 0;
    private bool gateArmed = false;
    public float gateTol = 0.0001f; // tolerance

    public List<Vector3> LastStroke { get; private set; } = new List<Vector3>(); //last completed stroke (world-space points)
    void Start()
    {
        gameManager = GetComponent<GameManager>();

        // Head / XR camera
        if (!headTransform && Camera.main != null)
            headTransform = Camera.main.transform;

        // --- Cursor setup ---
        if (cursorPrefab != null)
        {
            cursorInstance = Instantiate(cursorPrefab);
            cursorInstance.transform.localScale = Vector3.one * cursorScale;
            cursorInstance.SetActive(true);
        }

        // --- Ray visual setup ---
        if (rayLine == null)
        {
            rayLine = new GameObject("ControllerRay").AddComponent<LineRenderer>();
            rayLine.material = new Material(Shader.Find("Unlit/Color"));
            rayLine.material.color = rayColor;
            rayLine.startWidth = rayStartWidth;
            rayLine.endWidth = rayStartWidth;
            rayLine.positionCount = 2;
        }

        board = GameObject.FindWithTag("Board");
    }

    void Update()
    {
        if (board == null) return;

        bool isSteeringNow = (hasStartedStroke && gameManager != null && gameManager.isSteering);

        Ray ray = new Ray(controller.position, controller.forward);
        RaycastHit hit;
        Vector3 cursorPos;

        // Define board plane
        Plane boardPlane = new Plane(board.transform.up, board.transform.position);

        // --- Find cursor position ---
        if (Physics.Raycast(ray, out hit, rayLength, boardLayer))
        {
            // position slightly above the board along the surface normal
            Vector3 surfacePos = hit.point + hit.normal * strokeSurfaceOffset;
            cursorPos = surfacePos;

            string tag = hit.collider.tag;

            //if (!hasStartedStroke && tag == "StartPoint" && !triggerSteering)
            //{
            //    triggerSteering = true;
            //    gameManager.lockPosRot = true;
            //}

            GetBoardLocalProgressLateral(surfacePos, out float prog, out float lat);


            //if (!hasStartedStroke && triggerSteering)
            //{
            //    StartStroke();
            //}
            ////StartStroke();

            //if (!hasStartedStroke && tag == "Board" && triggerSteering)
            //    StartStroke();

            Transform bt = gameManager.boardTransform;

            // Physical sizes for this trial
            float L = gameManager.CurrentLengthP;
            float W = gameManager.CurrentWidthP;

            // Axes (based on your confirmed debug)
            Vector3 progressAxis = bt.right.normalized;    // dRight
            Vector3 lateralAxis = bt.forward.normalized;  // dFwd
            Vector3 origin = bt.position;

            // Signed coordinates
            float p = Vector3.Dot(surfacePos - origin, progressAxis);
            float latO = Vector3.Dot(surfacePos - origin, lateralAxis);

            

            float halfL = L * 0.5f;
            float halfW = W * 0.5f;

         
            bool isRL = false;
            // ---- START ----
            // Arm only when you're clearly outside the "start side"
            if (!hasStartedStroke && !triggerSteering)
            {
                bool insideWidth = Mathf.Abs(latO) <= halfW;

                // Arm only when you're clearly outside the start side AND inside width
                if ((isRL && p > halfL + gateTol) ||   // RL: start at +halfL
                     (!isRL && p < -halfL - gateTol))   // LR: start at -halfL
                {
                    gateArmed = true;
                    UnityEngine.Debug.Log("<==> TRUE");
                }

                // Start when you cross into the path AND still inside width
                if (gateArmed && insideWidth &&
                    ((isRL && p <= halfL - gateTol) ||
                     (!isRL && p >= -halfL + gateTol)))
                {
                    triggerSteering = true;
                    gameManager.lockPosRot = true;
                    StartStroke();
                }

                // Optional: disarm if you leave width before starting (prevents accidental start)
                if (!insideWidth) gateArmed = false;
            }

            // ---- DURING STROKE ----
            if (hasStartedStroke && gameManager.isSteering)
            {
              
                // FAIL if outside width at any point
                if (Mathf.Abs(latO) > halfW)
                {
                    EndStroke();
                    gameManager.EndTrial(false);
                    gateArmed = false;
                    return;
                }

                // END when reaching the end side
                if ((isRL && p <= -halfL + gateTol) ||
                    (!isRL && p >= halfL - gateTol))
                {
                    EndStroke();
                    gameManager.EndTrial(true);
                    gateArmed = false;
                    return;
                }
                if (currentStroke.Count == 0 ||
                    Vector3.Distance(currentStroke[^1], surfacePos) > 0.01f)
                {
                    AddPoint(surfacePos);
                }
            }

        }
        else
        {
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

        // --- Update cursor ---
        if (cursorInstance)
        {
            cursorInstance.SetActive(true);
            cursorInstance.transform.position = cursorPos;
            cursorInstance.transform.rotation = Quaternion.LookRotation(board.transform.forward);
        }

        // --- Update visible ray ---
        if (rayLine)
        {
            rayLine.enabled = true;
            rayLine.SetPosition(0, controller.position);
            rayLine.SetPosition(1, cursorPos);

            float cursorRadius = cursorScale * 0.5f;
            float endWidth = cursorRadius / 3f;   // 1/3 of cursor radius

            rayLine.startWidth = rayStartWidth;
            rayLine.endWidth = endWidth;
        }
    }


    private void GetBoardLocalProgressLateral(Vector3 worldPoint, out float progressX, out float lateralZ)
    {
        Vector3 local = board.transform.InverseTransformPoint(worldPoint);
        progressX = local.x;   // task axis (LR/RL)
        lateralZ = local.z;   // width axis on the plane
    }
    // --- Stroke methods (unchanged) ---
    void StartStroke()
    {
        hasStartedStroke = true;
        gameManager.OnStartTraversing();

        currentLine = Instantiate(linePrefab);
        currentLine.useWorldSpace = true; // important for world-size width

        currentStroke.Clear();
        LastStroke.Clear(); // clear last stroke when a new one starts

        // ----- FIX VISUAL STROKE SIZE FOR THIS CONDITION -----
        float dist = GetHeadToBoardDistance();
        if (referenceDistance <= 1e-4f) referenceDistance = 1.0f;

        // scale world width so visual angle is constant
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
            // store completed stroke
            allStrokes.Add(new List<Vector3>(currentStroke));
            LastStroke = new List<Vector3>(currentStroke);
        }

        // --- REMOVE VISIBLE LINE AFTER SAVING ---
        if (currentLine != null)
        {
            Destroy(currentLine.gameObject);
        }

        currentLine = null;
    }

    float GetHeadToBoardDistance()
    {
        if (!headTransform || !board)
            return referenceDistance; // fallback

        // Distance from head to the board plane (or just to its center)
        Plane boardPlane = new Plane(board.transform.forward, board.transform.position);

        Ray ray = new Ray(headTransform.position,
                          board.transform.position - headTransform.position);

        float dist;
        if (boardPlane.Raycast(ray, out dist))
            return Mathf.Max(0.01f, dist);

        // Fallback: straight distance to board center
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
