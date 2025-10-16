//using System.IO;
//using System.Collections.Generic;
//using UnityEngine;

//public class GameManager : MonoBehaviour
//{
//    //TO CHANGE: just for visualizing the hit point
//    //public GameObject dotPrefab; 
//    //private GameObject currentDot;
//    public DebugText debugText;

//    // participant based variables
//    public int participantID;
//    public bool rightHanded;
//    private List<(Vector2, Quaternion)> participantTrials;
//    public GameObject wirePrefab;
//    [SerializeField] private List<GameObject> ringPrefabs; // contains 3 different rings of experiment
//    private Vector3 targetPosition;

//    // for saving tracking information
//    private string trackingOutputFile;
//    private float trialW;
//    private float trialL;
//    private Quaternion trialR;


//    // for single condition

//    public int currentTrial = 0;

//    [SerializeField] private List<GameObject> wires;
   
//    private GameObject currentWire;
//    private GameObject currentRing;
//    private HashSet<GameObject> visitedWires = new HashSet<GameObject>();
//    private bool isTraversingWire = false;

//    // Task Conditions
//    public List<Vector2> indexOfDiffs = new List<Vector2> // L (wire), W (ring diameter), wire diameter is fixed to 0.01 m
//    {
//        new Vector2(0.20f, 0.04f),
//        new Vector2(0.20f, 0.08f),
//        new Vector2(0.30f, 0.04f),
//        new Vector2(0.30f, 0.08f)
//    };

//    private List<Quaternion> wireRotations = new List<Quaternion> { 
//        // z-plane
//        Quaternion.Euler(0, 0, 0),
//        Quaternion.Euler(0, 0, 45),
//        Quaternion.Euler(0, 0, 90),
//        Quaternion.Euler(0, 0, 135),
//        Quaternion.Euler(0, 0, 180),
//        Quaternion.Euler(0, 0, 225),
//        Quaternion.Euler(0, 0, 270),
//        Quaternion.Euler(0, 0, 315),
//        // x-plane
//        Quaternion.Euler(45, 0, 0),
//        Quaternion.Euler(90, 0, 0),
//        Quaternion.Euler(135, 0, 0),
//        Quaternion.Euler(225, 0, 0),
//        Quaternion.Euler(270, 0, 0),
//        Quaternion.Euler(315, 0, 0),
//        // y-plane
//        Quaternion.Euler(0, 45, 90),
//        Quaternion.Euler(0, 135, 90),
//        Quaternion.Euler(0, 225, 90),
//        Quaternion.Euler(0, 315, 90),
//        // 3d-diagonal up
//        Quaternion.Euler(0, 45, 45),
//        Quaternion.Euler(0, 135, 45),
//        Quaternion.Euler(0, 225, 45),
//        Quaternion.Euler(0, 315, 45),
//        // 3d-diagonal down
//        Quaternion.Euler(0, 45, 135),
//        Quaternion.Euler(0, 135, 135),
//        Quaternion.Euler(0, 225, 135),
//        Quaternion.Euler(0, 315, 135)
//    };

//    private int[,] placements =
//    {
//        {1,2,3},
//        {2,3,1},
//        {3,1,2}
//    };

//    void Start()
//    {
//        participantTrials = GenerateParticipantTrial(participantID); //TODO: update numbers based on shoulder and eye level

//        //TODO get the hmd position for generating position trials
//        targetPosition = new Vector3(0.158f, 1.1f, 3.2f);



//        //if (rightHanded)
//        //{
//        //    targetPosition = new Vector3(0.158f, 1.1f, 3.2f); // right handed participant
//        //}
//        //else
//        //{
//        //    targetPosition = new Vector3(0.5f, 1.0f, 3.23f); // left handed participant
//        //}
//        Debug.Log($"Trials initialized: {participantTrials?.Count ?? 0} trials created.");
//        NextTrial();
//        //ActivateRandomWire();
//    }


//    void Update()
//    {
//        // for debug: with space we move to next trial
//        if (Input.GetKeyDown(KeyCode.Space))
//        {
//            EndTrial();
//        }
//        if (isTraversingWire)
//        {
//            OnTraversingTracking();
//        }
//    }


