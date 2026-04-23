using System.Collections.Generic;

[System.Serializable]
public class TrialData
{
    public int trialIndex;
    public float startTime;
    public float endTime;
    public float Duration => endTime - startTime;
    public bool completed;
    public int resetCount;
    public float travelledPath;
    public TrialConfig config;
    public List<FrameData> frames = new List<FrameData>();

    // Populated by StudyManager at trial end
    public float avgNormalizedOffset;
    public float maxRadialDistance;
    public int FrameCount => frames.Count;
}