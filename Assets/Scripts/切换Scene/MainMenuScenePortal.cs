using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuScenePortal : MonoBehaviour
{
    [Header("目标场景")]
    public int targetSceneIndex = 1;

    [Header("渐变时间")]
    public float fadeDuration = 1.5f;

    [Header("第一阶段颜色")]
    public Color firstColor = Color.red;

    [Header("第二阶段颜色")]
    public Color secondColor = Color.black;

    [Header("全屏遮罩 Image")]
    public Image fadeImage;

    [Header("全屏遮罩 Image")]
    public AudioSource audioSource;
    public List<AudioClip> startGameSFX = new List<AudioClip>();

    private bool isPlayingSFX = false;
    private bool isChangingScene;

    // ------------------------
    // 点击物体
    // ------------------------

    void Awake()
{
    // 停止全局 SFX
    if (AudioSFXManager.Instance != null)
    {
        AudioSFXManager.Instance.StopAllAudioImmediately();
    }

    if(audioSource == null)
    {
        audioSource = GetComponent<AudioSource>();
    }
}
    void Update()
    {
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            Application.Quit();
            Debug.Log("已退出游戏！");
        }
    }
    private void OnMouseDown()
    {
        if (isChangingScene)
            return;

        ChangeScene();
    }

    // ------------------------
    // 切换场景
    // ------------------------

    public void ChangeScene()
    {
        StartCoroutine(ChangeSceneCoroutine());
    }

    // ------------------------
    // 场景切换协程
    // ------------------------

    IEnumerator ChangeSceneCoroutine()
    {
        
        isChangingScene = true;
        if(!isPlayingSFX)
        {
            foreach (AudioClip clip in startGameSFX)
{
            if (clip != null)
            {
                audioSource.PlayOneShot(clip);
            }
}
            isPlayingSFX = true;
        }

        fadeImage.gameObject.SetActive(true);

        // 初始透明
        Color startColor = firstColor;
        startColor.a = 0f;

        fadeImage.color = startColor;

        // ========================
        // 第一阶段：
        // 透明 -> 红色
        // ========================

        float timer = 0f;

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;

            float t = timer / fadeDuration;

            fadeImage.color =
                Color.Lerp(
                    startColor,
                    firstColor,
                    t
                );

            yield return null;
        }

        fadeImage.color = firstColor;

        // ========================
        // 第二阶段：
        // 红色 -> 黑色
        // ========================

        timer = 0f;

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;

            float t = timer / fadeDuration;

            fadeImage.color =
                Color.Lerp(
                    firstColor,
                    secondColor,
                    t
                );

            yield return null;
        }

        fadeImage.color = secondColor;

        // ========================
        // 切换场景
        // ========================

        SceneManager.LoadScene(targetSceneIndex);
    }
}