//using UnityEngine;
//using UnityEngine.ProBuilder;
//using UnityEngine.ProBuilder.Shapes;

///// <summary>
///// Keeps a circular path (RingVisual + BoardColliderMesh + GateRoot) at a desired depth
///// while keeping the *visual angle* constant across depths.
/////
///// IMPORTANT ASSUMPTIONS (match your current pipeline):
///// - baseSize.x is the CIRCUMFERENCE (meters) at referenceDistance (usually 1m).
///// - The ring mesh local "radius" is unknown (ProBuilder torus is NOT unit-radius).
/////   This script auto-calibrates the mesh's base radius from its bounds (in local XZ),
/////   so the ring radius in-world becomes exactly R = L / (2pi).
///// - 
///// </summary>
//public class CircularSizeHandler : MonoBehaviour
//{
//    [Header("References")]
//    public Transform headTransform;

//    [Tooltip("Assign the visual ring/torus transform (e.g., child named RingVisual).")]
//    public Transform ringVisual;

//    [Tooltip("Optional: collider surface used for raycast (e.g., child named BoardColliderMesh).")]
//    public Transform boardCollider;

//    [Tooltip("Optional: GateRoot at 12 o'clock (child object). We only reposition it; coloring handled elsewhere if you want.")]
//    public Transform gateRoot;

//    [Header("Control")]
//    public bool lockPositionAndRotation = false;

//    [Tooltip("0 = CW, 1 = CCW (logic is in RayBrush; here it's only for convenience if you want to swap colors externally).")]
//    public int circleDirection = 0;

//    [Header("Settings")]
//    [Tooltip("Distance of the circle plane in front of the user (meters).")]
//    public float desiredDistance = 1.5f;

//    public Vector2 baseSize = new Vector2(1f, 1f);
//    // baseSize.x = CIRCUMFERENCE (meters) at referenceDistance
//    // baseSize.y = WIDTH W (meters) at referenceDistance (optional; only used if you scale thickness/visuals)

//    [Tooltip("Reference distance where baseSize values are defined (meters). Typically 1m.")]
//    public float referenceDistance = 1f;

//    public bool smoothFollow = false;
//    public float followSpeed = 5f;

//    [Header("Gate Placement")]
//    [Tooltip("Lift gate slightly off the surface to avoid z-fighting (meters).")]
//    public float gateHeightOffset = 0.001f;

//    // Cached ring mesh base radius in LOCAL units (after reset scale)
//    private float ringBaseRadiusLocal = 1f;
//    private bool ringCalibrated = false;

//    void Start()
//    {
//        if (!headTransform && Camera.main != null)
//            headTransform = Camera.main.transform;

//        // Auto-find common children if not assigned
//        if (!ringVisual)
//        {
//            var t = transform.Find("RingVisual");
//            if (t) ringVisual = t;
//        }

//        if (!boardCollider)
//        {
//            var t = transform.Find("BoardColliderMesh");
//            if (t) boardCollider = t;
//        }

//        if (!gateRoot)
//        {
//            var t = transform.Find("GateRoot");
//            if (t) gateRoot = t;
//        }

//        CalibrateRingRadius();

//        // Keep your current defaults
//        smoothFollow = false;
//        referenceDistance = 1f;
//    }

//    void Update()
//    {
//        if (!headTransform) return;

//        // Place in front of head
//        Vector3 targetPos = headTransform.position + headTransform.forward * desiredDistance;

    
//        Quaternion targetRot = Quaternion.LookRotation(-Vector3.up, Vector3.forward);

//        // Visual-angle constant scaling: sizes grow linearly with distance
//        float scaleFactor = desiredDistance / Mathf.Max(1e-5f, referenceDistance);

//        // Physical circumference for THIS depth that preserves visual angle
//        float C = baseSize.x * scaleFactor; // meters
//        float R = C / (2f * Mathf.PI);      // meters

