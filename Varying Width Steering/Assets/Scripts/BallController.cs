using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class BallController : MonoBehaviour
{
    //[Header("Movement")]
    //public float moveForce = 15f;
    //public float maxSpeed = 8f;

    private Rigidbody rb;
    private MeshRenderer ballRenderer;
    private SphereCollider ballCollider;

    public Vector3 Velocity => rb.linearVelocity;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        ballRenderer = GetComponent<MeshRenderer>();
        ballCollider = GetComponent<SphereCollider>();
        rb.useGravity = false;
        //    rb.useGravity = false;
        //    rb.constraints = RigidbodyConstraints.FreezePositionY
        //                   | RigidbodyConstraints.FreezeRotation;
    }

    public void HideBall()
    {
        ballRenderer.enabled = false;
       

    }

    public void ShowBall()
    {
        ballRenderer.enabled = true;
 
    }

    //void FixedUpdate()
    //{
    //    float h = Input.GetAxis("Horizontal");
    //    float v = Input.GetAxis("Vertical");

    //    Vector3 dir = new Vector3(h, 0f, v);
    //    if (dir.sqrMagnitude > 0f)
    //        rb.AddForce(dir.normalized * moveForce);

    //    if (rb.linearVelocity.magnitude > maxSpeed)
    //        rb.linearVelocity = rb.linearVelocity.normalized * maxSpeed;
    //}

    public void TeleportTo(Vector3 worldPos)
    {
        HideBall();
        rb.position = worldPos;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        ShowBall();
    }
}