//    public void NextTrial()
//    {
//        if (currentTrial >= participantTrials.Count)
//        {
//            Debug.Log("All trials completed for participant.");
//            return;
//        }

//        //Decompose trial condition
//        (Vector2 id, Quaternion rotation) = participantTrials[currentTrial];
//        float len = id.x;
//        float width = id.y;

//        // create wire
//        currentWire = Instantiate(wirePrefab, targetPosition, rotation);
//        currentWire.transform.localScale = new Vector3(0.01f, len, 0.01f);

//        //create ring
//        Vector3 wireForward = currentWire.transform.up;
//        //Vector3 wireRight = currentWire.transform.right;
//        float ringOffset = len+0.05f;
//        Vector3 ringPosition = targetPosition - ringOffset * wireForward;
//        currentRing = Instantiate(SelectRingPrefab(width), ringPosition, rotation);
//        currentRing.transform.forward = currentWire.transform.up; // to overcome problem regarding orientation of the ring-to be prependicular to wire
//        Debug.Log($"Trial {currentTrial + 1} started: L = {len}, W = {width}, Rotation = {rotation.eulerAngles}");

//        //saving tracking information as output for each trial
//        string trackingOutputPath = Path.Combine(Application.dataPath, "CapturedData");
//        string trackingOutputName = $"P{participantID}_T{currentTrial + 1}_wireTrack.csv";
//        trackingOutputFile = Path.Combine(trackingOutputPath, trackingOutputName);
//        if (!Directory.Exists(trackingOutputPath))
//        {
//            Debug.Log("Directory Not Found!! created new one");
//            Directory.CreateDirectory(trackingOutputPath);
//        }
//        if (!File.Exists(trackingOutputFile))
//        {
//            File.WriteAllText(trackingOutputFile, "PID,rightHanded,width,length,rotationX,rotationY,rotationZ,PositionX,PositionY\n");
//        }
//        else
//        {
//            Debug.Log("WARNING: file already exists, overwritting!");
//        }
//        //update trial info for saving tracking info in output file
//        trialL = len;
//        trialR = rotation;
//        trialW = width;
//    }

//    private void SaveWireTrack(float x, float y)
//    {
//        string newData = $"{participantID},{rightHanded},{trialW},{trialL},{trialR.x},{trialR.y},{trialR.z},{x},{y}\n";
//        File.AppendAllText(trackingOutputFile, newData);
//    }

//    public void EndTrial() // to end a trial and move to the next one
//    {

//        isTraversingWire = false;
//        // destroy previous trial objects
//        if (currentRing != null) Destroy(currentRing);
//        if (currentWire != null) Destroy(currentWire);

//        Debug.Log("OBJ deleted");

//        currentTrial++;

//        NextTrial();

//    }

//    GameObject SelectRingPrefab(float W)
//    {
//        GameObject selectedRingPrefab = null;

//        switch (W)
//        {
//            case 0.02f:
//                selectedRingPrefab = ringPrefabs[0];
//                break;
//            case 0.04f:
//                selectedRingPrefab = ringPrefabs[1];
//                break;
//            case 0.08f:
//                selectedRingPrefab = ringPrefabs[2];
//                break;
//            default:
//                Debug.LogError("No ring prefab for W: " + W);
//                break;
//        }

//        return selectedRingPrefab;
//    }


//    List<Quaternion> CounterBalanceRotations(int participantId) // Generate latin square of rotations for counter balancing rotations
//    {
//        List<Quaternion> rotationOrder = new List<Quaternion>();
//        for (int i = 0; i < wireRotations.Count; i++)
//        {
//            int index = (i + participantId) % wireRotations.Count;
//            rotationOrder.Add(wireRotations[index]);
//        }

//        return rotationOrder;
//    }

