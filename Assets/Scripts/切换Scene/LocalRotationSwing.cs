using UnityEngine;

public class LocalRotationSwing : MonoBehaviour
{
    [Header("X轴摇摆")]
    public float xAmplitude = 15f;
    public float xSpeed = 1f;
    public float xTimeOffset = 0f;

    [Header("Y轴摇摆")]
    public float yAmplitude = 15f;
    public float ySpeed = 1f;
    public float yTimeOffset = 1f;

    [Header("Z轴摇摆")]
    public float zAmplitude = 15f;
    public float zSpeed = 1f;
    public float zTimeOffset = 2f;

    private Vector3 initialRotation;

    void Awake()
    {
        // 记录初始局部旋转
        initialRotation = transform.localEulerAngles;
    }

    void Update()
    {
        // Sin 来回摇摆
        float x =
            Mathf.Sin(Time.time * xSpeed + xTimeOffset)
            * xAmplitude;

        float y =
            Mathf.Sin(Time.time * ySpeed + yTimeOffset)
            * yAmplitude;

        float z =
            Mathf.Sin(Time.time * zSpeed + zTimeOffset)
            * zAmplitude;

        // 基于初始旋转偏移
        transform.localRotation =
            Quaternion.Euler(
                initialRotation.x + x,
                initialRotation.y + y,
                initialRotation.z + z
            );
    }
}