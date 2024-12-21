using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    // participant based variables
    public int participantID;
    public bool rightHanded;
    private List<(Vector2, Quaternion)> participantTrials;
    public GameObject wirePrefab;
    [SerializeField] private List<GameObject> ringPrefabs; // contains 3 different rings of experiment
    private Vector3 targetPosition;



    // for single condition

    public int currentTrial = 0;

    [SerializeField] private List<GameObject> wires;
   
    private GameObject currentWire;
    private GameObject currentRing;
    private HashSet<GameObject> visitedWires = new HashSet<GameObject>();
    private bool isTraversingWire = false;

    // Task Conditions
    public List<Vector2> indexOfDiffs = new List<Vector2> // L (wire), W (ring diameter), wire diameter is fixed to 0.01 m
    {
        new Vector2(0.20f, 0.02f),
        new Vector2(0.20f, 0.04f),
        new Vector2(0.20f, 0.08f),
        new Vector2(0.25f, 0.02f),
        new Vector2(0.25f, 0.04f),
        new Vector2(0.25f, 0.08f),
        new Vector2(0.35f, 0.02f),
        new Vector2(0.35f, 0.04f),
        new Vector2(0.35f, 0.08f)
    };

    private List<Quaternion> wireRotations = new List<Quaternion> { 
        // z-plane
        Quaternion.Euler(0, 0, 0),
        Quaternion.Euler(0, 0, 45),
        Quaternion.Euler(0, 0, 90),
        Quaternion.Euler(0, 0, 135),
        Quaternion.Euler(0, 0, 180),
        Quaternion.Euler(0, 0, 225),
        Quaternion.Euler(0, 0, 270),
        Quaternion.Euler(0, 0, 315),
        // x-plane
        Quaternion.Euler(45, 0, 0),
        Quaternion.Euler(90, 0, 0),
        Quaternion.Euler(135, 0, 0),
        Quaternion.Euler(225, 0, 0),
        Quaternion.Euler(270, 0, 0),
        Quaternion.Euler(315, 0, 0),
        // y-plane
        Quaternion.Euler(0, 45, 90),
        Quaternion.Euler(0, 135, 90),
        Quaternion.Euler(0, 225, 90),
        Quaternion.Euler(0, 315, 90),
        // 3d-diagonal up
        Quaternion.Euler(0, 45, 45),
        Quaternion.Euler(0, 135, 45),
        Quaternion.Euler(0, 225, 45),
        Quaternion.Euler(0, 315, 45),
        // 3d-diagonal down
        Quaternion.Euler(0, 45, 135),
        Quaternion.Euler(0, 135, 135),
        Quaternion.Euler(0, 225, 135),
        Quaternion.Euler(0, 315, 135)
    };

    void Start()
    {
        participantTrials = GenerateParticipantTrial(participantID); //TODO: update numbers based on shoulder and eye level
        if (rightHanded)
        {
            targetPosition = new Vector3(0.158f, 1.1f, 3.2f); // right handed participant
        }
        else
        {
            targetPosition = new Vector3(0.5f, 1.0f, 3.23f); // left handed participant
        }
        Debug.Log($"Trials initialized: {participantTrials?.Count ?? 0} trials created.");
        NextTrial();
        //ActivateRandomWire();
    }


    void Update()
    {
        // for debug: with space we move to next trial
        if (Input.GetKeyDown(KeyCode.Space))
        {
            EndTrial();
        }
    }


    public void NextTrial()
    {
        if (currentTrial >= participantTrials.Count)
        {
            Debug.Log("All trials completed for participant.");
            return;
        }

        //Decompose trial condition
        (Vector2 id, Quaternion rotation) = participantTrials[currentTrial];
        float len = id.x;
        float width = id.y;

        // create wire
        currentWire = Instantiate(wirePrefab, targetPosition, rotation);
        currentWire.transform.localScale = new Vector3(0.01f, len, 0.01f);

        //create ring
        Vector3 wireForward = currentWire.transform.up;
        //Vector3 wireRight = currentWire.transform.right;
        float ringOffset = len+0.05f;
        Vector3 ringPosition = targetPosition - ringOffset * wireForward;
        currentRing = Instantiate(SelectRingPrefab(width), ringPosition, rotation);
        currentRing.transform.forward = currentWire.transform.up; // to overcome problem regarding orientation of the ring-to be prependicular to wire
        Debug.Log($"Trial {currentTrial + 1} started: L = {len}, W = {width}, Rotation = {rotation.eulerAngles}");
    }

    public void EndTrial() // to end a trial and move to the next one
    {

        isTraversingWire = false;
        // destroy previous trial objects
        if (currentRing != null) Destroy(currentRing);
        if (currentWire != null) Destroy(currentWire);

        Debug.Log("OBJ deleted");

        currentTrial++;

        NextTrial();

    }

    GameObject SelectRingPrefab(float W)
    {
        GameObject selectedRingPrefab = null;

        switch (W)
        {
            case 0.02f:
                selectedRingPrefab = ringPrefabs[0];
                break;
            case 0.04f:
                selectedRingPrefab = ringPrefabs[1];
                break;
            case 0.08f:
                selectedRingPrefab = ringPrefabs[2];
                break;
            default:
                Debug.LogError("No ring prefab for W: " + W);
                break;
        }

        return selectedRingPrefab;
    }



    List<Quaternion> CounterBalanceRotations(int participantId) // Generate latin square of rotations for counter balancing rotations
    {
        List<Quaternion> rotationOrder = new List<Quaternion>();
        for (int i = 0; i < wireRotations.Count; i++)
        {
            int index = (i + participantId) % wireRotations.Count;
            rotationOrder.Add(wireRotations[index]);
        }

        return rotationOrder;
    }

    public List<(Vector2, Quaternion)> GenerateParticipantTrial(int participantId) // generate the trial conditions for specific participant (tuple of ID and rotations)
    {
        int normalizedPID = (participantId - 1) % 26; // make it 0 to 25
        List<Quaternion> participantRotations = CounterBalanceRotations(normalizedPID);

        List<(Vector2, Quaternion)> trials = new List<(Vector2, Quaternion)>();
        foreach (Vector2 id in indexOfDiffs)
        {
            foreach (Quaternion rotation in participantRotations)
            {
                trials.Add((id, rotation));
            }
        }

        return trials;
    }


    private void ActivateRandomWire()
    {
        
        List<GameObject> unvisitedWires = wires.FindAll(wire => !visitedWires.Contains(wire));

        if (unvisitedWires.Count > 0)
        {
            currentWire = unvisitedWires[Random.Range(0, unvisitedWires.Count)]; // Choose a random unvisited wire
            currentWire.SetActive(true); // Activate the selected wire
            visitedWires.Add(currentWire); // Mark this wire as visited
        }
        else
        {
            Debug.Log("All wires have been visited. Restarting wire visit tracking.");
            visitedWires.Clear(); // Clear the visited wires to allow re-selection
            ActivateRandomWire(); // Re-activate a random wire
        }
    }

    public void OnStartTraversing()
    {
        Debug.Log("traversing started");

        isTraversingWire = true;
    }

    //public void OnTraversing(GameObject wire)
    //{
    //    if (wire == currentWire && isTraversingWire)
    //    {
    //        Debug.Log("Ring is traversing the wire.");
    //    }
    //}

    //public void OnFinishTraversing()
    //{
    //    if (isTraversingWire)
    //    {
    //        Debug.Log("Ring entered the end collider of the current wire.");
    //        currentWire.SetActive(false); // Deactivate the current wire
    //        isTraversingWire = false;
    //        ActivateRandomWire(); // Activate a new random wire
    //    }
    //}

    public void OnFailTraversing(GameObject wire) // going out of bounds while traversing
    {
        // TODO
    }
}