//    public List<(Vector2, Quaternion)> GenerateParticipantTrial(int participantId) // generate the trial conditions for specific participant (tuple of ID and rotations)
//    {
//        int normalizedPID = (participantId - 1) % 26; // make it 0 to 25
//        List<Quaternion> participantRotations = CounterBalanceRotations(normalizedPID);

//        List<(Vector2, Quaternion)> trials = new List<(Vector2, Quaternion)>();
//        foreach (Vector2 id in indexOfDiffs)
//        {
//            foreach (Quaternion rotation in participantRotations)
//            {
//                trials.Add((id, rotation));
//            }
//        }

//        return trials;
//    }

//    public void OnStartTraversing()
//    {
//        Debug.Log("traversing started");

//        isTraversingWire = true;
//    }

//    private void OnTraversingTracking()
//    {
//        Vector3 ringPlaneNormal = currentRing.transform.forward;
//        Vector3 ringCenter = currentRing.transform.position;

//        Vector3 wireRayStartPos = GetWireStartPoint().position;
//        Ray wireRay = new Ray(wireRayStartPos, currentWire.transform.up);
//        Plane ringPlane = new Plane(ringPlaneNormal, ringCenter);
//        Vector3 intersectionPoint;
//        if (ringPlane.Raycast(wireRay, out float intr))
//        {
//            intersectionPoint = wireRay.GetPoint(intr);
//            Vector3 localIntersection = intersectionPoint - ringCenter;
//            float x = Vector3.Dot(localIntersection, currentRing.transform.right);
//            float y = Vector3.Dot(localIntersection, currentRing.transform.up);

//            debugText.updateText("x: " + x + " / y: "+ y);
//            //saving tracking info to file
//            SaveWireTrack(x, y);
//        }
//        else
//        { 
//            debugText.updateText("no intersection found"); 
//        }

//    }

//    private Transform GetWireStartPoint()
//    {
//        Transform[] children = currentWire.GetComponentsInChildren<Transform>(true);
//        Transform startPoint = null;

//        foreach (Transform child in children)
//        {
//            if (child.CompareTag("StartPoint"))
//            {
//                startPoint = child;
//                break;
//            }
//        }
//        if (startPoint == null)
//        {
//            Debug.Log("No child with the specified tag found.");
//        }
//        return startPoint;
//    }

//    private void OnDrawGizmos()
//    {
//        if (currentWire == null || currentRing == null) return;

//        // Get the ring's plane normal and center
//        Vector3 ringPlaneNormal = currentRing.transform.forward; // Or adjust based on your orientation
//        Vector3 ringCenter = currentRing.transform.position;

//        // Visualize the plane with Gizmos
//        DrawPlaneGizmo(ringCenter, ringPlaneNormal, 5f, 5f); // 5x5 plane size
//    }

//    private void DrawPlaneGizmo(Vector3 center, Vector3 normal, float width, float height)
//    {
//        Gizmos.color = Color.cyan;

//        // Calculate plane corners
//        Vector3 right = Vector3.Cross(normal, Vector3.up).normalized;
//        if (right == Vector3.zero) right = Vector3.Cross(normal, Vector3.forward).normalized;

//        Vector3 forward = Vector3.Cross(normal, right);

//        Vector3 topLeft = center + (-right * width / 2) + (forward * height / 2);
//        Vector3 topRight = center + (right * width / 2) + (forward * height / 2);
//        Vector3 bottomLeft = center + (-right * width / 2) + (-forward * height / 2);
//        Vector3 bottomRight = center + (right * width / 2) + (-forward * height / 2);

//        // Draw the plane as a rectangle
//        Gizmos.DrawLine(topLeft, topRight);
//        Gizmos.DrawLine(topRight, bottomRight);
//        Gizmos.DrawLine(bottomRight, bottomLeft);
//        Gizmos.DrawLine(bottomLeft, topLeft);

//        // Draw the normal direction
//        Gizmos.color = Color.yellow;
//        Gizmos.DrawLine(center, center + normal * 2f); // Normal line (scaled for visibility)
//    }
    

//    public void OnFailTraversing(GameObject wire) // going out of bounds while traversing
//    {
//        // TODO: redo trial
//    }
//}

