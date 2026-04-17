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

    void Start() 
    {
        localCamera = GetComponentInChildren<CinemachineVirtualCamera>();

        localCamera.Priority = inactivePriority;
        playerForwardCamera.Priority = inactivePriority;
        playerBackwardCamera.Priority = inactivePriority;
        playerCloseShotCamera.Priority = inactivePriority;
    }
    void OnTriggerEnter(Collider other)
    {
        //localCamera情况，特殊关卡特殊设置
        if(targetSwitchCamera == TargetSwitchCamera.localCamera)
        {
            if (other.CompareTag(playerTag))
            {
                localCamera.Priority = activePriority;
            }
            
        }
        //进入trigger后转到玩家左相机视角
        if(targetSwitchCamera == TargetSwitchCamera.playerForwardCamera)
        {
            if (other.CompareTag(playerTag))
            {
                playerForwardCamera.Priority = activePriority;
            }
            
        }
        //进入trigger后转到玩家右相机视角
        if(targetSwitchCamera == TargetSwitchCamera.playerBackwardCamera)
        {
            if (other.CompareTag(playerTag))
            {
                playerBackwardCamera.Priority = activePriority;
            }
            
        }
        if(targetSwitchCamera == TargetSwitchCamera.playerCloseShotCamera)
        {
            if (other.CompareTag(playerTag))
            {
                playerCloseShotCamera.Priority = activePriority;
            }
            
        }
    }
    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            localCamera.Priority = inactivePriority;
            playerForwardCamera.Priority = inactivePriority;
            playerBackwardCamera.Priority = inactivePriority;
            playerCloseShotCamera.Priority = inactivePriority;
        }
    }
}