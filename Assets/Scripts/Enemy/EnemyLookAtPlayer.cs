using UnityEngine;

[RequireComponent(typeof(AudioSource))]
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

    [Header("音效设置")]
    [Tooltip("玩家进入范围时播放的音效")]
    public AudioClip detectClip;

    [Header("调试")]
    public bool debugDraw = true;

    private Transform player;

    public bool playerInRange;

    // 用于判断“刚进入”
    private bool lastPlayerInRange;

    private AudioSource audioSource;

    public enum Axis
    {
        X,
        Y,
        Z
    }

    // ------------------------
    // 初始化
    // ------------------------

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    void Update()
    {
        DetectPlayer();

        // ------------------------
        // 检测“进入瞬间”
        // ------------------------

        if (!lastPlayerInRange && playerInRange)
        {
            OnPlayerEnter();
        }

        lastPlayerInRange = playerInRange;

        // ------------------------
        // 持续朝向玩家
        // ------------------------

        if (playerInRange && player != null)
        {
            RotateTowardsPlayer();
        }
    }

    // ------------------------
    // 玩家进入时触发
    // ------------------------

    void OnPlayerEnter()
    {
        if (detectClip == null)
            return;
        audioSource.Stop();
        audioSource.PlayOneShot(detectClip);
       
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