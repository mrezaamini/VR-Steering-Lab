using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExperimentUI : MonoBehaviour
{
    [Header("References")]
    public GameManager gameManager;

    [Header("UI Settings")]
    public bool showUI = true;
    public bool showInBuild = true;

    [Header("Position & Size")]
    public TextAnchor screenPosition = TextAnchor.UpperLeft;
    public int fontSize = 18;
    public int padding = 10;
    public int boxWidth = 400;

    [Header("Colors")]
    public Color backgroundColor = new Color(0, 0, 0, 0.7f);
    public Color textColor = Color.white;
    public Color highlightColor = Color.yellow;
    public Color controllerColor = Color.cyan;
    public Color eyeGazeColor = Color.green;

    private GUIStyle boxStyle;
    private GUIStyle labelStyle;
    private GUIStyle headerStyle;
    private GUIStyle valueStyle;
    private Texture2D backgroundTexture;

    // Cached values
    private string currentConditionName = "";
    private string currentDepthInfo = "";
    private string currentSizeInfo = "";
    private string currentDistanceInfo = "";
    private string currentInteractionInfo = "";
    private string currentSelectionInfo = "";
    private string currentTrialInfo = "";
    private string currentClickInfo = "";
    // Start is called before the first frame update
    void Start()
    {
        CreateStyles();

        if (gameManager == null)
        {
            gameManager = FindObjectOfType<GameManager>();
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (gameManager == null) return;
        UpdateCachedValues();
    }

    void CreateStyles()
    {
        backgroundTexture = new Texture2D(1, 1);
        backgroundTexture.SetPixel(0, 0, backgroundColor);
        backgroundTexture.Apply();

        boxStyle = new GUIStyle();
        boxStyle.normal.background = backgroundTexture;
        boxStyle.padding = new RectOffset(padding, padding, padding, padding);

        labelStyle = new GUIStyle();
        labelStyle.fontSize = fontSize;
        labelStyle.normal.textColor = textColor;
        labelStyle.alignment = TextAnchor.MiddleLeft;

        headerStyle = new GUIStyle();
        headerStyle.fontSize = fontSize + 4;
        headerStyle.fontStyle = FontStyle.Bold;
        headerStyle.normal.textColor = highlightColor;
        headerStyle.alignment = TextAnchor.MiddleCenter;

        valueStyle = new GUIStyle();
        valueStyle.fontSize = fontSize;
        valueStyle.fontStyle = FontStyle.Bold;
        valueStyle.normal.textColor = highlightColor;
        valueStyle.alignment = TextAnchor.MiddleRight;
    }

    void UpdateCachedValues()
    {
        //// Condition name
        //int conditionCode = GetCurrentConditionCode();
        //currentConditionName = CounterbalanceManager.GetConditionName(conditionCode);

        // Depth
        float depthMeters = gameManager.getCurrentDepth();
        currentDepthInfo = $"Depth: ({depthMeters:F2}m)";

        // path width 
        float width_ang = gameManager.getCurrentWidth();
        currentSizeInfo = $"Path Width: ({width_ang:F2} deg)";

        // Sphere Distance
        float length_ang = gameManager.getCurrentLen();
        currentDistanceInfo = $"Path Length: ({length_ang:F2} deg)";

        // Interaction Method
        currentInteractionInfo = "Controller (R)";
        if(!gameManager.getRightHand()) currentInteractionInfo = "Controller (L)";

        // Selection Method
        currentSelectionInfo = "Index";

        // Trial info - 4 conditions for 2 depths
        float overall_progress = gameManager.getCurrentTrial();
        int overall_count = gameManager.getTrialCount();
        currentTrialInfo = $"Progress: ({((overall_progress+1)/overall_count)*100:F2} %)";
        //Debug.Log("progress: " + overall_progress);

        // Rep number
        currentClickInfo = $"Rep: {gameManager.getCurrentRep()+1} / 3";
    }

    
    void OnGUI()
    {
        if (!showUI) return;
        if (!showInBuild && !Application.isEditor) return;
        if (gameManager == null) return;

        if (boxStyle == null) CreateStyles();

        Rect boxRect = CalculateBoxRect();
        GUI.Box(boxRect, "", boxStyle);

        GUILayout.BeginArea(new Rect(boxRect.x + padding, boxRect.y + padding,
                                      boxRect.width - padding * 2, boxRect.height - padding * 2));

        DrawContent();

        GUILayout.EndArea();
    }

    Rect CalculateBoxRect()
    {
        int boxHeight = 350;
        int x = padding;
        int y = padding;

        switch (screenPosition)
        {
            case TextAnchor.UpperLeft:
                x = padding;
                y = padding;
                break;
            case TextAnchor.UpperCenter:
                x = (Screen.width - boxWidth) / 2;
                y = padding;
                break;
            case TextAnchor.UpperRight:
                x = Screen.width - boxWidth - padding;
                y = padding;
                break;
            case TextAnchor.MiddleLeft:
                x = padding;
                y = (Screen.height - boxHeight) / 2;
                break;
            case TextAnchor.MiddleCenter:
                x = (Screen.width - boxWidth) / 2;
                y = (Screen.height - boxHeight) / 2;
                break;
            case TextAnchor.MiddleRight:
                x = Screen.width - boxWidth - padding;
                y = (Screen.height - boxHeight) / 2;
                break;
            case TextAnchor.LowerLeft:
                x = padding;
                y = Screen.height - boxHeight - padding;
                break;
            case TextAnchor.LowerCenter:
                x = (Screen.width - boxWidth) / 2;
                y = Screen.height - boxHeight - padding;
                break;
            case TextAnchor.LowerRight:
                x = Screen.width - boxWidth - padding;
                y = Screen.height - boxHeight - padding;
                break;
        }

        return new Rect(x, y, boxWidth, boxHeight);
    }

    void DrawContent()
    {
        // Header
        GUILayout.Label($"<== EXPERIMENT STATE ==>", headerStyle);
        GUILayout.Space(5);

        // Participant
        DrawRow("Participant:", $"P{gameManager.getPID()}");

        GUILayout.Space(3);

        // Current Condition
        Color condColor = controllerColor;
        //DrawRowColored("Condition:", currentConditionName, condColor);

        GUILayout.Space(8);
        GUILayout.Label("== Parameters ==", labelStyle);
        GUILayout.Space(3);

        // Depth
        DrawRow(">", currentDepthInfo);

        // Size
        DrawRow(">", currentSizeInfo);

        // Distance
        DrawRow(">", currentDistanceInfo);

        GUILayout.Space(8);
        GUILayout.Label("== Input Method ==", labelStyle);
        GUILayout.Space(3);

        // Interaction
        DrawRowColored("Pointing:", currentInteractionInfo, condColor);

        // Selection
        DrawRowColored("Stroke:", currentSelectionInfo, condColor);

        GUILayout.Space(8);
        GUILayout.Label("== Progress ==", labelStyle);
        GUILayout.Space(3);

        // Trial
        DrawRow(">", currentTrialInfo);

        // Clicks
        DrawRow(">", currentClickInfo);
    }

    void DrawRow(string label, string value)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label(label, labelStyle, GUILayout.Width(boxWidth * 0.45f));
        GUILayout.Label(value, valueStyle, GUILayout.Width(boxWidth * 0.45f));
        GUILayout.EndHorizontal();
    }

    void DrawRowColored(string label, string value, Color color)
    {
        GUIStyle coloredValue = new GUIStyle(valueStyle);
        coloredValue.normal.textColor = color;

        GUILayout.BeginHorizontal();
        GUILayout.Label(label, labelStyle, GUILayout.Width(boxWidth * 0.45f));
        GUILayout.Label(value, coloredValue, GUILayout.Width(boxWidth * 0.45f));
        GUILayout.EndHorizontal();
    }

    public void ToggleUI()
    {
        showUI = !showUI;
    }

    public void SetUIVisible(bool visible)
    {
        showUI = visible;
    }
}

