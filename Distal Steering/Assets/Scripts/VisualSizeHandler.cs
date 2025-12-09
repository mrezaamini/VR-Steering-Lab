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
    }

    void LateUpdate()
    {
        if (!headTransform) return;

        // --- 1. Place parent in front of head ---
        Vector3 targetPos = headTransform.position + headTransform.forward * desiredDistance;
        Quaternion targetRot = Quaternion.LookRotation(-headTransform.up, -Vector3.forward);

        float scaleFactor = desiredDistance / referenceDistance;

        // Parent visual size at this distance (same logic you had, but any mapping works)
        float parentVisualWidth = baseSize.x * scaleFactor;
        float parentVisualLength = baseSize.y * scaleFactor;

        Vector3 targetScale = new Vector3(
            parentVisualWidth,  // width scale factor
            1f,
            parentVisualLength  // length scale factor
        );

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

        // --- 2. Target child visual width in WORLD units at this distance ---
        float childWorldWidthTarget = childBaseVisualWidth * scaleFactor;

        // Make each child actually have that world width (using its mesh size)
        float childHalfWorldWidth = 0f;
        FixChildVisualWidth(startPoint, childWorldWidthTarget, out float startHalf);
        FixChildVisualWidth(endPoint, childWorldWidthTarget, out float endHalf);

        // We assume start/end use the same mesh, so either half is fine;
        // if not, you could keep them separate.
        childHalfWorldWidth = Mathf.Max(startHalf, endHalf);

        // --- 3. Compute actual parent half-width in WORLD space ---
        float parentHalfWorldWidth =
            parentLocalHalfWidth * Mathf.Abs(transform.lossyScale.x);

        // --- 4. Place children at center ± (parentHalf + childHalf) ---
        PositionChildren(parentHalfWorldWidth, childHalfWorldWidth);
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
