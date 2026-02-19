using UnityEngine;

public class GateAtNoon : MonoBehaviour
{
    [Header("References")]
    public Transform gateLeft;
    public Transform gateRight;

    [Header("Gate size at reference depth (meters at BASE_DEPTH=1m)")]
    public float stripeWidth_ref = 0.012f;
    public float stripeLength_ref = 0.06f;
    public float stripeThickness_ref = 0.002f;
    public float halfGap_ref = 0.003f;
    public float heightOffset_ref = 0.002f;

    [Header("Optional: derive stripe length from path width")]
    public bool stripeLengthFromWidth = true;
    public float stripeLengthWidthMultiplier = 2.0f;

    /// <summary>
    /// radiusMeters: ring centerline radius (meters)
    /// widthMeters : path width W at current depth (meters)
    /// depthScale  : trialD / BASE_DEPTH
    /// </summary>
    public void UpdateGate(float widthAng, float depthScale, int trialRot)
    {
        if (!gateLeft || !gateRight) return;

        

        float widthMeters = 0f;
        switch (widthAng)
        {
            case 2: widthMeters = 0.0349f; break;
            case 3: widthMeters = 0.0524f; break;
            case 4.5f: widthMeters = 0.0786f; break;
            case 6: widthMeters = 0.1048f; break;
            default:
                Debug.LogError("Unknown Path Width!");
                break;
        }

        stripeLength_ref = widthMeters;

        // Scale visuals with depth for constant visual angle
        float stripeWidth = stripeWidth_ref * depthScale;
        float stripeThickness = stripeThickness_ref * depthScale;
        float halfGap = halfGap_ref * depthScale;

        float stripeLength = stripeLength_ref * depthScale;
        

        // Stripe scales in GateRoot local axes: X=tangent thickness, Y=normal thickness, Z=radial length
        Vector3 s = new Vector3(stripeWidth, stripeThickness, stripeLength);
        gateLeft.localScale = s;
        gateRight.localScale = s;

        float xOffset = (stripeWidth * 0.5f) + halfGap;
        if (trialRot == 0) //clockwise
        {
            gateLeft.localPosition = new Vector3(-xOffset, 0f, 0f);
            gateRight.localPosition = new Vector3(+xOffset, 0f, 0f);
        }
        else
        { //ccw
            gateLeft.localPosition = new Vector3(+xOffset, 0f, 0f);
            gateRight.localPosition = new Vector3(-xOffset, 0f, 0f);
        }
        

        gateLeft.localRotation = Quaternion.identity;
        gateRight.localRotation = Quaternion.identity;
    }
}
