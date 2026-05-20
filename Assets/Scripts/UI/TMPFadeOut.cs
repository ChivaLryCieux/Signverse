using System.Collections;
using TMPro;
using UnityEngine;

public class TMPFadeOut : MonoBehaviour
{
    [Header("TMP")]
    public TMP_Text targetText;

    [Header("开始渐隐前等待时间")]
    public float startOffset = 1f;

    [Header("渐隐持续时间")]
    public float fadeDuration = 1f;

    private Coroutine fadeCoroutine;

    void OnEnable()
    {
        if (targetText == null)
        {
            targetText = GetComponent<TMP_Text>();
        }

        if (targetText == null)
        {
            Debug.LogWarning("TMP_Text not found!");
            return;
        }

        // 防止重复协程
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }

        // 重置透明度
        Color color = targetText.color;
        color.a = 1f;
        targetText.color = color;

        fadeCoroutine = StartCoroutine(FadeOutCoroutine());
    }

    IEnumerator FadeOutCoroutine()
    {
        // 开始前等待
        yield return new WaitForSeconds(startOffset);

        float timer = 0f;

        Color startColor = targetText.color;

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;

            float t = timer / fadeDuration;

            Color newColor = startColor;

            newColor.a = Mathf.Lerp(1f, 0f, t);

            targetText.color = newColor;

            yield return null;
        }

        Color finalColor = targetText.color;
        finalColor.a = 0f;
        targetText.color = finalColor;

        fadeCoroutine = null;
    }
}