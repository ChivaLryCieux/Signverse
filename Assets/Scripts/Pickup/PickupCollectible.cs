using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Collider))]
public class PickupCollectible : MonoBehaviour
{
    [Header("拾取物")]
    [SerializeField] private PickupItemId itemId;
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private bool destroyAfterPickup = true;

    [Header("提示")]
    [SerializeField] private GameObject skillPrompt;
    [SerializeField] private string skillPromptName = "SkillPrompt";

    [Header("可选：同步到 PlayerCC 技能")]
    [Tooltip("不为空时，拾取后会调用 PlayerCC.UnlockNewSkill(skillId)。")]
    [SerializeField] private string skillId;

    private bool playerInRange;
    private bool collected;
    private bool promptShown;
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
        ResolvePrompt();
        if (skillPrompt != null)
        {
            skillPrompt.SetActive(false);
        }
    }

    private void OnDisable()
    {
        HidePrompt();
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
        bool isPlayer = other.CompareTag(playerTag) || enteringPlayer != null;
        if (!isPlayer)
        {
            return;
        }

        player = enteringPlayer;
        playerInRange = true;
        ShowPrompt();
    }

    private void OnTriggerExit(Collider other)
    {
        PlayerCC exitingPlayer = other.GetComponentInParent<PlayerCC>();
        bool isPlayer = other.CompareTag(playerTag) || exitingPlayer != null;
        if (!isPlayer || (player != null && exitingPlayer != null && exitingPlayer != player))
        {
            return;
        }

        playerInRange = false;
        player = null;
        HidePrompt();
    }

    private void TryCollect()
    {
        if (collected)
        {
            return;
        }

        collected = true;
        playerInRange = false;
        HidePrompt();

        PickupUIController uiController = PickupUIController.Instance;
        if (uiController == null)
        {
            uiController = FindObjectOfType<PickupUIController>();
        }

        if (uiController != null)
        {
            uiController.Unlock(itemId);
        }
        else
        {
            Debug.LogWarning($"场景中没有 PickupUIController，无法显示拾取物 {itemId} 的 UI。", this);
        }

        if (player != null && !string.IsNullOrWhiteSpace(skillId))
        {
            player.UnlockNewSkill(skillId);
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

    private void ShowPrompt()
    {
        if (promptShown)
        {
            return;
        }

        ResolvePrompt();
        if (skillPrompt == null)
        {
            return;
        }

        promptShown = true;
        skillPrompt.SetActive(true);
    }

    private void HidePrompt()
    {
        if (!promptShown)
        {
            return;
        }

        promptShown = false;
        if (skillPrompt != null)
        {
            skillPrompt.SetActive(false);
        }
    }

    private void ResolvePrompt()
    {
        if (skillPrompt != null || string.IsNullOrWhiteSpace(skillPromptName))
        {
            return;
        }

        skillPrompt = GameObject.Find(skillPromptName);
        if (skillPrompt != null)
        {
            return;
        }

        GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
        for (int i = 0; i < allObjects.Length; i++)
        {
            GameObject candidate = allObjects[i];
            if (candidate != null &&
                candidate.name == skillPromptName &&
                candidate.scene.IsValid() &&
                candidate.scene.isLoaded)
            {
                skillPrompt = candidate;
                return;
            }
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
