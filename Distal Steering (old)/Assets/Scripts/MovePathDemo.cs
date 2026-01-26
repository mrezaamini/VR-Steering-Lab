using UnityEngine;

[RequireComponent(typeof(VisualSizeHandler))]
public class MovePathDemo : MonoBehaviour
{
    public float from = 0.5f;
    public float to = 3f;
    public float duration = 2f;
    public bool pingPong = true;
    public AnimationCurve curve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private float t = 0f;
    private bool forward = true;
    private VisualSizeHandler visualPlane;

    void Start()
    {
        visualPlane = GetComponent<VisualSizeHandler>();
        visualPlane.desiredDistance = from;
    }

    void Update()
    {
        if (!visualPlane || duration <= 0f) return;

        float dir = forward ? 1f : -1f;
        t = Mathf.Clamp01(t + dir * Time.deltaTime / duration);

        float u = curve.Evaluate(t);
        float currentDepth = Mathf.Lerp(from, to, u);

        // Instead of moving transform, update the plane’s target distance
        visualPlane.desiredDistance = currentDepth;

        // Flip direction when needed
        if (t >= 1f && forward && pingPong) forward = false;
        else if (t <= 0f && !forward && pingPong) forward = true;
    }
}
