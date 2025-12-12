
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VisualSizeHandler : MonoBehaviour
{
    [Header("References")]
    public Transform headTransform; // XR camera (headset)

    [Header("Control")]
    public bool lockPositionAndRotation = false; // if true, stop following head
    public int pathDirection = 1;                // 0 = LR, 1 = RL

    [Header("Settings")]
    public float desiredDistance = 1.5f;      // meters in front of user
    public Vector2 baseSize = new Vector2(1f, 1f); // size at reference distance
    public float referenceDistance = 1f;    // distance where base size looks correct
    public bool smoothFollow = false;
    public float followSpeed = 5f;

    [Header("End Points")]
    public Transform startPoint;
    public Transform endPoint;
    public float childBaseVisualWidth = 0.02f; // desired visual width of the endpoints
    public Renderer parentRenderer;      // renderer on the bar / parent visual
    public Renderer childRenderer;       // renderer on the Start/End marker (they're same size)

    public MeshFilter parentMeshFilter;  // mesh of the bar/parent visual
    private float parentLocalHalfWidth = 0.5f;

    void Start()
    {
        if (!headTransform && Camera.main != null)
            headTransform = Camera.main.transform;

        if (!parentMeshFilter)
            parentMeshFilter = GetComponent<MeshFilter>();

        if (parentMeshFilter && parentMeshFilter.sharedMesh)
        {
            Bounds mb = parentMeshFilter.sharedMesh.bounds;
            // ASSUME bar width is along local X. Change to mb.size.y/mb.size.z if needed.
            parentLocalHalfWidth = mb.size.x * 0.5f;
        }
        smoothFollow = false;
        referenceDistance = 1f;
    }

    void LateUpdate()
    {
        if (!headTransform) return;

        // --- 1. Place parent in front of head ---
        Vector3 targetPos = headTransform.position + headTransform.forward * desiredDistance;
        Quaternion targetRot = Quaternion.LookRotation(-Vector3.up, -Vector3.forward);
        Vector3 flatForward = Vector3.ProjectOnPlane(headTransform.forward, Vector3.up).normalized;
        //Vector3 normal = (headTransform.position - targetPos).normalized; // board faces head
        //Quaternion targetRot = Quaternion.LookRotation(normal, Vector3.up);


        // Apply direction (simple 180° flip around local X if pathDirection == 1)
        if (pathDirection == 0) //
        {
            targetRot = Quaternion.LookRotation(-Vector3.up, Vector3.forward);
            targetRot *= Quaternion.Euler(180f, 0f, 0f);
            //targetRot *= Quaternion.Euler(0f, 180f, 0f);
        }

        float scaleFactor = desiredDistance / referenceDistance;
        scaleFactor = scaleFactor / 10; // plane is already 10mx10m
        float parentVisualWidth = baseSize.x * scaleFactor;
        float parentVisualLength = baseSize.y * scaleFactor;

        Vector3 targetScale = new Vector3(
            parentVisualWidth,
            1f,
            parentVisualLength
        );

        // If locked, don't change position or rotation anymore
        if (!lockPositionAndRotation)
        {
            if (smoothFollow)
            {
                transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * followSpeed);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * followSpeed);
            }
            else
            {
                transform.position = targetPos;
                transform.rotation = targetRot;
            }
            if (smoothFollow)
                transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * followSpeed);
            else
                transform.localScale = targetScale;
        }

       
        

        // --- rest of your code (children width, endpoints positioning) stays the same ---
        float childWorldWidthTarget = childBaseVisualWidth * scaleFactor;

        FixChildVisualWidth(startPoint, childWorldWidthTarget, out float startHalf);
        FixChildVisualWidth(endPoint, childWorldWidthTarget, out float endHalf);

        float childHalfWorldWidth = Mathf.Max(startHalf, endHalf);
        float parentHalfWorldWidth = parentLocalHalfWidth * Mathf.Abs(transform.lossyScale.x);

        PositionChildren(parentHalfWorldWidth, childHalfWorldWidth);
        //lockPositionAndRotation = true;
    }


    /// <summary>
    /// Scales the child so its *actual* world width matches childWorldWidthTarget.
    /// Returns the resulting half-width in world units.
    /// </summary>
    void FixChildVisualWidth(Transform child, float childWorldWidthTarget, out float childHalfWorld)
    {
        childHalfWorld = 0f;
        if (!child) return;

        // Get the child's mesh and its local width
        MeshFilter mf = child.GetComponentInChildren<MeshFilter>();
        if (!mf || !mf.sharedMesh) return;

        Bounds cb = mf.sharedMesh.bounds;
        float childLocalWidth = cb.size.x;          // ASSUME width is local X
        float childLocalHalfWidth = childLocalWidth * 0.5f;

        // Parent world scale (we assume no extra scaled ancestors in between)
        float parentWorldScaleX = transform.lossyScale.x;

        // We want:  childWorldWidthTarget = childLocalWidth * parentWorldScaleX * childLocalScaleX
        // => childLocalScaleX = childWorldWidthTarget / (childLocalWidth * parentWorldScaleX)
        if (Mathf.Abs(parentWorldScaleX) < 1e-5f || childLocalWidth < 1e-5f)
            return;

        float targetLocalScaleX = childWorldWidthTarget / (childLocalWidth * parentWorldScaleX);

        Vector3 ls = child.localScale;
        ls.x = targetLocalScaleX;
        child.localScale = ls;

        // Now compute the actual half-width in world units, to use for offset
        float childWorldWidth = childLocalWidth * parentWorldScaleX * targetLocalScaleX;
        childHalfWorld = childWorldWidth * 0.5f;
    }

    void PositionChildren(float parentHalfWorldWidth, float childHalfWorldWidth)
    {
        if (!startPoint || !endPoint) return;

        float offset = parentHalfWorldWidth + childHalfWorldWidth;

        // Width axis in world space (change if bar is along Y/Z)
        Vector3 widthDir = transform.right.normalized;
        Vector3 center = transform.position;

        startPoint.position = center - widthDir * offset;
        endPoint.position = center + widthDir * offset;
    }

}
