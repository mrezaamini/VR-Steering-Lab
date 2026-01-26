using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CursorVisualSizeHandler : MonoBehaviour
{
    [Header("References")]
    public Transform headTransform; // XR headset camera

    [Header("Settings")]
    public float referenceDistance = 1.0f;  // Distance where sphere looks correct
    public float referenceScale = 0.02f;    // Sphere radius at referenceDistance

    void Start()
    {
        if (!headTransform && Camera.main != null)
            headTransform = Camera.main.transform;
    }

    void LateUpdate()
    {
        if (!headTransform) return;

        
        float distance = Vector3.Distance(headTransform.position, transform.position);
        float scaleFactor = distance / referenceDistance;
        float targetScale = referenceScale * scaleFactor;
        transform.localScale = Vector3.one * targetScale;
    }
}
