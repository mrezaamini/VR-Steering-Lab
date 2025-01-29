using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PilotPlaceManager : MonoBehaviour
{
    [Header("Participant Info")]
    public int participantID;
    public bool shoulderBreadth; // true: long, false: short
    public bool isRightHanded;

    [Header("Game Objects")]

    public GameObject wirePrefab;
    [SerializeField] private List<GameObject> ringPrefabs;
    private Vector3 targetPosition;

    


    private string trackingOutputFile;
    private float trialW;
    private float trialL;
    private Quaternion trialR;

    public int currentTrial = 0;

    [SerializeField] private List<GameObject> wires;

    private GameObject currentWire;
    private GameObject currentRing;

    private bool isTraversingWire = false;

    private List<Vector2> indexOfDiffs = new List<Vector2> // L (wire), W (ring diameter), wire diameter is fixed to 0.01 m
    {
        new Vector2(0.20f, 0.04f),
        new Vector2(0.20f, 0.08f),
        new Vector2(0.30f, 0.04f),
        new Vector2(0.30f, 0.08f)
    };

    private List<Quaternion> wireRotations = new List<Quaternion> { 
        // main axes
        Quaternion.Euler(0, 0, 0),
        Quaternion.Euler(0, 0, 90),
        Quaternion.Euler(0, 0, 180),
        Quaternion.Euler(0, 0, 270),
        Quaternion.Euler(90, 0, 0),
        Quaternion.Euler(270, 0, 0),
    };

    private List<float> lateralOffsets = new List<float> { -0.2f, 0.0f, 0.2f };
    private Vector3 scenePosition;

    private GameObject sphere;
    private GameObject mainCamera;

    private bool calibrationStatus = false;
    public GameObject startButton;

    // Start is called before the first frame update
    void Start()
    {
        mainCamera = Camera.main.gameObject;
        sphere = GameObject.CreatePrimitive(PrimitiveType.Cube);
        sphere.transform.localScale = new Vector3(0.01f, 0.01f, 0.25f);

    }

    // Update is called once per frame
    void Update()
    {
        if (calibrationStatus)
        {
            Debug.Log("YAY, started");
        }
    }

    public void startExperiment()
    {
        Invoke("CalbrationSetup", 0.5f);
        
    }

    void CalbrationSetup()
    {
        startButton.SetActive(false);
        Vector3 cameraForward = mainCamera.transform.forward;
        Vector3 startPos = new Vector3(0f, mainCamera.transform.position.y, mainCamera.transform.position.z);
        Vector3 scenePosition = startPos + cameraForward * 0.4f;
        Debug.Log(startPos);
        calibrationStatus = true;
        sphere.transform.position = scenePosition;
    }
}
