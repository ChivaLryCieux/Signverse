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
        PlayerCC player = other.GetComponent<PlayerCC>();
        if (player == null)
        {
            return;
        }

        if (player.IsDead)
        {
            return;
        }

        player.SetCheckpoint(transform.position + respawnOffset);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(transform.position + respawnOffset, new Vector3(0.5f, 1.5f, 0.5f));
    }
}
