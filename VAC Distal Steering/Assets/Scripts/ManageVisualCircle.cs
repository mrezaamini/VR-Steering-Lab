using UnityEngine;
using System.Collections.Generic;
public class ManageVisualCircle : MonoBehaviour
{
    [Header("Visual Prefabs (same order as your condition ids)")]
    public GameObject[] visualPrefabs;

    GameObject currentVisual;

    Dictionary<(float W, float L), int> lookup = new Dictionary<(float, float), int>()
    {
        {(2f, 25f), 0},
        {(2f, 35f), 1},
        {(2f, 50f), 2},

        {(3f, 25f), 3},
        {(3f, 35f), 4},
        {(3f, 50f), 5},

        {(4.5f, 25f), 6},
        {(4.5f, 35f), 7},
        {(4.5f, 50f), 8},

        {(6f, 25f), 9},
        {(6f, 35f), 10},
        {(6f, 50f), 11},
    };

    public void SetVisual(float W, float L)
    {
        
        // Destroy previous child
        if (currentVisual != null)
            Destroy(currentVisual);
        int index = GetIndex(W, L);
        // Spawn new child under THIS parent
        currentVisual = Instantiate(visualPrefabs[index], transform);

        currentVisual.transform.localPosition = Vector3.zero;
        //currentVisual.transform.localRotation = Quaternion.identity;
    }

    public int GetIndex(float W, float L)
    {
        if (lookup.TryGetValue((W, L), out int index))
            return index;

        Debug.LogError("Invalid W/L combination!");
        return -1;
    }

    public void setScale(float D)
    {
        if(currentVisual != null)
        {
            //assuming  1 as the base depth
            float scaleFactor = D / 1;
            Vector3 ls = currentVisual.transform.localScale;
            currentVisual.transform.localScale = new Vector3(
                    ls.x * scaleFactor,
                    ls.y * scaleFactor,
                    ls.z              // leave Z untouched
             );
        }
        
    }



}
