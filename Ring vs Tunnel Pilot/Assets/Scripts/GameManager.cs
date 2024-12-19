using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField] private List<GameObject> wires; 
    //[SerializeField] private GameObject ring; 

    private GameObject currentWire;
    private HashSet<GameObject> visitedWires = new HashSet<GameObject>();
    private bool isTraversingWire = false;


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
        Debug.Log("outt");
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

