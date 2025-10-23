using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VisualSizeHandler : MonoBehaviour
{
    [Header("References")]
    public Transform headTransform; // XR camera (headset)

    [Header("Settings")]
    public float desiredDistance = 1.5f;      // meters in front of user
    public Vector2 baseSize = new Vector2(1f, 1f); // size at reference distance
    public float referenceDistance = 1.5f;    // distance where base size looks correct
    public bool smoothFollow = true;
    public float followSpeed = 5f;

    void Start()
    {
        if (!headTransform && Camera.main != null)
            headTransform = Camera.main.transform;
    }

    void LateUpdate()
    {
        if (!headTransform) return;

        // --- 1. Compute target position directly in front of headset ---
        Vector3 targetPos = headTransform.position + headTransform.forward * desiredDistance;

        // --- 2. Align plane perpendicular to the gaze (facing the user) ---
        Quaternion targetRot = Quaternion.LookRotation(-headTransform.up, -Vector3.forward);

        // --- 3. Keep visual size constant in perspective ---
        float scaleFactor = desiredDistance / referenceDistance;
        Vector3 targetScale = new Vector3(baseSize.x * scaleFactor, 1f, baseSize.y * scaleFactor);

        // --- 4. Apply with optional smoothing ---
        if (smoothFollow)
        {
            transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * followSpeed);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * followSpeed);
            transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * followSpeed);
        }
        else
        {
            transform.position = targetPos;
            transform.rotation = targetRot;
            transform.localScale = targetScale;
        }
    }
}
