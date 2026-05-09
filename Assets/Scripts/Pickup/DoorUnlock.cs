using UnityEngine;

public class DoorUnlock : MonoBehaviour
{
    private Collider doorCollider;

    private bool unlocked;

    void Awake()
    {
        doorCollider = GetComponent<Collider>();
    }

    void OnTriggerEnter(Collider other)
    {
        if (unlocked)
        {
            return;
        }

        PlayerCC player = other.GetComponent<PlayerCC>();

        if (player == null)
        {
            return;
        }

        if (player.hasDoorPickup)
        {
            unlocked = true;

            doorCollider.enabled = false;
        }
    }
}