using UnityEngine;

public class CameraFollow25D : MonoBehaviour
{
    [Header("追踪目标")]
    public Transform target;            // 玩家变换组件
    
    [Header("偏移与平滑")]
    public Vector3 offset = new Vector3(0, 2, -10); // 相对于玩家的固定偏移
    public float smoothSpeed = 5f;      // 跟随平滑度（值越大越快）
    
    [Header("死区设置 (Dead Zone)")]
    public Vector2 deadZone = new Vector2(1f, 0.5f); // 玩家在这个范围内移动时，相机不动

    [Header("边界限制 (可选)")]
    public bool useBounds;
    public Vector2 minBounds;           // 关卡左下角坐标
    public Vector2 maxBounds;           // 关卡右上角坐标

    private Vector3 targetPosition;

    void LateUpdate()
    {
        if (target == null) return;

        // 1. 计算目标位置
        targetPosition = target.position + offset;

        // 2. 应用死区逻辑 (可选)
        // 如果当前相机位置与目标位置的差距在死区内，则保持部分坐标不变
        Vector3 currentPos = transform.position;
        if (Mathf.Abs(currentPos.x - targetPosition.x) < deadZone.x) targetPosition.x = currentPos.x;
        if (Mathf.Abs(currentPos.y - targetPosition.y) < deadZone.y) targetPosition.y = currentPos.y;

        // 3. 限制相机不超出关卡边界
        if (useBounds)
        {
            targetPosition.x = Mathf.Clamp(targetPosition.x, minBounds.x, maxBounds.x);
            targetPosition.y = Mathf.Clamp(targetPosition.y, minBounds.y, maxBounds.y);
        }

        // 4. 平滑移动 (使用 Lerp 或 SmoothDamp)
        // 注意：Z轴保持 offset.z 不变，确保 2.5D 视角固定
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, targetPosition, smoothSpeed * Time.deltaTime);
        transform.position = new Vector3(smoothedPosition.x, smoothedPosition.y, offset.z);
    }
}