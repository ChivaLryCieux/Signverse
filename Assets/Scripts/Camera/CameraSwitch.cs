using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;

public class CameraSwitch : MonoBehaviour
{
    CinemachineVirtualCamera localCamera;
    [Header ("Priority简要控制")]
    public int activePriority = 11;
    public int inactivePriority = 0;

    const string playerTag = "Player";

    void Start() 
    {
        localCamera = GetComponentInChildren<CinemachineVirtualCamera>();
        localCamera.Priority = inactivePriority;
    }
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            localCamera.Priority = activePriority;
        }
    }
    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            localCamera.Priority = inactivePriority;
        }
    }
}