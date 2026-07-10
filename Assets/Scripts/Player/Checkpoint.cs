using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Checkpoint : MonoBehaviour
{
    [SerializeField] private Vector3 respawnOffset = Vector3.zero;

    [Header("标号")]
    [Tooltip("手动分配的检查点编号，从 0 开始。不分配则留 -1。")]
    [SerializeField] private int checkpointIndex = -1;

    // 检查点编号（-1 表示未分配）
    public int CheckpointIndex => checkpointIndex;

    private void Reset()
    {
        Collider checkpointCollider = GetComponent<Collider>();
        checkpointCollider.isTrigger = true;
    }

    private void OnValidate()
    {
        Collider checkpointCollider = GetComponent<Collider>();
        checkpointCollider.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerCC player = other.GetComponentInParent<PlayerCC>();
        if (player == null)
        {
            return;
        }

        if (player.IsDead)
        {
            return;
        }

        player.SetCheckpoint(GetRespawnPosition(), checkpointIndex);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(GetRespawnPosition(), new Vector3(0.5f, 1.5f, 0.5f));
    }

    private Vector3 GetRespawnPosition()
    {
        Transform respawnPoint = transform.childCount > 0 ? transform.GetChild(0) : transform;
        return respawnPoint.position + respawnOffset;
    }
}
