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

    [Header("音效")]
    public AudioSource audioSource;

    public List<AudioClip> startGameSFX = new List<AudioClip>();

    private bool isPlayingSFX = false;

    private bool isChangingScene = false;

    private Camera mainCamera;

    void Awake()
    {
        mainCamera = Camera.main;

        if (AudioSFXManager.Instance != null)
        {
            AudioSFXManager.Instance.StopAllAudioImmediately();
        }

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
    }

    void Update()
    {
        // Q退出
        if (Keyboard.current != null &&
            Keyboard.current.qKey.wasPressedThisFrame)
        {
            Application.Quit();

            Debug.Log("已退出游戏！");
        }

        // 鼠标左键点击
        if (Mouse.current != null &&
            Mouse.current.leftButton.wasPressedThisFrame)
        {
            DetectClick();
        }
    }

    void DetectClick()
    {
        if (isChangingScene)
        {
            return;
        }

        Ray ray =
            mainCamera.ScreenPointToRay(
                Mouse.current.position.ReadValue()
            );

        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            Debug.Log("Clicked : " + hit.collider.name);

            // 判断是不是点到了自己
            if (hit.collider.gameObject == gameObject)
            {
                Debug.Log("Portal Clicked!");

                ChangeScene();
            }
        }
    }

    public void ChangeScene()
    {
        StartCoroutine(ChangeSceneCoroutine());
    }

    IEnumerator ChangeSceneCoroutine()
    {
        isChangingScene = true;

        if (!isPlayingSFX)
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

        Color startColor = firstColor;

        startColor.a = 0f;

        fadeImage.color = startColor;

        // 第一阶段
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

        // 第二阶段
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

        SceneManager.LoadScene(targetSceneIndex);
    }
}