using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class UIImageFadeIn : MonoBehaviour
{
    [Header("淡入时长")]
    [SerializeField] private float fadeDuration = 1f;

    [Header("启用时自动淡入")]
    [SerializeField] private bool fadeOnEnable = true;

    private Image image;

    private Coroutine fadeCoroutine;

    // 记录Inspector里原本设置的透明度
    private float targetAlpha;

    private void Awake()
    {
        image = GetComponent<Image>();

        // 获取当前Image原本的透明度
        targetAlpha = image.color.a;
    }

    private void OnEnable()
    {
        if (fadeOnEnable)
        {
            PlayFadeIn();
        }
    }

    /// <summary>
    /// 开始淡入
    /// </summary>
    public void PlayFadeIn()
    {
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }

        fadeCoroutine = StartCoroutine(FadeInCoroutine());
    }

    private IEnumerator FadeInCoroutine()
    {
        Color color = image.color;

        // 从透明开始
        color.a = 0f;

        image.color = color;

        float timer = 0f;

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;

            // 渐变到原本设置的透明度
            float alpha = Mathf.Lerp(0f, targetAlpha, timer / fadeDuration);

            color.a = alpha;

            image.color = color;

            yield return null;
        }

        // 确保最终值准确
        color.a = targetAlpha;

        image.color = color;

        fadeCoroutine = null;
    }
}