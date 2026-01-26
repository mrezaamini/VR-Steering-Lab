using UnityEngine;
using System.Diagnostics;

public class StopwatchTimer : MonoBehaviour
{
    private Stopwatch stopwatch;

    void Start()
    {
        stopwatch = new Stopwatch();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            //stopwatch.Reset();
            //stopwatch.Start();
            stopwatch.Restart();
            UnityEngine.Debug.Log("Stopwatch Started!");
        }

        if (Input.GetKeyDown(KeyCode.S))
        {
            //if (stopwatch.IsRunning)
            //{
                stopwatch.Stop();
            
                UnityEngine.Debug.Log("Stopwatch Stopped! Elapsed Time: " + stopwatch.ElapsedMilliseconds + " ms");
            stopwatch.Reset();
            //}
        }
    }
}
