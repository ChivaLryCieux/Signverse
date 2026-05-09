using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Collider))]
public class FifthEquippedSlotPickupTrigger : MonoBehaviour
{
    [Header("第五装备槽解锁")]
    [SerializeField] private PickupUIController pickupUIController;
    [SerializeField] private bool destroyAfterPickup = true;

    [Header("提示")]
    [SerializeField] private BoltPickupTipController tipController;

    private bool playerInRange;
    private bool collected;
    private bool tipShown;
    private PlayerCC player;

    private void Reset()
    {
        ConfigureTriggerCollider();
    }

    private void OnValidate()
    {
        ConfigureTriggerCollider();
    }

    private void Awake()
    {
        ConfigureTriggerCollider();
        ResolveReferences();
    }

    private void OnDisable()
    {
        HideTip();
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
        ShowTip();
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
        HideTip();
    }

    private void TryCollect()
    {
        ResolveReferences();

        if (pickupUIController == null)
        {
            Debug.LogWarning("FifthEquippedSlotPickupTrigger 没有找到 PickupUIController，无法解锁第 5 个装备槽。", this);
            return;
        }

        collected = true;
        playerInRange = false;
        pickupUIController.UnlockFifthEquippedSlot();
        HideTip();

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

    private void ShowTip()
    {
        if (tipShown)
        {
            return;
        }

        ResolveReferences();
        if (tipController == null)
        {
            return;
        }

        tipShown = true;
        tipController.Show();
    }

    private void HideTip()
    {
        if (!tipShown)
        {
            return;
        }

        tipShown = false;
        if (tipController != null)
        {
            tipController.Hide();
        }
    }

    private void ResolveReferences()
    {
        if (pickupUIController == null)
        {
            pickupUIController = PickupUIController.Instance;
        }

        if (pickupUIController == null)
        {
            pickupUIController = FindObjectOfType<PickupUIController>();
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

    private void ConfigureTriggerCollider()
    {
        Collider triggerCollider = GetComponent<Collider>();
        if (triggerCollider != null)
        {
            triggerCollider.isTrigger = true;
        }
    }
}
