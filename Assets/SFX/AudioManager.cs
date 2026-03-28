using UnityEngine;

public class AudioManager : MonoBehaviour
{
    // 单例模式：让其他脚本通过 AudioManager.Instance 访问
    public static AudioManager Instance { get; private set; }

    [Header("音频源")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource sfxSource;

    private void Awake()
    {
        // 确保场景中只有一个 AudioManager
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 切换场景时不销毁
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // --- BGM 控制 ---
    public void PlayBGM(AudioClip clip, float volume = 0.5f)
    {
        if (clip == null) return;
        
        bgmSource.clip = clip;
        bgmSource.volume = volume;
        bgmSource.loop = true;
        bgmSource.Play();
    }

    public void StopBGM() => bgmSource.Stop();

    // --- SFX 控制 (后续扩展) ---
    public void PlaySFX(AudioClip clip, float volume = 1.0f)
    {
        if (clip == null) return;
        sfxSource.PlayOneShot(clip, volume);
    }
}