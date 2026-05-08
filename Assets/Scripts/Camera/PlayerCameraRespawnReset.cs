using Cinemachine;
using UnityEngine;

public class PlayerCameraRespawnReset : MonoBehaviour
{
    private static readonly Vector3 DefaultMainCameraOffsetFromPoint = new Vector3(-2.934f, 1.9714456f, -9.374474f);

    [Header("目标")]
    [SerializeField] private Transform followTarget;

    [Header("复活相机重置")]
    [SerializeField] private bool restoreInitialPriorities = true;
    [SerializeField] private bool resetCameraSwitches = true;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Vector3 mainCameraOffsetFromPoint = new Vector3(-2.934f, 1.9714456f, -9.374474f);

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

        Transform cameraFollowPoint = ResolveCameraFollowPoint();
        SnapMainCameraToFollowPoint(cameraFollowPoint);

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

            if (cameraFollowPoint != null &&
                (virtualCamera.Follow == cameraFollowPoint || virtualCamera.LookAt == cameraFollowPoint))
            {
                Vector3 cameraPosition = GetMainCameraPosition(cameraFollowPoint);
                Quaternion cameraRotation = Quaternion.LookRotation(cameraFollowPoint.position - cameraPosition, Vector3.up);
                virtualCamera.transform.SetPositionAndRotation(cameraPosition, cameraRotation);
                virtualCamera.ForceCameraPosition(cameraPosition, cameraRotation);
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

    private Transform ResolveCameraFollowPoint()
    {
        for (int i = 0; i < virtualCameras.Length; i++)
        {
            CinemachineVirtualCamera virtualCamera = virtualCameras[i];
            if (virtualCamera == null)
            {
                continue;
            }

            if (virtualCamera.Follow != null && virtualCamera.Follow.GetComponent<CameraFollowPoint>() != null)
            {
                return virtualCamera.Follow;
            }

            if (virtualCamera.LookAt != null && virtualCamera.LookAt.GetComponent<CameraFollowPoint>() != null)
            {
                return virtualCamera.LookAt;
            }
        }

        for (int i = 0; i < followPoints.Length; i++)
        {
            if (followPoints[i] != null)
            {
                return followPoints[i].transform;
            }
        }

        return followTarget;
    }

    private void SnapMainCameraToFollowPoint(Transform cameraFollowPoint)
    {
        if (cameraFollowPoint == null)
        {
            return;
        }

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (mainCamera == null)
        {
            return;
        }

        Vector3 cameraPosition = GetMainCameraPosition(cameraFollowPoint);
        mainCamera.transform.SetPositionAndRotation(
            cameraPosition,
            Quaternion.LookRotation(cameraFollowPoint.position - cameraPosition, Vector3.up));
    }

    private Vector3 GetMainCameraPosition(Transform cameraFollowPoint)
    {
        Vector3 offset = GetCameraOffsetFromPoint(cameraFollowPoint);
        return cameraFollowPoint.position + offset;
    }

    private Vector3 GetCameraOffsetFromPoint(Transform cameraFollowPoint)
    {
        if (cameraFollowPoint == null)
        {
            return DefaultMainCameraOffsetFromPoint;
        }

        CameraFollowPoint followPoint = cameraFollowPoint.GetComponent<CameraFollowPoint>();
        if (followPoint != null)
        {
            followPoint.SnapLinkedCamerasToOffset();
        }

        Vector3 offset = followPoint != null ? followPoint.CameraOffsetFromPoint : mainCameraOffsetFromPoint;
        if (offset == Vector3.zero)
        {
            offset = DefaultMainCameraOffsetFromPoint;
        }

        return offset;
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
        followPoints = GetComponentsInChildren<CameraFollowPoint>(true);

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