//        if (!lockPositionAndRotation)
//        {
//            if (smoothFollow)
//            {
//                transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * followSpeed);
//                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * followSpeed);
//            }
//            else
//            {
//                transform.position = targetPos;
//                transform.rotation = targetRot;
//            }
//        }

//        // Scale RingVisual so its WORLD radius equals R
//        if (ringVisual)
//        {
//            if (!ringCalibrated) CalibrateRingRadius();

//            float s = R / Mathf.Max(1e-5f, ringBaseRadiusLocal);

//            // Preserve current Y scale (useful if your torus thickness is modeled in Y or you want to keep it)
//            Vector3 ls = ringVisual.localScale;
//            float y = ls.y; // keep whatever you set in editor
//            Vector3 ringScale = new Vector3(s, y, s);


//            if (!lockPositionAndRotation)
//            {
//                if (smoothFollow)
//                    ringVisual.localScale = Vector3.Lerp(ringVisual.localScale, ringScale, Time.deltaTime * followSpeed);
//                else
//                    ringVisual.localScale = ringScale;
//            }
//        }

//        // Keep the raycast surface under the ring (optional). Typically keep it constant big enough,
//        // but if you want it to follow the circle object, we at least keep its pose aligned.
//        if (boardCollider && !lockPositionAndRotation)
//        {
//            // Match the parent pose; scale can remain as authored (big enough).
//            boardCollider.position = transform.position;
//            boardCollider.rotation = transform.rotation;
//        }

       
//        if (gateRoot && !lockPositionAndRotation)
//        {
//            // 12 o'clock is +forward in the circle's local plane (we define it that way)
//            Vector3 noonPos = transform.position
//                              + (Vector3.up * R)
//                              + (transform.up.normalized * gateHeightOffset);

//            if (smoothFollow)
//                gateRoot.position = Vector3.Lerp(gateRoot.position, noonPos, Time.deltaTime * followSpeed);
//            else
//                gateRoot.position = noonPos;

//            // Orient gate so it is tangent at 12 o'clock (tangent points along local right)
//            // We keep gateRoot rotation aligned with the circle plane.
//            if (smoothFollow)
//                gateRoot.rotation = Quaternion.Slerp(gateRoot.rotation, transform.rotation, Time.deltaTime * followSpeed);
//            else
//                gateRoot.rotation = transform.rotation;
//        }
//    }

//    /// <summary>
//    /// Calibrates the ring mesh's base radius in local units (XZ plane) so we can scale to meters reliably.
//    /// This avoids any ProBuilder/asset-specific unit radius assumptions.
//    /// </summary>
//    private void CalibrateRingRadius()
//    {
//        ringCalibrated = false;
//        ringBaseRadiusLocal = 1f;

//        if (!ringVisual) return;

//        var mf = ringVisual.GetComponent<MeshFilter>();
//        if (mf == null || mf.sharedMesh == null) return;

//        Bounds b = mf.sharedMesh.bounds;
//        float rLocal = Mathf.Max(b.extents.x, b.extents.z);

//        if (rLocal > 1e-5f)
//        {
//            ringBaseRadiusLocal = rLocal;
//            ringCalibrated = true;
//        }
//    }
//}


using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.ProBuilder.Shapes;

/// <summary>
/// Keeps a circular path (RingVisual + BoardColliderMesh + GateRoot) at a desired depth
/// while keeping the *visual angle* constant across depths.
///
/// IMPORTANT ASSUMPTIONS (match your current pipeline):
/// - baseSize.x is the CIRCUMFERENCE (meters) at referenceDistance (usually 1m).
/// - The ring mesh local "radius" is unknown (ProBuilder torus is NOT unit-radius).
///   This script auto-calibrates the mesh's base radius from its bounds (in local XZ),
///   so the ring radius in-world becomes exactly R = L / (2pi).
/// - 
/// </summary>
public class CircularSizeHandler : MonoBehaviour
{
    [Header("References")]
    public Transform headTransform;

    [Tooltip("Optional: collider surface used for raycast (e.g., child named BoardColliderMesh).")]
    public Transform boardCollider;

