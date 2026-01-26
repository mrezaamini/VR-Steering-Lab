using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;


public class StateText : MonoBehaviour
{
    [SerializeField] private TMP_Text stateText;

    void Start()
    {

    }

    public void updateText(string input)
    {
        stateText.text = input;
    }

}
