using UnityEngine;

[System.Serializable]
public class FrameData
{
    public float timestamp;
    public float deltaTime;
    public Vector3 ballPosition;
    public Vector3 ballVelocity;
    public float tunnelT;
    public float radialDistance;
    public float allowedRadius;
    public float normalizedOffset;
    public bool isInsideTunnel;
}