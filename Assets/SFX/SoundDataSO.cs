using UnityEngine;

[CreateAssetMenu(fileName = "NewSound", menuName = "Audio/SoundData")]
public class SoundDataSO : ScriptableObject
{
    public AudioClip clip;
    [Range(0, 1)] public float volume = 0.5f;
    [Range(0.5f, 1.5f)] public float pitch = 1.0f; // 随机音高可以让音效更自然

    public void Play()
    {
        // 直接调用单例播放
        AudioManager.Instance.PlaySFX(clip, volume);
    }
}