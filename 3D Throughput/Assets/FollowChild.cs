using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowChild : MonoBehaviour
{
    public Transform child;

    void LateUpdate()
    {
        if (child != null)
            transform.position = child.position;
    }
}