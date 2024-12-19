using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    // participant based variables
    public bool rightHanded;
    // condition management
    public GameObject wire_prefab;
    public GameObject ring_prefab;
    public List<Vector2> conditions = new List<Vector2> // L (wire), W (ring diameter), wire diameter is fixed to 0.01 m
    {
        new Vector2(0.20f, 0.02f),
        new Vector2(0.20f, 0.04f),
        new Vector2(0.20f, 0.08f),
        new Vector2(0.25f, 0.02f),
        new Vector2(0.25f, 0.04f),
        new Vector2(0.25f, 0.08f),
        new Vector2(0.35f, 0.02f),
        new Vector2(0.35f, 0.04f),
        new Vector2(0.35f, 0.08f),
        new Vector2(0.50f, 0.02f),
        new Vector2(0.50f, 0.04f),
        new Vector2(0.50f, 0.08f)
    };


    // for single condition
    [SerializeField] private List<GameObject> wires; 
    private GameObject currentWire;
    private HashSet<GameObject> visitedWires = new HashSet<GameObject>();
    private bool isTraversingWire = false;

    // positions and rotations in each condition
    private Vector3[] wirePositions = { // TODO update based on right hand and left hand conditions near shoulder and position of eyelevel near shoulder
        new Vector3(0.5f, 1.0f, 3.23f), // right hand
        new Vector3(0.5f, 1.0f, 3.23f) // left hand
    };

    private Quaternion[] wireRotations = {
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
        ActivateRandomWire(); 
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

    public void OnFinishTraversing()
    {
        if (isTraversingWire)
        {
            Debug.Log("Ring entered the end collider of the current wire.");
            currentWire.SetActive(false); // Deactivate the current wire
            isTraversingWire = false;
            ActivateRandomWire(); // Activate a new random wire
        }
    }

    public void OnFailTraversing(GameObject wire) // going out of bounds while traversing
    {
        // TODO
    }
}

