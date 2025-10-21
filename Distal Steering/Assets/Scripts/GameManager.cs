using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System.Diagnostics;

public class GameManager : MonoBehaviour // GAME MANAGER FOR PLACEMENT PILOT STUDY, REMEMBER TO PUT THE UPDATED VERSION BACK INTO THE SOURCE FILE!!!!!!
{
    [Header("Participant Info")]
    public int participantID; // starts from 0
    public bool isMale; // true: long, false: short for shoulder breadth
    public bool isRightHanded;

    [Header("UI")]
    public AudioSource success_sound;
    public AudioSource error_sound;

    [Header("Steering")]
    public bool isSteering = false;



    void Start()
    {
        //placement start, end, path and scaling

    }

    // Update is called once per frame
    void Update()
    {


    }

}