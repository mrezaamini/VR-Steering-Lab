using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;
using TMPro;
using UnityEngine.Rendering;
public class Recorder : MonoBehaviour
{
    public Transform camera;
    public Transform controller;

    public Transform controllerLeft;
    public Transform controllerRight;

    public Transform pointer;
    public Transform pointerLeft;
    public Transform pointerRight;

    public Transform target;

    public Transform[] calibrationPoints;

    public GameObject syringe;
    public GameObject syringeRight;
    public GameObject syringeLeft;

    private string FilePathSummary;
    private string FilePathAll;
    private string FilePathCollider;

    public bool isInside;

    public int clickCounter = 0;

    public int totalError;

    public bool assistanceActivated;

    private float previousTime = 0;

    public bool inCollider;

    public Canvas text;
    public TMP_Text status;


    public Transform selectionCollider;
    public Transform recorderCollider;

    public SphereInsert sphereInsert;

    public string ParticipantNumber;
    public bool leftHanded;
    public bool isNurse;

    private bool experiementDone;
    private bool training;
    public ActivateVisualGuides activateVisualGuides;

    // Start is called before the first frame update
    void Start()
    {
        if (leftHanded)
        {
            controller = controllerLeft;
            syringe = syringeLeft;
            pointer = pointerLeft;
        }
        else
        {
            controller = controllerRight;
            syringe = syringeRight;
            pointer = pointerRight;
        }
            
        experiementDone = false;    
        if(int.Parse(ParticipantNumber) % 2 == 0)
        {
            assistanceActivated = false;
        }
        else
        {
            assistanceActivated = true;

        }

        calibrationPoints = new Transform[6];
        string FileName = ParticipantNumber + "_" + DateTime.Now.ToString("h-mm-ss") + ".txt";
        FilePathAll = Application.dataPath + "/DataCollection/All/All_" + FileName;
        FilePathSummary = Application.dataPath + "/DataCollection/Summary/Summary_" + FileName;
        FilePathCollider = Application.dataPath + "/DataCollection/Collider/ColliderArea_" + FileName;

        string message = "ParticipantNumber; " +
            "Nurse; " +
            "Click Counter; " +
            "Left Handed; " +
            "Assistance Activated; " +
            "Controller Position; " +
            "Pointer Position; " +
            "Camera Position; " +
            "Target Position; " +
            "Calibration Point Position 1; " +
            "Calibration Point Position 2; " +
            "Calibration Point Position 3; " +
            "Calibration Point Position 4; " +
            "Calibration Point Position 5; " +
            "Calibration Point Position 6; " +
            "Syringe angle x; " +
            "Syringe angle y; " +
            "Syringe angle z; " +
            "Angle Offset X ; " +
            "Angle Offset Y ; " +
            "Distance b/w Syringe and Insert position; " +
            "Is Inside; " +
            "Selection Collider; " +
            "Recorder Collider; " +
            "Total Time; " +
            "Time Seconds; " +
            "Time Milliseconds; ";

        using (StreamWriter sw = File.AppendText(FilePathSummary))
        {
            sw.WriteLine(message);
        }

        message = "ParticipantNumber; " +
            "Nurse; " +
            "Click Counter; " +
            "Assistance Activated; " +
            "Controller Position; " +
            "Pointer Position; " +
            "Target Position; " +
            "Is Inside; " +
            "Total Time; ";
        using (StreamWriter sw = File.AppendText(FilePathAll))
        {
            sw.WriteLine(message);
        }

        message = "ParticipantNumber; " +
            "Nurse; " +
            "Click Counter; " +
            "Assistance Activated; " +
            "Controller Position; " +
            "Pointer Position; " +
            "Target Position; " +
            "Syringe angle x; " +
            "Syringe anfle y; " +
            "Syringe angle z; " +
            "Accuracy x; " +
            "Accuracy y; " +
            "isInside; " +
            "Distance b/w Syringe and Insert position; " +
            "Total Time; ";

        // distance between syringe and target
        using (StreamWriter sw = File.AppendText(FilePathCollider))
        {
            sw.WriteLine(message);
        }
    }

    public void StartText(string text)
    {
        status.text = text;
    }

    public void StopText()
    {
        status.text = string.Empty;
    }


    public void SetParticipantNumber(string participantNumber)
    {
        this.ParticipantNumber = participantNumber;
    }

    public void SetAssistance(bool assistance)
    {
        assistanceActivated = assistance;
    }

