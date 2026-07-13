using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuTransitionController : MonoBehaviour
{
    [Header("绑定存档面板事件")]
    [SerializeField] private SavePanelController savePanelController;


    [Header("全屏遮罩 Image")]
    [SerializeField] private Image fadeImage;


    [Header("颜色渐变时间")]
    [SerializeField] private float fadeDuration = 1.5f;


    [Header("第一阶段颜色")]
    [SerializeField] private Color firstColor = Color.red;


    [Header("第二阶段颜色")]
    [SerializeField] private Color secondColor = Color.black;


    [Header("结束淡出")]
    [Tooltip("黑屏保持时间")]
    [SerializeField] private float fadeOutDelay = 1f;

    [Tooltip("黑屏淡出到透明的时间")]
    [SerializeField] private float fadeOutDuration = 1.5f;


    [Header("音效")]
    [SerializeField] private AudioSource audioSource;

    [SerializeField] private List<AudioClip> transitionSFX = new List<AudioClip>();


    private bool isTransitioning = false;
    private Canvas transitionCanvas;



    private void Awake()
    {
        DontDestroyOnLoad(gameObject);

        // 确保 Canvas 切换场景后排序在最上层
        if(transitionCanvas == null)
        {
            transitionCanvas = GetComponent<Canvas>();
        }
        transitionCanvas.overrideSorting = true;
        transitionCanvas.sortingOrder = 999;

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }


        if (fadeImage == null)
        {
            fadeImage = GetComponentInChildren<Image>();
        }
    }



    private void OnEnable()
    {
        if (savePanelController != null)
        {
            savePanelController.OnNewGameEvent
                .AddListener(StartTransition);


            savePanelController.OnContinueGameEvent
                .AddListener(StartTransition);
        }
    }



    private void OnDisable()
    {
        if (savePanelController != null)
        {
            savePanelController.OnNewGameEvent
                .RemoveListener(StartTransition);


            savePanelController.OnContinueGameEvent
                .RemoveListener(StartTransition);
        }
    }



    /// <summary>
    /// 外部调用的转场入口
    /// </summary>
    public void StartTransition()
    {
        if (isTransitioning)
        {
            return;
        }


        StartCoroutine(TransitionCoroutine());
    }



    private IEnumerator TransitionCoroutine()
    {
        isTransitioning = true;


        // 播放转场音效
        if (audioSource != null)
        {
            foreach (AudioClip clip in transitionSFX)
            {
                if (clip != null)
                {
                    audioSource.PlayOneShot(clip);
                }
            }
        }



        // 激活遮罩
        if (fadeImage != null)
        {
            fadeImage.gameObject.SetActive(true);
        }



        // 初始透明颜色
        Color transparentStart = firstColor;
        transparentStart.a = 0f;


        fadeImage.color = transparentStart;



        // =========================
        // 第一阶段：透明 → firstColor
        // =========================

        float timer = 0f;


        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;


            float t = timer / fadeDuration;


            fadeImage.color =
                Color.Lerp(
                    transparentStart,
                    firstColor,
                    t
                );


            yield return null;
        }


        fadeImage.color = firstColor;



        // =========================
        // 第二阶段：firstColor → secondColor
        // =========================

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



        // =========================
        // 黑屏停留
        // =========================

        yield return new WaitForSeconds(fadeOutDelay);



        // =========================
        // 第三阶段：secondColor → 透明
        // =========================

        timer = 0f;


        Color finalTransparent = secondColor;
        finalTransparent.a = 0f;


        while (timer < fadeOutDuration)
        {
            timer += Time.deltaTime;


            float t = timer / fadeOutDuration;


            fadeImage.color =
                Color.Lerp(
                    secondColor,
                    finalTransparent,
                    t
                );


            yield return null;
        }


        fadeImage.color = finalTransparent;



        // 隐藏Image
        fadeImage.gameObject.SetActive(false);


        isTransitioning = false;
    }
}