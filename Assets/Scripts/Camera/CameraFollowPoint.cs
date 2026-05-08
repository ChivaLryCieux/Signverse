using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;

[DefaultExecutionOrder(10000)]
public class CameraFollowPoint : MonoBehaviour
{
    [SerializeField] Transform playerTransform;
    [SerializeField] float cameraOffset = 0f;
    [SerializeField] Vector3 cameraOffsetFromPoint = new Vector3(5.869836f, -7.93083f, 1.726473f);
    [SerializeField] bool lockLinkedCamerasToOffset = true;

    float velocityTransform;
    PlayerCC playerController;
    Camera mainCamera;
    CinemachineVirtualCamera[] linkedVirtualCameras;

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
        Vector3 cameraPosition = transform.position + cameraOffsetFromPoint;
        Quaternion cameraRotation = Quaternion.LookRotation(transform.position - cameraPosition, Vector3.up);

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
            virtualCamera.ForceCameraPosition(cameraPosition, cameraRotation);
            virtualCamera.PreviousStateIsValid = false;
        }
    }
}
