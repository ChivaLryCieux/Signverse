using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollowPoint : MonoBehaviour
{
    [SerializeField] Transform playerTransform;
    [SerializeField] float cameraOffset = 0f;

    float velocityTransform;

    private void Start()
    {

        velocityTransform = playerTransform.position.y - 1;
    }
    void Update()
    {
        transform.position = new Vector3(playerTransform.position.x, playerTransform.position.y  - cameraOffset, 0);
    }
}
