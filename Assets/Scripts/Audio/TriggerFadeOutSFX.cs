using UnityEngine;

public class TriggerFadeOutSFX : MonoBehaviour
{
    [Header("渐隐时间")]
    [SerializeField] private float fadeDuration = 1f;

    [Header("设置")]
    [SerializeField] private bool triggerOnlyOnce = true;

    private bool hasTriggered;

    private void OnTriggerEnter(Collider other)
    {
        if (triggerOnlyOnce && hasTriggered)
        {
            return;
        }

        // 检测玩家
        PlayerCC player = other.GetComponentInParent<PlayerCC>();

        if (player == null)
        {
            return;
        }

        AudioSFXManager.Instance.FadeOutAndStop(fadeDuration);

        hasTriggered = true;
    }
}