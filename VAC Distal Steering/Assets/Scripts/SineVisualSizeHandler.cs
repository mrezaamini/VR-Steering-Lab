using UnityEngine;

public class SineVisualSizeHandler : MonoBehaviour
{
    [Header("References")]
    public Transform headTransform;

    [Tooltip("Optional: collider surface used for raycast")]
    public Transform boardCollider;

    [Header("Control")]
    public bool lockPositionAndRotation = false;

    [Tooltip("0 = CW, 1 = CCW (logic is in RayBrush; here it's only for convenience if you want to swap colors externally).")]
    public int circleDirection = 0;

    [Header("Settings")]
    [Tooltip("Distance of the circle plane in front of the user (meters).")]
    public float desiredDistance = 1.5f;

    public Vector2 baseSize = new Vector2(1f, 1f);
    

    [Tooltip("Reference distance where baseSize values are defined (meters). Typically 1m.")]
    public float referenceDistance = 1f;

    public bool smoothFollow = false;
    public float followSpeed = 5f;

    [Header("Gate Placement")]
    [Tooltip("Lift gate slightly off the surface to avoid z-fighting (meters).")]
    public float gateHeightOffset = 0.001f;

    public float path_length = 1f;

    public GameManager gameManager;

    void Start()
    {
        if (!headTransform && Camera.main != null)
            headTransform = Camera.main.transform;


        if (!boardCollider)
        {
            var t = transform.Find("BoardColliderMeshSine");
            if (t) boardCollider = t;
        }


        smoothFollow = false;
        referenceDistance = 1f;
    }

    
    void Update()
    {
        if (!headTransform) return;

        Vector3 targetPos = headTransform.position + headTransform.forward * desiredDistance;

        Quaternion targetRot = Quaternion.LookRotation(-Vector3.up, Vector3.forward);

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

        if (boardCollider && !lockPositionAndRotation)
        {
            // Match the parent pose; scale can remain as authored (big enough).
            boardCollider.position = transform.position;
            boardCollider.rotation = transform.rotation;
        }


        float scaleFactor = desiredDistance / 1; //assumming 1m as the base depth


        //////////TODO: gate placement and scaling
        //float condition_radius = 1f;

        //switch (path_length)
        //{
        //    case 25:
        //        condition_radius = 0.07056739191f;
        //        break;
        //    case 35:
        //        condition_radius = 0.1003624705f;
        //        break;
        //    case 50:
        //        condition_radius = 0.1484302873f;
        //        break;
        //    default:
        //        Debug.LogError("Unknown Path Length!");
        //        break;

        //}


        //if (gateRoot && !lockPositionAndRotation)
        //{
        //    // 12 o'clock is +forward in the circle's local plane (we define it that way) //TODO: change second line of noon pos
        //    Vector3 noonPos = transform.position
        //                      + (Vector3.up * scaleFactor * condition_radius)
        //                      - (transform.up.normalized * gateHeightOffset);

        //    if (smoothFollow)
        //        gateRoot.position = Vector3.Lerp(gateRoot.position, noonPos, Time.deltaTime * followSpeed);
        //    else
        //        gateRoot.position = noonPos;

        //    // Orient gate so it is tangent at 12 o'clock (tangent points along local right)
        //    // We keep gateRoot rotation aligned with the circle plane.
        //    if (smoothFollow)
        //        gateRoot.rotation = Quaternion.Slerp(gateRoot.rotation, transform.rotation, Time.deltaTime * followSpeed);
        //    else
        //        gateRoot.rotation = transform.rotation;
        //}
    }
}
