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
    private static int activeSwitchCount;
    private bool playerInside;

    public static bool HasActiveSwitchCamera => activeSwitchCount > 0;

    public static void ResetAllSwitches()
    {
        activeSwitchCount = 0;
        CameraSwitch[] switches = FindObjectsOfType<CameraSwitch>();
        for (int i = 0; i < switches.Length; i++)
        {
            if (switches[i] != null)
            {
                switches[i].playerInside = false;
                switches[i].ResetCameraPriorities();
            }
        }
    }

    void Start() 
    {
        localCamera = GetComponentInChildren<CinemachineVirtualCamera>();

        ResetCameraPriorities();
    }

    private void OnDisable()
    {
        if (!playerInside)
        {
            return;
        }

        playerInside = false;
        activeSwitchCount = Mathf.Max(0, activeSwitchCount - 1);
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
        if (!IsPlayer(other))
        {
            return;
        }
    
        if (!playerInside)
        {
            playerInside = true;
            activeSwitchCount++;
        }
    
        //localCamera情况，特殊关卡特殊设置
        if(targetSwitchCamera == TargetSwitchCamera.localCamera)
        {
            ActivateCamera(localCamera);
            
        }
        //进入trigger后转到玩家左相机视角
        if(targetSwitchCamera == TargetSwitchCamera.playerForwardCamera)
        {
            ActivateCamera(playerForwardCamera);
            
        }
        //进入trigger后转到玩家右相机视角
        if(targetSwitchCamera == TargetSwitchCamera.playerBackwardCamera)
        {
            ActivateCamera(playerBackwardCamera);
            
        }
        if(targetSwitchCamera == TargetSwitchCamera.playerCloseShotCamera)
        {
            ActivateCamera(playerCloseShotCamera);
            
        }
    }
    void OnTriggerExit(Collider other)
    {
        if (IsPlayer(other))
        {
            if (playerInside)
            {
                playerInside = false;
                activeSwitchCount = Mathf.Max(0, activeSwitchCount - 1);
            }
    
            ResetCameraPriorities();
        }
    }

    private bool IsPlayer(Collider other)
    {
        return other != null && (other.CompareTag(playerTag) || other.GetComponentInParent<PlayerCC>() != null);
    }

    private void ActivateCamera(CinemachineVirtualCamera virtualCamera)
    {
        if (virtualCamera == null)
        {
            return;
        }

        int priority = Mathf.Max(activePriority, GetHighestCameraPriority() + 1);
        SetPriority(virtualCamera, priority);
    }

    private static int GetHighestCameraPriority()
    {
        int highestPriority = int.MinValue;
        CinemachineVirtualCamera[] cameras = FindObjectsOfType<CinemachineVirtualCamera>();
        for (int i = 0; i < cameras.Length; i++)
        {
            if (cameras[i] != null)
            {
                highestPriority = Mathf.Max(highestPriority, cameras[i].Priority);
            }
        }

        return highestPriority == int.MinValue ? 0 : highestPriority;
    }

    private static void SetPriority(CinemachineVirtualCamera virtualCamera, int priority)
    {
        if (virtualCamera != null)
        {
            virtualCamera.Priority = priority;
        }
    }
}
