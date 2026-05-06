using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Collider))]
public class BoltPickupTrigger : MonoBehaviour
{
    [Header("Bolt 解锁")]
    [SerializeField] private BoltPanelController boltPanel;
    [SerializeField] private bool destroyAfterPickup = true;

    [Header("提示")]
    [SerializeField] private BoltPickupTipController tipController;

    private bool playerInRange;
    private bool collected;
    private PlayerCC player;

    private void Reset()
    {
        Collider triggerCollider = GetComponent<Collider>();
        triggerCollider.isTrigger = true;
    }

    private void OnValidate()
    {
        Collider triggerCollider = GetComponent<Collider>();
        if (triggerCollider != null)
        {
            triggerCollider.isTrigger = true;
        }
    }

    private void Awake()
    {
        ResolveReferences();
    }

    private void Update()
    {
        if (!playerInRange || collected || !WasInteractPressed())
        {
            return;
        }

        TryCollect();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (collected)
        {
            return;
        }

        PlayerCC enteringPlayer = other.GetComponentInParent<PlayerCC>();
        if (enteringPlayer == null)
        {
            return;
        }

        player = enteringPlayer;
        playerInRange = true;
        ResolveReferences();

        if (tipController != null)
        {
            tipController.Show();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        PlayerCC exitingPlayer = other.GetComponentInParent<PlayerCC>();
        if (exitingPlayer == null || exitingPlayer != player)
        {
            return;
        }

        playerInRange = false;
        player = null;

        if (tipController != null)
        {
            tipController.Hide();
        }
    }

    private void TryCollect()
    {
        ResolveReferences();

        if (boltPanel == null)
        {
            Debug.LogWarning("BoltPickupTrigger 没有找到 BoltPanelController，无法解锁 bolt。", this);
            return;
        }

        if (!boltPanel.TryUnlockNextBolt())
        {
            return;
        }

        collected = true;
        playerInRange = false;

        if (tipController != null)
        {
            tipController.Hide();
        }

        if (destroyAfterPickup)
        {
            Destroy(gameObject);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    private bool WasInteractPressed()
    {
        return Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;
    }

    private void ResolveReferences()
    {
        if (boltPanel == null)
        {
            boltPanel = BoltPanelController.Instance;
        }

        if (boltPanel == null)
        {
            boltPanel = FindObjectOfType<BoltPanelController>();
        }

        if (tipController == null)
        {
            tipController = BoltPickupTipController.Instance;
        }

        if (tipController == null)
        {
            tipController = FindObjectOfType<BoltPickupTipController>();
        }
    }
}
