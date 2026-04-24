using UnityEngine;

[System.Serializable]
public class FrameData
{
    public float timestamp;
    public float deltaTime;
    public Vector3 ballPosition;
    public float distanceTravelled; // 3D distance moved this frame (metres)
    public float speed;             // distanceTravelled / deltaTime (metres/sec)
    public float tunnelT; //normalized progress
    public float tunnelL; //actual length
    public float radialDistance;
    public float allowedRadius;
    public float ballLateralOffset;
    public float ballDepthOffset;
    public float normalizedOffset;
    public bool isInsideTunnel;
    public float normLatOffset;
    public float normDepthOffset;
}