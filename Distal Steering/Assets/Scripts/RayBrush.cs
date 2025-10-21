using UnityEngine;
using System.Collections.Generic;

public class RayBrush : MonoBehaviour
{
    [Header("Controller and Layers")]
    public Transform controller;
    public LineRenderer linePrefab;
    public LayerMask boardLayer;
    public float rayLength = 5f;

    [Header("Cursor Settings")]
    public GameObject cursorPrefab;
    public float cursorScale = 0.01f;

    [Header("Ray Visual Settings")]
    public LineRenderer rayLine;        // assign a LineRenderer for the visible beam
    public float rayStartWidth = 0.002f; // fixed start width
    public Color rayColor = Color.white; // optional

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
            cursorPos = hit.point;
            string tag = hit.collider.tag;

            if (!hasStartedStroke && tag == "StartPoint")
                StartStroke();

            if (gameManager.isSteering && hasStartedStroke)
            {
                if (currentStroke.Count == 0 || Vector3.Distance(currentStroke[^1], hit.point) > 0.01f)
                    AddPoint(hit.point);
            }

            if (hasStartedStroke && tag == "EndPoint")
                EndStroke();
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
        currentStroke.Clear();
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
