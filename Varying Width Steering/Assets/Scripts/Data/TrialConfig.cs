
[System.Serializable]
public class TrialConfig
{
    public int trialIndex;
    public float pathLength;
    public float startWidth;   // tunnel radius at entry
    public float endWidth;     // tunnel radius at exit
    public MovementDirection direction;

    // Human-readable ID for CSV
    public string ID => $"T{trialIndex:00}_L{pathLength}_S{startWidth}_E{endWidth}_{direction}";
}