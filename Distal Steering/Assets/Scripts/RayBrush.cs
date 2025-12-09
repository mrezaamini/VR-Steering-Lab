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

            if (!hasStartedStroke && tag == "StartPoint")
                StartStroke();

            if (gameManager.isSteering && hasStartedStroke)
            {
                if (currentStroke.Count == 0 ||
                    Vector3.Distance(currentStroke[^1], surfacePos) > 0.01f)
                {
                    AddPoint(surfacePos);
                }
            }

            if (hasStartedStroke && tag == "EndPoint")
            {
                EndStroke();
                gameManager.EndTrial(true);
            }
        }
        else
        {
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

    // --- Stroke methods (unchanged) ---
    void StartStroke()
    {
        hasStartedStroke = true;
        gameManager.isSteering = true;

        currentLine = Instantiate(linePrefab);
        currentLine.useWorldSpace = true; // important for world-size width

        currentStroke.Clear();

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

        if (currentStroke.Count > 1)
            allStrokes.Add(new List<Vector3>(currentStroke));
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
