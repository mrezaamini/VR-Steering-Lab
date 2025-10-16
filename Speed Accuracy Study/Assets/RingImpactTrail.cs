using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class RingImpactTrail : MonoBehaviour
{
    public Transform wire;  // Assign the wire (capsule) GameObject here
    public GameObject contactPointPrefab;  // Prefab for the small sphere to show the center of contact points
    public Collider ringCollider;  // The collider on the ring (thin cube covering the ring)
    private string csvFilePath;
    public Transform ring;

    private List<Vector3> contactPoints = new List<Vector3>();  // List to store contact points
    private ContactPoint[] contactBuffer = new ContactPoint[10];  // Preallocated buffer for contact points
    private List<Vector2> impactPoints = new List<Vector2>();  // List to store contact points

    /*
     * void OnCollisionStay(Collision collision)
    {
        if (collision.transform == wire)
        {

            // Clear previous contact points to avoid accumulation from past collisions
            contactPoints.Clear();

            // Get contact points and store them in the preallocated buffer
            int contactCount = collision.GetContacts(contactBuffer);

            // Loop through the collected contacts and store the contact points
            for (int i = 0; i < contactCount; i++)
            {
                contactPoints.Add(contactBuffer[i].point);  // Add the contact point to the list
            }

            // Calculate and display the center of all contact points
            if (contactPoints.Count > 0)
            {
                Vector3 centerOfContactPoints = CalculateCenterOfContactPoints(contactPoints);
                ShowContactPoint(centerOfContactPoints);
            }
        }
    }
    */

    private void OnTriggerStay(Collider other)
    {
        if (other.transform.parent == transform.parent) return;
        if (other.transform == wire)
        {
            Debug.Log("inside");
            Vector3 closest = other.ClosestPoint(transform.position);
            ShowContactPoint(closest);
            //impactPoints.Add(ProjectPointOntoRingPlane(closest));
            impactPoints.Add(ProjectionOnRing(closest));

        }
    }

    public Vector2 ProjectionOnRing(Vector3 contactPoint)
    {
        //Vector3 projectedPoint = Vector3.ProjectOnPlane(contactPoint - ring.position, ring.up);
        //return new Vector2(projectedPoint.z, projectedPoint.x);

        Vector3 projectedPoint = Vector3.ProjectOnPlane(contactPoint - ring.position, ring.up) + ring.position;
        // Calculate the local 2D coordinates
        float x = Vector3.Dot(projectedPoint - ring.position, ring.right);
        float y = Vector3.Dot(projectedPoint - ring.position, ring.forward);

        return new Vector2(x, y);

    }

    public Vector2 ProjectPointOntoRingPlane(Vector3 point)
    {
        // Convert the world point into the local space of the ring
        Vector3 localPoint = transform.InverseTransformPoint(point);

        // In the local space, we can treat it as a 2D point in the ring's plane (XY-plane in this case)
        return new Vector2(localPoint.x, localPoint.y);
    }

    void ShowContactPoint(Vector3 centerPosition)
    {
        // Instantiate the sphere at the center of all contact points
        GameObject sphere = Instantiate(contactPointPrefab, centerPosition, Quaternion.identity);

        // Parent the sphere to the ring so it moves with the rin

        // Optional: Destroy the sphere after a certain time (e.g., 5 seconds)
        Destroy(sphere, 2f);
    }

    Vector3 CalculateCenterOfContactPoints(List<Vector3> points)
    {
        Vector3 sum = Vector3.zero;

        // Sum up all contact points
        foreach (Vector3 point in points)
        {
            sum += point;
        }

        // Calculate the average (center) point
        return sum / points.Count;
    }
    // Start is called before the first frame update
    void Start()
    {
        // Set up the path for the CSV file
        csvFilePath = Application.dataPath + "/impact_points.csv";  // Adjust path as necessary
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnApplicationQuit()
    {
        SaveContactPointsToCSV();
    }

    void SaveContactPointsToCSV()
    {
        using (StreamWriter writer = new StreamWriter(csvFilePath))
        {
            writer.WriteLine("x,y");  // CSV headers

            // Write each contact point to the CSV file
            foreach (Vector2 point in impactPoints)
            {
                writer.WriteLine(point.x + "," + point.y);
            }
        }

        Debug.Log("Contact points saved to: " + csvFilePath);
    }
}
