using UnityEngine;

public class SunRotate : MonoBehaviour
{
    public enum RotateAxis
    {
        X,
        Y,
        Z
    }

    [Header("旋转轴（世界坐标）")]
    public RotateAxis rotateAxis = RotateAxis.X;

    [Header("旋转速度")]
    public float rotateSpeed = 10f;

    private Vector3 worldAxis;

    void Awake()
    {
        // 根据选择设置世界坐标轴
        switch (rotateAxis)
        {
            case RotateAxis.X:
                worldAxis = Vector3.right;
                break;

            case RotateAxis.Y:
                worldAxis = Vector3.up;
                break;

            case RotateAxis.Z:
                worldAxis = Vector3.forward;
                break;
        }
    }

    void Update()
    {
        // 按世界坐标旋转
        transform.Rotate(worldAxis, rotateSpeed * Time.deltaTime, Space.World);
    }
}