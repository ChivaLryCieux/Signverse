using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;

public class CameraSwitch : MonoBehaviour
{
    [Header ("相机选择与链接玩家相机")]
    CinemachineVirtualCamera localCamera;
    public CinemachineVirtualCamera playerForwardCamera;
    public CinemachineVirtualCamera playerBackwardCamera;
    public CinemachineVirtualCamera playerCloseShotCamera;

    public enum TargetSwitchCamera
    {
        localCamera,
        playerForwardCamera,
        playerBackwardCamera,
        playerCloseShotCamera
    }
    public TargetSwitchCamera targetSwitchCamera;


    [Header ("Priority简要控制")]
    public int activePriority = 11;
    public int inactivePriority = 0;

    const string playerTag = "Player";

    public static void ResetAllSwitches()
    {
        CameraSwitch[] switches = FindObjectsOfType<CameraSwitch>();
        for (int i = 0; i < switches.Length; i++)
        {
            if (switches[i] != null)
            {
                switches[i].ResetCameraPriorities();
            }
        }
    }

    void Start() 
    {
        localCamera = GetComponentInChildren<CinemachineVirtualCamera>();

        ResetCameraPriorities();
    }

    public void ResetCameraPriorities()
    {
        if (localCamera == null)
        {
            localCamera = GetComponentInChildren<CinemachineVirtualCamera>();
        }

        SetPriority(localCamera, inactivePriority);
        SetPriority(playerForwardCamera, inactivePriority);
        SetPriority(playerBackwardCamera, inactivePriority);
        SetPriority(playerCloseShotCamera, inactivePriority);
    }

    void OnTriggerEnter(Collider other)
    {
        //localCamera情况，特殊关卡特殊设置
        if(targetSwitchCamera == TargetSwitchCamera.localCamera)
        {
            if (other.CompareTag(playerTag))
            {
                SetPriority(localCamera, activePriority);
            }
            
        }
        //进入trigger后转到玩家左相机视角
        if(targetSwitchCamera == TargetSwitchCamera.playerForwardCamera)
        {
            if (other.CompareTag(playerTag))
            {
                SetPriority(playerForwardCamera, activePriority);
            }
            
        }
        //进入trigger后转到玩家右相机视角
        if(targetSwitchCamera == TargetSwitchCamera.playerBackwardCamera)
        {
            if (other.CompareTag(playerTag))
            {
                SetPriority(playerBackwardCamera, activePriority);
            }
            
        }
        if(targetSwitchCamera == TargetSwitchCamera.playerCloseShotCamera)
        {
            if (other.CompareTag(playerTag))
            {
                SetPriority(playerCloseShotCamera, activePriority);
            }
            
        }
    }
    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            ResetCameraPriorities();
        }
    }

    private static void SetPriority(CinemachineVirtualCamera virtualCamera, int priority)
    {
        if (virtualCamera != null)
        {
            virtualCamera.Priority = priority;
        }
    }
}
