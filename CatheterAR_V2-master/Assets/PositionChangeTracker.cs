using UnityEngine;

public class PositionChangeTracker : MonoBehaviour
{
    private Vector3 lastWorldPos;

    void Awake() => lastWorldPos = transform.position;

    // Fires when this object's parent changes
    void OnTransformParentChanged()
    {
        UnityEngine.Debug.LogWarning(
            $"[Tracker] {gameObject.name} PARENT CHANGED\n" +
            $"new parent: {(transform.parent != null ? transform.parent.name : "none")}\n" +
            $"world pos now: {transform.position}\n" +
            $"world pos was: {lastWorldPos}",
            gameObject);
    }

    // Fires when this object is enabled
    void OnEnable()
    {
        UnityEngine.Debug.LogWarning(
            $"[Tracker] {gameObject.name} OnEnable\n" +
            $"world pos: {transform.position}",
            gameObject);
    }

    // Fires when transform changes — Unity internal callback
    void OnTransformChildrenChanged()
    {
        UnityEngine.Debug.LogWarning(
            $"[Tracker] {gameObject.name} CHILDREN CHANGED\n" +
            $"world pos: {transform.position}",
            gameObject);
    }

    void LateUpdate()
    {
        if (transform.position != lastWorldPos)
        {
            UnityEngine.Debug.LogWarning(
                $"[Tracker] {gameObject.name} world pos changed\n" +
                $"from: {lastWorldPos}\n" +
                $"to:   {transform.position}\n" +
                $"local pos: {transform.localPosition}\n" +
                $"parent: {(transform.parent != null ? transform.parent.name : "none")}",
                gameObject);

            lastWorldPos = transform.position;
        }
    }
}