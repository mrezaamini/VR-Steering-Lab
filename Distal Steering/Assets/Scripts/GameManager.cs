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

    [Header("Scene")]
    public Camera mainCamera;
    public float referenceDepth = 5f; // base depth for condition
    public Vector2 baseSize = new Vector2(1f, 1f); // base width/length at base depth


    void Start()
    {
        //placement start, end, path and scaling
        if (!mainCamera) mainCamera = Camera.main; //make sure camera is attached

    }

    // Update is called once per frame
    void Update()
    {
        


    }

    private void adjustPathSize() // to adjust path size based on the depth relative to the base condition
    {
        
    }

}