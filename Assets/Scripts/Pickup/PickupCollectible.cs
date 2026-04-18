using UnityEngine;

public class PickupCollectible : MonoBehaviour
{
    [Header("拾取物")]
    [SerializeField] private PickupItemId itemId;
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private bool destroyAfterPickup = true;

    [Header("可选：同步到 PlayerCC 技能")]
    [Tooltip("不为空时，拾取后会调用 PlayerCC.UnlockNewSkill(skillId)。")]
    [SerializeField] private string skillId;

    private bool collected;

    private void OnTriggerEnter(Collider other)
    {
        TryCollect(other.gameObject);
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        TryCollect(hit.gameObject);
    }

    private void TryCollect(GameObject other)
    {
        if (collected || other == null)
        {
            return;
        }

        PlayerCC player = other.GetComponentInParent<PlayerCC>();
        bool isPlayer = other.CompareTag(playerTag) || player != null;
        if (!isPlayer)
        {
            return;
        }

        collected = true;

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
}
