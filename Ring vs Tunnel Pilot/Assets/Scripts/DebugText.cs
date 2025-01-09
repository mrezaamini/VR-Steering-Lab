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

    public void updateText (string input)
    {
        debugText.text = input;
    }

   
}
