using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Checkpoint : MonoBehaviour
{
    [SerializeField] private Vector3 respawnOffset = Vector3.zero;

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

        player.SetCheckpoint(GetRespawnPosition());
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
