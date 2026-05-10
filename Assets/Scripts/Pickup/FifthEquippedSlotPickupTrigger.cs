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

    [Header("按E捡起后显示的对象")]
    public GameObject gameObjectToShowOnPickup;  // 公开，可在Inspector中拖拽

    [Header("按E捡起后隐藏的对象")]
    public GameObject gameObjectToHideOnPickup;  // 新增：捡起后要隐藏的对象

    [Header("按E捡起时的音效")]
    [SerializeField] private AudioClip pickupSound;  // 音效片段
    [SerializeField] private AudioSource audioSource; // 播放音效的AudioSource

    private bool playerInRange;
    private bool collected;
    private bool tipShown;
    private PlayerCC player;

    private void Reset()
    {
        ConfigureTriggerCollider();
        TryGetAudioSource();
    }

    private void OnValidate()
    {
        ConfigureTriggerCollider();
        TryGetAudioSource();
    }

    private void Awake()
    {
        ConfigureTriggerCollider();
        ResolveReferences();
        TryGetAudioSource();
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

        // 显示指定对象
        if (gameObjectToShowOnPickup != null)
        {
            gameObjectToShowOnPickup.SetActive(true);
        }
        else
        {
            Debug.LogWarning("在 Inspector 中没有为 gameObjectToShowOnPickup 指定要显示的物体。", this);
        }

        // 隐藏指定对象
        if (gameObjectToHideOnPickup != null)
        {
            gameObjectToHideOnPickup.SetActive(false);
        }
        else
        {
            Debug.LogWarning("在 Inspector 中没有为 gameObjectToHideOnPickup 指定要隐藏的物体。", this);
        }

        // 播放音效
        PlayPickupSound();

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
        // 播放音效逻辑
        if (pickupSound != null)
        {
            // 如果有指定 AudioSource，用它的位置播放
            if (audioSource != null)
            {
                audioSource.PlayOneShot(pickupSound);
            }
            else
            {
                // 如果没有指定 AudioSource，在物体位置创建一个临时 AudioSource 播放
                AudioSource.PlayClipAtPoint(pickupSound, transform.position);
            }
        }
        else
        {
            Debug.LogWarning("没有为 pickupSound 指定音效片段。", this);
        }
    }

    private void TryGetAudioSource()
    {
        // 尝试获取同物体上的 AudioSource
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
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