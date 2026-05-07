using System.Collections.Generic;
using UnityEngine;

public class PlayerDirectionalMoveBlockTrigger : MonoBehaviour
{
    [SerializeField] private PlayerCC player;
    [SerializeField] private Collider triggerCollider;
    [SerializeField] private Rigidbody triggerRigidbody;
    [SerializeField] private bool ignoreLowContacts = true;
    [SerializeField] private float lowContactHeightPadding = 0.02f;

    private readonly List<Collider> activeContacts = new List<Collider>();

    private void Awake()
    {
        if (player == null)
        {
            player = GetComponentInParent<PlayerCC>();
        }

        if (triggerCollider == null)
        {
            triggerCollider = GetComponent<Collider>();
        }

        EnsureTriggerSetup();
    }

    private void OnTriggerEnter(Collider other)
    {
        RegisterContact(other);
    }

    private void OnTriggerStay(Collider other)
    {
        if (CanRegisterContact(other))
        {
            RegisterContact(other);
        }
        else
        {
            UnregisterContact(other);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        UnregisterContact(other);
    }

    private void OnDisable()
    {
        if (player == null)
        {
            activeContacts.Clear();
            return;
        }

        for (int i = activeContacts.Count - 1; i >= 0; i--)
        {
            player.ExitDirectionalMoveBlockTrigger(activeContacts[i], transform);
        }

        activeContacts.Clear();
    }

    private void RegisterContact(Collider other)
    {
        if (!CanRegisterContact(other))
        {
            return;
        }

        if (!activeContacts.Contains(other))
        {
            activeContacts.Add(other);
        }

        player.EnterDirectionalMoveBlockTrigger(other, transform);
    }

    private void UnregisterContact(Collider other)
    {
        if (player != null)
        {
            player.ExitDirectionalMoveBlockTrigger(other, transform);
        }

        activeContacts.Remove(other);
    }

    private bool CanRegisterContact(Collider other)
    {
        if (player == null || !player.CanUseDirectionalMoveBlockContact(other, transform))
        {
            return false;
        }

        if (!ignoreLowContacts || triggerCollider == null)
        {
            return true;
        }

        Bounds triggerBounds = triggerCollider.bounds;
        Bounds otherBounds = other.bounds;
        float minBlockingHeight = triggerBounds.center.y + Mathf.Max(0f, lowContactHeightPadding);
        return otherBounds.max.y >= minBlockingHeight;
    }

    private void ClearContacts()
    {
        for (int i = activeContacts.Count - 1; i >= 0; i--)
        {
            UnregisterContact(activeContacts[i]);
        }
    }

    private void EnsureTriggerSetup()
    {
        if (triggerCollider != null)
        {
            triggerCollider.isTrigger = true;
        }

        if (triggerRigidbody == null)
        {
            triggerRigidbody = GetComponent<Rigidbody>();
        }

        if (triggerRigidbody == null)
        {
            triggerRigidbody = gameObject.AddComponent<Rigidbody>();
        }

        triggerRigidbody.isKinematic = true;
        triggerRigidbody.useGravity = false;
    }
}
