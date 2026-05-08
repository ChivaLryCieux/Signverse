using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollowPoint : MonoBehaviour
{
    [SerializeField] Transform playerTransform;
    [SerializeField] float cameraOffset = 0f;

    float velocityTransform;
    PlayerCC playerController;

    private void Start()
    {
        playerController = playerTransform != null ? playerTransform.GetComponent<PlayerCC>() : null;
        Transform followTarget = playerController != null ? playerController.GetControlTransform() : playerTransform;
        velocityTransform = followTarget != null ? followTarget.position.y - 1 : 0f;
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
}