    public void SetVisualGuides()
    {
        activateVisualGuides = FindObjectOfType<ActivateVisualGuides>();
    }

    public Vector2 AngleOffset()
    {
        float x = 0;
        float y = 0;

        if (syringe.transform.eulerAngles.x > 180f)
        {
            x = 396.15f - syringe.transform.eulerAngles.x;
        }
        else
            x = syringe.transform.eulerAngles.x - 36f;

        if (syringe.transform.eulerAngles.y > 180f)
        {
            y = syringe.transform.eulerAngles.y - 347.85f;
        }
        else
            y = syringe.transform.eulerAngles.y + 12.15f;
        
        return new Vector2(x, y);
    }

    public void summaryWriter()
    {
        clickCounter++;
        Vector2 offset = AngleOffset();
        if (!training)
        {
            string message = ParticipantNumber + ";" +
            isNurse + ";" +
            clickCounter + ";" +
            leftHanded + ";" +
            assistanceActivated + ";" +
            controller.position + ";" +
            pointer.position + ";" +
            camera.position + ";" +
            target.position + ";" +
            calibrationPoints[0].position.ToString() + ";" +
            calibrationPoints[1].position.ToString() + ";" +
            calibrationPoints[2].position.ToString() + ";" +
            calibrationPoints[3].position.ToString() + ";" +
            calibrationPoints[4].position.ToString() + ";" +
            calibrationPoints[5].position.ToString() + ";" +
            syringe.transform.eulerAngles.x + ";" +
            syringe.transform.eulerAngles.y + ";" + 
            syringe.transform.eulerAngles.z + ";" +
            offset.x + ";" + 
            offset.y + ";" +
            sphereInsert.distance + ";" +
            isInside + ";" +
            selectionCollider.position + ";" +
            recorderCollider.position + ";" +
            Time.time + ";" +
            (Time.time - previousTime) + ";" +
            (Time.time - previousTime) * 1000;

            previousTime = Time.time;

            using (StreamWriter sw = File.AppendText(FilePathSummary))
            {
                sw.WriteLine(message);
            }
        }
        if(clickCounter == 0)
        {
            StartText("Training session");
            Invoke("StopText", 4f);
        }
        if(clickCounter == 10)
        {
            training = false;
            activateVisualGuides.SetActivation(assistanceActivated);
            StartText("Experiement starting: Trial 1 :: visual guides: " + assistanceActivated);
            Invoke("StopText", 4f);
        }
        if(clickCounter == 21)
        {
            assistanceActivated = !assistanceActivated;
            activateVisualGuides.SetActivation(assistanceActivated);
            StartText("Take off headset to fill out the questionnaires before moving on to trial 2 :: visual guides: " + assistanceActivated);
            Invoke("StopText", 10f);
        }
        if(clickCounter == 32)
        {
            experiementDone = true;
            text.enabled = true;
            Time.timeScale = 0;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (!experiementDone)
        {
            string message = "";
            if (target != null)
            {
                message = ParticipantNumber + "; " +
                isNurse + ";" +
                clickCounter + ";" +
                assistanceActivated + ";" +
                controller.position + ";" +
                pointer.position + ";" +
                target.position + ";" +
                isInside + ";" +
                Time.time;
            }
            else
            {
                message = ParticipantNumber + "; " +
                isNurse + ";" +
                clickCounter + ";" +
                assistanceActivated + ";" +
                controller.position + ";" +
                pointer.position + ";" +
                "TargetNotSetYet;" +
                isInside + ";" +
                Time.time;
            }

            using (StreamWriter sw = File.AppendText(FilePathAll))
            {
                sw.WriteLine(message);
            }
            


            if (inCollider && sphereInsert != null)
            {
                Vector2 accuracy = AngleOffset();
                message = ParticipantNumber + "; " +
                isNurse + ";" +
                clickCounter + ";" +
                assistanceActivated + ";" +
                controller.position + ";" +
                pointer.position + ";" +
                target.position + ";" +
                syringe.transform.eulerAngles.x + ";" +
                syringe.transform.eulerAngles.y + ";" +
                syringe.transform.eulerAngles.z + ";" +
                accuracy.x + ";" + 
                accuracy.y + ";" +
                isInside + ";" +
                sphereInsert.distance + ";" +
                Time.time;

                using (StreamWriter sw = File.AppendText(FilePathCollider))
                {
                    sw.WriteLine(message);
                }
            }
        }
    }
}
