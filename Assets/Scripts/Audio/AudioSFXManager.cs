using System.Collections;
using UnityEngine;

public class AudioSFXManager : MonoBehaviour
{
    public static AudioSFXManager Instance;

    [Header("Audio Source")]
    [SerializeField] private AudioSource sfxSource;

    private Coroutine fadeCoroutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
        }

        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// 播放一次性音效
    /// </summary>
    public void PlaySFX(AudioClip clip, float volume = 1f)
    {
        if (clip == null)
        {
            return;
        }

        sfxSource.PlayOneShot(clip, volume);
    }

    /// <summary>
    /// 播放循环音效（用于后续渐隐）
    /// </summary>
    public void PlayLoop(AudioClip clip, float volume = 1f)
    {
        if (clip == null)
        {
            return;
        }

        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }

        sfxSource.clip = clip;
        sfxSource.volume = volume;
        sfxSource.loop = true;
        sfxSource.Play();
    }

    /// <summary>
    /// 渐隐并停止当前播放
    /// </summary>
    public void FadeOutAndStop(float fadeDuration = 1f)
    {
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }

        fadeCoroutine = StartCoroutine(FadeOutCoroutine(fadeDuration));
    }

    private IEnumerator FadeOutCoroutine(float duration)
    {
        float startVolume = sfxSource.volume;

        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;

            sfxSource.volume = Mathf.Lerp(startVolume, 0f, timer / duration);

            yield return null;
        }

        sfxSource.volume = 0f;

        sfxSource.Stop();

        sfxSource.clip = null;

        sfxSource.loop = false;

        sfxSource.volume = startVolume;

        fadeCoroutine = null;
    }
}