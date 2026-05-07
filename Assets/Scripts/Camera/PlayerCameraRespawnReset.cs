using Cinemachine;
using UnityEngine;

public class PlayerCameraRespawnReset : MonoBehaviour
{
    [Header("目标")]
    [SerializeField] private Transform followTarget;

    [Header("复活相机重置")]
    [SerializeField] private bool restoreInitialPriorities = true;
    [SerializeField] private bool resetCameraSwitches = true;

    private PlayerCC player;
    private CinemachineVirtualCamera[] virtualCameras;
    private int[] initialPriorities;
    private CameraFollow25D[] legacyFollowCameras;
    private CameraFollowPoint[] followPoints;

    private void Awake()
    {
        player = GetComponent<PlayerCC>();
        ResolveFollowTarget();
        CacheCameras();
    }

    public void ResetAfterRespawn(Vector3 previousPosition, Vector3 respawnPosition)
    {
        ResolveFollowTarget();
        CacheCameras();

        if (resetCameraSwitches)
        {
            CameraSwitch.ResetAllSwitches();
        }

        for (int i = 0; i < followPoints.Length; i++)
        {
            if (followPoints[i] != null)
            {
                followPoints[i].SnapToTarget();
            }
        }

        Vector3 warpDelta = respawnPosition - previousPosition;

        for (int i = 0; i < virtualCameras.Length; i++)
        {
            CinemachineVirtualCamera virtualCamera = virtualCameras[i];
            if (virtualCamera == null)
            {
                continue;
            }

            if (restoreInitialPriorities && initialPriorities != null && i < initialPriorities.Length)
            {
                virtualCamera.Priority = initialPriorities[i];
            }

            Transform target = virtualCamera.Follow != null ? virtualCamera.Follow : followTarget;
            if (target != null)
            {
                virtualCamera.OnTargetObjectWarped(target, warpDelta);
            }

            virtualCamera.PreviousStateIsValid = false;
        }

        for (int i = 0; i < legacyFollowCameras.Length; i++)
        {
            if (legacyFollowCameras[i] != null)
            {
                legacyFollowCameras[i].SnapToTarget();
            }
        }
    }

    private void ResolveFollowTarget()
    {
        if (followTarget != null)
        {
            return;
        }

        followTarget = player != null ? player.GetControlTransform() : transform;
    }

    private void CacheCameras()
    {
        virtualCameras = GetComponentsInChildren<CinemachineVirtualCamera>(true);
        legacyFollowCameras = FindObjectsOfType<CameraFollow25D>();
        followPoints = FindObjectsOfType<CameraFollowPoint>();

        if (initialPriorities == null || initialPriorities.Length != virtualCameras.Length)
        {
            initialPriorities = new int[virtualCameras.Length];
            for (int i = 0; i < virtualCameras.Length; i++)
            {
                initialPriorities[i] = virtualCameras[i] != null ? virtualCameras[i].Priority : 0;
            }
        }
    }
}
