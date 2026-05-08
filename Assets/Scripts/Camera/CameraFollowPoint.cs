using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;

[DefaultExecutionOrder(10000)]
public class CameraFollowPoint : MonoBehaviour
{
    [SerializeField] Transform playerTransform;
    [SerializeField] float cameraOffset = 0f;
    [SerializeField] Vector3 cameraOffsetFromPoint = new Vector3(-0.167f, 1.443f, -9.89f);
    [SerializeField] bool lockLinkedCamerasToOffset = true;
    [SerializeField] float switchReturnBlendTime = 0.45f;

    float velocityTransform;
    PlayerCC playerController;
    Camera mainCamera;
    CinemachineVirtualCamera[] linkedVirtualCameras;
    bool wasSwitchCameraActive;
    bool switchReturnBlendActive;
    float switchReturnBlendElapsed;
    Vector3 switchReturnStartPosition;
    Quaternion switchReturnStartRotation;

    public Vector3 CameraOffsetFromPoint => cameraOffsetFromPoint;

    private void Start()
    {
        playerController = playerTransform != null ? playerTransform.GetComponent<PlayerCC>() : null;
        Transform followTarget = playerController != null ? playerController.GetControlTransform() : playerTransform;
        velocityTransform = followTarget != null ? followTarget.position.y - 1 : 0f;
        CacheLinkedCameras();
    }

    public void SnapToTarget()
    {
        if (playerTransform == null)
        {
            return;
        }

        if (playerController == null)
        {
            playerController = playerTransform.GetComponent<PlayerCC>();
        }

        Transform followTarget = playerController != null ? playerController.GetControlTransform() : playerTransform;
        if (followTarget != null)
        {
            transform.position = new Vector3(followTarget.position.x, followTarget.position.y - cameraOffset, 0);
        }
    }

    void Update()
    {
        SnapToTarget();
    }

    private void LateUpdate()
    {
        if (!lockLinkedCamerasToOffset)
        {
            return;
        }

        bool switchCameraActive = CameraSwitch.HasActiveSwitchCamera;
        if (switchCameraActive)
        {
            wasSwitchCameraActive = true;
            switchReturnBlendActive = false;
            switchReturnBlendElapsed = 0f;
            return;
        }

        if (wasSwitchCameraActive)
        {
            wasSwitchCameraActive = false;
            BeginSwitchReturnBlend();
        }

        if (switchReturnBlendActive)
        {
            UpdateSwitchReturnBlend();
            return;
        }

        SnapLinkedCamerasToOffset();
    }

    private void CacheLinkedCameras()
    {
        mainCamera = Camera.main;
        CinemachineVirtualCamera[] allVirtualCameras = FindObjectsOfType<CinemachineVirtualCamera>(true);
        List<CinemachineVirtualCamera> matchingVirtualCameras = new List<CinemachineVirtualCamera>();

        for (int i = 0; i < allVirtualCameras.Length; i++)
        {
            CinemachineVirtualCamera virtualCamera = allVirtualCameras[i];
            if (virtualCamera != null && (virtualCamera.Follow == transform || virtualCamera.LookAt == transform))
            {
                matchingVirtualCameras.Add(virtualCamera);
            }
        }

        linkedVirtualCameras = matchingVirtualCameras.ToArray();
    }

    public void SnapLinkedCamerasToOffset()
    {
        Vector3 cameraPosition = GetOffsetCameraPosition();
        Quaternion cameraRotation = GetOffsetCameraRotation(cameraPosition);

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (mainCamera != null)
        {
            mainCamera.transform.SetPositionAndRotation(cameraPosition, cameraRotation);
        }

        if (linkedVirtualCameras == null || linkedVirtualCameras.Length == 0)
        {
            CacheLinkedCameras();
        }

        for (int i = 0; i < linkedVirtualCameras.Length; i++)
        {
            CinemachineVirtualCamera virtualCamera = linkedVirtualCameras[i];
            if (virtualCamera == null)
            {
                continue;
            }

            virtualCamera.transform.SetPositionAndRotation(cameraPosition, cameraRotation);
            virtualCamera.ForceCameraPosition(virtualCamera.transform.position, virtualCamera.transform.rotation);
            virtualCamera.PreviousStateIsValid = false;
        }
    }

    private void BeginSwitchReturnBlend()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (mainCamera == null || switchReturnBlendTime <= 0f)
        {
            switchReturnBlendActive = false;
            switchReturnBlendElapsed = 0f;
            SnapLinkedCamerasToOffset();
            return;
        }

        switchReturnBlendActive = true;
        switchReturnBlendElapsed = 0f;
        switchReturnStartPosition = mainCamera.transform.position;
        switchReturnStartRotation = mainCamera.transform.rotation;
    }

    private void UpdateSwitchReturnBlend()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (mainCamera == null)
        {
            switchReturnBlendActive = false;
            return;
        }

        switchReturnBlendElapsed += Time.deltaTime;
        float duration = Mathf.Max(0.01f, switchReturnBlendTime);
        float t = Mathf.Clamp01(switchReturnBlendElapsed / duration);
        float easedT = Mathf.SmoothStep(0f, 1f, t);

        Vector3 targetPosition = GetOffsetCameraPosition();
        Quaternion targetRotation = GetOffsetCameraRotation(targetPosition);
        Vector3 position = Vector3.Lerp(switchReturnStartPosition, targetPosition, easedT);
        Quaternion rotation = Quaternion.Slerp(switchReturnStartRotation, targetRotation, easedT);
        mainCamera.transform.SetPositionAndRotation(position, rotation);

        if (t < 1f)
        {
            return;
        }

        switchReturnBlendActive = false;
        SnapLinkedCamerasToOffset();
    }

    private Vector3 GetOffsetCameraPosition()
    {
        return transform.position + cameraOffsetFromPoint;
    }

    private Quaternion GetOffsetCameraRotation(Vector3 cameraPosition)
    {
        return Quaternion.LookRotation(transform.position - cameraPosition, Vector3.up);
    }
}
