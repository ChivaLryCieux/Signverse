using System.Collections.Generic;
using UnityEngine;

public class PlayerSFX : MonoBehaviour
{
    [Header("脚步音列表")]
    public List<AudioClip> footstepClips = new List<AudioClip>();

    [Header("AudioSource")]
    public AudioSource audioSource;

    [Header("随机范围控制（可选）")]
    public float minPitch = 0.95f;
    public float maxPitch = 1.05f;

    void Awake()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
    }

    // =========================
    // 脚步声播放（核心方法）
    // =========================
    public void PlayFootstep()
    {
        if (footstepClips == null || footstepClips.Count == 0)
        {
            return;
        }

        if (audioSource == null)
        {
            return;
        }

        int index = Random.Range(0, footstepClips.Count);
        AudioClip clip = footstepClips[index];

        if (clip == null)
        {
            return;
        }

        // 可选：轻微随机音高，避免机械重复
        audioSource.pitch = Random.Range(minPitch, maxPitch);

        audioSource.PlayOneShot(clip);
    }
}