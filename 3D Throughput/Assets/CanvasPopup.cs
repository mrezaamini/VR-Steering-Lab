using UnityEngine;
using System.Collections;

public class CanvasPopup : MonoBehaviour
{
    public RectTransform popupCanvas; 
    public float popupDuration = 0.4f; 
    public float visibleTime = 2f;     

    public void ShowPopup()
    {
       
        popupCanvas.gameObject.SetActive(true);
        popupCanvas.localScale = Vector3.zero;
        StopAllCoroutines();  
        StartCoroutine(PopupRoutine());
    }

    private void Start()
    {
        popupCanvas.localScale = Vector3.zero;
    }

    private IEnumerator PopupRoutine()
    {
        // Make sure the canvas is active and start hidden
        popupCanvas.gameObject.SetActive(true);
        popupCanvas.localScale = Vector3.zero;

        // POP IN with bounce
        float elapsed = 0f;
        while (elapsed < popupDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / popupDuration;

            // Bounce easing (easeOutBack)
            float bounce = EaseOutBack(t);

            popupCanvas.localScale = Vector3.LerpUnclamped(Vector3.zero, Vector3.one, bounce);
            yield return null;
        }
        popupCanvas.localScale = Vector3.one;

        // Stay visible for a while
        yield return new WaitForSeconds(visibleTime);

        // POP OUT with bounce (reverse)
        elapsed = 0f;
        while (elapsed < popupDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / popupDuration;

            // Bounce easing in reverse
            float bounce = EaseInBack(t);

            popupCanvas.localScale = Vector3.LerpUnclamped(Vector3.one, Vector3.zero, bounce);
            yield return null;
        }
        popupCanvas.localScale = Vector3.zero;
        popupCanvas.gameObject.SetActive(false);
    }

    // --- Bounce easing functions ---
    private float EaseOutBack(float t)
    {
        float c1 = 1.70158f;
        float c3 = c1 + 1f;
        return 1 + c3 * Mathf.Pow(t - 1, 3) + c1 * Mathf.Pow(t - 1, 2);
    }

    private float EaseInBack(float t)
    {
        float c1 = 1.70158f;
        float c3 = c1 + 1f;
        return c3 * t * t * t - c1 * t * t;
    }
}
