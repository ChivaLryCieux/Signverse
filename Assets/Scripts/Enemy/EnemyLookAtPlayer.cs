using UnityEngine;

public class EnemyLookAtPlayer : MonoBehaviour
{
    [Header("检测范围")]
    [Tooltip("索敌半径")]
    public float field = 5f;

    [Header("旋转设置")]
    [Tooltip("旋转速度（度/秒）")]
    public float rotateSpeed = 180f;

    [Tooltip("哪个轴的正方向用于朝向玩家")]
    public Axis forwardAxis = Axis.Z;

    [Header("调试")]
    public bool debugDraw = true;

    private Transform player;
    public bool playerInRange;

    public enum Axis
    {
        X,
        Y,
        Z
    }

    void Update()
    {
        DetectPlayer();

        if (playerInRange && player != null)
        {
            RotateTowardsPlayer();
        }
    }

    // ------------------------
    // 玩家检测
    // ------------------------

    void DetectPlayer()
    {
        Collider[] hits =
            Physics.OverlapSphere(
                transform.position,
                field
            );

        playerInRange = false;

        foreach (var hit in hits)
        {
            if (hit.CompareTag("Player"))
            {
                player = hit.transform;
                playerInRange = true;
                return;
            }
        }
    }

    // ------------------------
    // 平滑旋转
    // ------------------------

    void RotateTowardsPlayer()
    {
        Vector3 direction =
            player.position - transform.position;

        if (direction.sqrMagnitude < 0.001f)
            return;

        direction.Normalize();

        Quaternion targetRotation =
            GetTargetRotation(direction);

        transform.rotation =
            Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                rotateSpeed * Time.deltaTime
            );
    }

    // ------------------------
    // 根据指定轴计算目标旋转
    // ------------------------

    Quaternion GetTargetRotation(Vector3 direction)
    {
        switch (forwardAxis)
        {
            case Axis.X:
                return Quaternion.FromToRotation(
                    Vector3.right,
                    direction
                ) * transform.rotation;

            case Axis.Y:
                return Quaternion.FromToRotation(
                    Vector3.up,
                    direction
                ) * transform.rotation;

            default: // Z
                return Quaternion.LookRotation(direction);
        }
    }

    // ------------------------
    // Gizmos 可视化检测范围
    // ------------------------

    void OnDrawGizmosSelected()
    {
        if (!debugDraw) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(
            transform.position,
            field
        );
    }
}