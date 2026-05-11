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

    [Header("消失物体")]
    [SerializeField] private GameObject objectToDisappear;  // 按E后要从画面中消失的物体

    [Header("音效")]
    [SerializeField] private AudioClip pickupSound;        // 拾取音效
    public AudioSource audioSource;      // 播放音效的源

    private bool playerInRange;
    private bool collected;
    private bool tipShown;
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
        // 如果没有手动指定 AudioSource，尝试从当前物体获取或添加
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            
        }
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
        ResolveReferences();

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

        HideTip();

        // 播放拾取音效
        PlayPickupSound();

        // 让指定的物体从画面中消失
        if (objectToDisappear != null)
        {
            objectToDisappear.SetActive(false);
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

    private void PlayPickupSound()
    {
        if (pickupSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(pickupSound);
        }
    }

    private bool WasInteractPressed()
    {
        return !CartoonPanelController.IsPlaying &&
               Keyboard.current != null &&
               Keyboard.current.eKey.wasPressedThisFrame;
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
