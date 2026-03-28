using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollowPoint : MonoBehaviour
{
    [SerializeField] Transform playerTransform;
    private void Start()
    {
         
    }
    void Update()
    {
        transform.position = playerTransform.position + new Vector3(0, 1, 0);
    }
}
