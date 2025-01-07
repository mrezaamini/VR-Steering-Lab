using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
public class DebugText : MonoBehaviour
{
    [SerializeField] private TMP_Text debugText;

    void Start()
    {
       
    }

    // Update is called once per frame
    void Update()
    {
        debugText.text = $"time: {Time.time}";
    }
}
