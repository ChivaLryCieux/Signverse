using UnityEngine;

public class TriggerPlaySFX : MonoBehaviour
{
    [Header("播放音频")]
    [SerializeField] private AudioClip bgmClip;

    [Range(0f, 1f)]
    [SerializeField] private float volume = 1f;

    [Header("播放模式")]
    [SerializeField] private bool playAsLoop = false;

    [Header("设置")]
    [SerializeField] private bool triggerOnlyOnce = true;

    private bool hasPlayed;

    private void OnTriggerEnter(Collider other)
    {
        if (triggerOnlyOnce && hasPlayed)
        {
            return;
        }

        // 判断是否玩家
        PlayerCC player = other.GetComponentInParent<PlayerCC>();
        Debug.Log("enteredBGMTrigger");
        if (player == null)
        {
            return;
        }
        Debug.Log("playBGM");
        // 根据 Inspector 选择播放模式
        if (playAsLoop)
        {
            AudioSFXManager.Instance.PlayLoop(bgmClip, volume);
        }
        else
        {
            AudioSFXManager.Instance.PlaySFX(bgmClip, volume);
        }

        hasPlayed = true;
    }
}