    [Tooltip("Optional: GateRoot at 12 o'clock (child object). We only reposition it; coloring handled elsewhere if you want.")]
    public Transform gateRoot;

    [Header("Control")]
    public bool lockPositionAndRotation = false;

    [Tooltip("0 = CW, 1 = CCW (logic is in RayBrush; here it's only for convenience if you want to swap colors externally).")]
    public int circleDirection = 0;

    [Header("Settings")]
    [Tooltip("Distance of the circle plane in front of the user (meters).")]
    public float desiredDistance = 1.5f;

    public Vector2 baseSize = new Vector2(1f, 1f);
    // baseSize.x = CIRCUMFERENCE (meters) at referenceDistance
    // baseSize.y = WIDTH W (meters) at referenceDistance (optional; only used if you scale thickness/visuals)

    [Tooltip("Reference distance where baseSize values are defined (meters). Typically 1m.")]
    public float referenceDistance = 1f;

    public bool smoothFollow = false;
    public float followSpeed = 5f;

    [Header("Gate Placement")]
    [Tooltip("Lift gate slightly off the surface to avoid z-fighting (meters).")]
    public float gateHeightOffset = 0.001f;

    // Cached ring mesh base radius in LOCAL units (after reset scale)
    private float ringBaseRadiusLocal = 1f;
    private bool ringCalibrated = false;
    public float path_length = 1f;

    public GameManager gameManager;

    void Start()
    {
        if (!headTransform && Camera.main != null)
            headTransform = Camera.main.transform;
        



        if (!boardCollider)
        {
            var t = transform.Find("BoardColliderMesh");
            if (t) boardCollider = t;
        }

        if (!gateRoot)
        {
            var t = transform.Find("GateRoot");
            if (t) gateRoot = t;
        }

        //CalibrateRingRadius();

        // Keep your current defaults
        smoothFollow = false;
        referenceDistance = 1f;
    }

    void Update()
    {
        if (!headTransform) return;

        // Place in front of head
        Vector3 targetPos = headTransform.position + headTransform.forward * desiredDistance;


        Quaternion targetRot = Quaternion.LookRotation(-Vector3.up, Vector3.forward);

        // Visual-angle constant scaling: sizes grow linearly with distance

        
        lockPositionAndRotation = gameManager.lockPosRot;
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
        }

        

        // Keep the raycast surface under the ring (optional). Typically keep it constant big enough,
        // but if you want it to follow the circle object, we at least keep its pose aligned.
        if (boardCollider && !lockPositionAndRotation)
        {
            // Match the parent pose; scale can remain as authored (big enough).
            boardCollider.position = transform.position;
            boardCollider.rotation = transform.rotation;
        }


        float scaleFactor = desiredDistance / 1; //assumming 1m as the base depth
        float condition_radius = 1f;

        switch (path_length)
        {
            case 25:
                condition_radius = 0.07056739191f;
                break;
            case 35:
                condition_radius = 0.1003624705f;
                break;
            case 50:
                condition_radius = 0.1484302873f;
                break;
            default:
                Debug.LogError("Unknown Path Length!");
                break;

        }


        if (gateRoot && !lockPositionAndRotation)
        {
            // 12 o'clock is +forward in the circle's local plane (we define it that way) //TODO: change second line of noon pos
            Vector3 noonPos = transform.position
                              + (Vector3.up * scaleFactor*condition_radius) 
                              - (transform.up.normalized * gateHeightOffset);

            if (smoothFollow)
                gateRoot.position = Vector3.Lerp(gateRoot.position, noonPos, Time.deltaTime * followSpeed);
            else
                gateRoot.position = noonPos;

            // Orient gate so it is tangent at 12 o'clock (tangent points along local right)
            // We keep gateRoot rotation aligned with the circle plane.
            if (smoothFollow)
                gateRoot.rotation = Quaternion.Slerp(gateRoot.rotation, transform.rotation, Time.deltaTime * followSpeed);
            else
                gateRoot.rotation = transform.rotation;
        }
    }

   
}
