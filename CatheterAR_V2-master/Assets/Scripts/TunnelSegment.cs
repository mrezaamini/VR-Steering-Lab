using UnityEngine;

public struct TunnelSegment
{
    public Vector3 startPoint;
    public Vector3 endPoint;
    public float startRadius;
    public float endRadius;

    /// <summary>
    /// Projects ballPos onto the tunnel axis and checks containment.
    /// </summary>
    /// <returns>
    /// inside       — true if ball is within the tapered radius
    /// t            — normalised progress along axis [0,1]
    /// radialDist   — distance from axis at ball position
    /// allowedRadius— interpolated tunnel radius at t
    /// </returns>
    public (bool inside, float t, float progressL, float radialDist, float allowedRadius)
    Evaluate(Vector3 ballPos)
    {
        Vector3 axis = endPoint - startPoint;
        float axisLen = axis.magnitude;

        if (axisLen < Mathf.Epsilon)
            return (false, 0f, 0f, 0f, 0f);

        Vector3 axisDir = axis / axisLen;

        float l = Vector3.Dot(ballPos - startPoint, axisDir);
        float t = l / axisLen;
        float tClamped = Mathf.Clamp01(t);
        Vector3 closestOnAxis = startPoint + axisDir * (tClamped * axisLen);
        float radialDist = Vector3.Distance(ballPos, closestOnAxis);
        float allowedRadius = Mathf.Lerp(startRadius, endRadius, tClamped);
        

        // t must be in (0,1) — ball outside the caps is considered out
        bool inside = (t >= 0f) && (radialDist <= allowedRadius);

        return (inside, tClamped, l, radialDist, allowedRadius);
    }


    public Vector3 WorldToTunnelLocal(Vector3 worldPos, Transform insertionPoint)
    {
        Vector3 delta = worldPos - startPoint;
        float z = Vector3.Dot(delta, insertionPoint.forward);
        float x = Vector3.Dot(delta, insertionPoint.up);
        float y = Vector3.Dot(delta, insertionPoint.right);
        return new Vector3(x, y, z); //x: up down, y: left right, z progress
    }


}