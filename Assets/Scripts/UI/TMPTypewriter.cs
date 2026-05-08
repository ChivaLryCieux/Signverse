using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_Text))]
public class TMPTypewriterFade : MonoBehaviour
{
    [Header("打字速度")]
    [SerializeField] private float typingSpeed = 0.03f;

    [Header("淡入淡出速度")]
    [SerializeField] private float fadeSpeed = 6f;

    [Header("Enable时自动播放")]
    [SerializeField] private bool playOnEnable = true;

    [Header("Disable时是否播放打字机淡出")]
    [SerializeField] private bool useDisableFadeOut = true;

    private TMP_Text textComponent;

    private Coroutine typingCoroutine;

    private readonly List<FadingCharacter> fadingCharacters = new();

    private bool isPlayingFadeOut;

    private class FadingCharacter
    {
        public int charIndex;
        public float alpha;
        public bool fadeOut;
    }

    private void Awake()
    {
        textComponent = GetComponent<TMP_Text>();
    }

    private void OnEnable()
    {
        if (playOnEnable)
        {
            PlayTypewriter();
        }
    }

    private void Update()
    {
        UpdateFadingCharacters();
    }

    /// <summary>
    /// 开始打字机淡入
    /// </summary>
    public void PlayTypewriter()
    {
        isPlayingFadeOut = false;

        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }

        typingCoroutine = StartCoroutine(TypeTextIn());
    }

    /// <summary>
    /// 开始打字机淡出
    /// </summary>
    public void PlayFadeOut()
    {
        if (!gameObject.activeInHierarchy)
        {
            return;
        }

        isPlayingFadeOut = true;

        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }

        typingCoroutine = StartCoroutine(TypeTextOut());
    }

    /// <summary>
    /// 外部调用：播放淡出后关闭物体
    /// </summary>
    public void FadeOutAndDisable()
    {
        if (!useDisableFadeOut)
        {
            gameObject.SetActive(false);
            return;
        }

        PlayFadeOut();
    }

    private IEnumerator TypeTextIn()
    {
        textComponent.ForceMeshUpdate();

        TMP_TextInfo textInfo = textComponent.textInfo;

        int totalCharacters = textInfo.characterCount;

        fadingCharacters.Clear();

        // 全透明初始化
        for (int i = 0; i < totalCharacters; i++)
        {
            if (!textInfo.characterInfo[i].isVisible)
            {
                continue;
            }

            SetCharacterAlpha(i, 0);
        }

        textComponent.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);

        // 持续生成淡入字符
        for (int i = 0; i < totalCharacters; i++)
        {
            if (!textInfo.characterInfo[i].isVisible)
            {
                continue;
            }

            fadingCharacters.Add(new FadingCharacter()
            {
                charIndex = i,
                alpha = 0,
                fadeOut = false
            });

            yield return new WaitForSeconds(typingSpeed);
        }

        typingCoroutine = null;
    }

    private IEnumerator TypeTextOut()
    {
        textComponent.ForceMeshUpdate();

        TMP_TextInfo textInfo = textComponent.textInfo;

        int totalCharacters = textInfo.characterCount;

        fadingCharacters.Clear();

        // 从第一个字开始依次淡出
        for (int i = 0; i < totalCharacters; i++)
        {
            if (!textInfo.characterInfo[i].isVisible)
            {
                continue;
            }

            fadingCharacters.Add(new FadingCharacter()
            {
                charIndex = i,
                alpha = 255,
                fadeOut = true
            });

            yield return new WaitForSeconds(typingSpeed);
        }

        // 等待最后几个字淡完
        yield return new WaitForSeconds(1f / fadeSpeed + 0.05f);

        gameObject.SetActive(false);

        typingCoroutine = null;
    }

    private void UpdateFadingCharacters()
    {
        if (fadingCharacters.Count == 0)
        {
            return;
        }

        for (int i = fadingCharacters.Count - 1; i >= 0; i--)
        {
            FadingCharacter fadingChar = fadingCharacters[i];

            if (fadingChar.fadeOut)
            {
                fadingChar.alpha -= Time.deltaTime * fadeSpeed * 255f;
            }
            else
            {
                fadingChar.alpha += Time.deltaTime * fadeSpeed * 255f;
            }

            byte alphaByte = (byte)Mathf.Clamp(fadingChar.alpha, 0, 255);

            SetCharacterAlpha(fadingChar.charIndex, alphaByte);

            bool finished =
                (!fadingChar.fadeOut && fadingChar.alpha >= 255f) ||
                (fadingChar.fadeOut && fadingChar.alpha <= 0f);

            if (finished)
            {
                fadingCharacters.RemoveAt(i);
            }
        }

        textComponent.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
    }

    private void SetCharacterAlpha(int charIndex, byte alpha)
    {
        TMP_TextInfo textInfo = textComponent.textInfo;

        if (charIndex >= textInfo.characterCount)
        {
            return;
        }

        TMP_CharacterInfo charInfo = textInfo.characterInfo[charIndex];

        if (!charInfo.isVisible)
        {
            return;
        }

        int materialIndex = charInfo.materialReferenceIndex;
        int vertexIndex = charInfo.vertexIndex;

        Color32[] colors = textInfo.meshInfo[materialIndex].colors32;

        colors[vertexIndex + 0].a = alpha;
        colors[vertexIndex + 1].a = alpha;
        colors[vertexIndex + 2].a = alpha;
        colors[vertexIndex + 3].a = alpha;
    }
}