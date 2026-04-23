using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ControllerSyringe : MonoBehaviour
{
    public void SetSyringe(GameObject pointer, GameObject syringe, GameObject controllerVisual, bool leftHanded)
    {
        if (leftHanded)
        {
            pointer.transform.localPosition = new Vector3(-0.0168007724f, -0.118902676f, -0.128902212f);
        }
        else
            pointer.transform.localPosition = new Vector3(0.0198299997f, -0.119829997f, -0.117859997f);
        syringe.SetActive(true);
        controllerVisual.SetActive(false);
    }
}
