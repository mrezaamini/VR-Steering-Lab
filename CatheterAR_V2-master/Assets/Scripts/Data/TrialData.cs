using System.Collections.Generic;

[System.Serializable]
public class TrialData
{
    public int trialIndex;
    public float startTime;
    public float endTime;
    public double Duration;
    public int HitNumber;
    public double hitTime;
    public bool completed;
    public int resetCount;
    public float travelledPath;
    public TrialConfig config;
    public List<FrameData> frames = new List<FrameData>();

    // Populated by StudyManager at trial end
    public float speed;
    public float latOffset;
    public float sdSpeed;
    public float sdLatOffset;
    public float sdDepthOffset;
    public float sdBivariate;
    public float sdRadialOffset;
    public float n_latOffset; //normalizd
    public float depthOffset;
    public float n_depthOffset;
    public float radialOffset;
    public float n_radialOffset;
    public float effectiveAmplitude;
    public float sd_nDepthOffset; //sd of normalized ones
    public float sd_nLatOffset;
    public float sd_nRadOffset;
    public float sdBivariate_norm;


    public int FrameCount => frames.Count;
}