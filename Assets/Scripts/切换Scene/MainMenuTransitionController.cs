using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuTransitionController : MonoBehaviour
{
    public static MainMenuTransitionController Instance;

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
    [SerializeField] private float fadeOutDelay = 1f;

    [SerializeField] private float fadeOutDuration = 1.5f;

    [Header("音效")]
    [SerializeField] private AudioSource audioSource;

    [SerializeField] private List<AudioClip> transitionSFX = new List<AudioClip>();


    private bool isTransitioning;
    private Canvas transitionCanvas;



    private void Awake()
    {
        // =========================
        // 单例 + 保留跨场景
        // =========================

        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }


        Instance = this;

        DontDestroyOnLoad(gameObject);



        // =========================
        // Canvas排序
        // =========================

        transitionCanvas = GetComponent<Canvas>();

        if (transitionCanvas != null)
        {
            transitionCanvas.overrideSorting = true;
            transitionCanvas.sortingOrder = 999;
        }



        // =========================
        // 自动寻找组件
        // =========================

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



        // 播放音效

        if (audioSource != null)
        {
            foreach(AudioClip clip in transitionSFX)
            {
                if(clip != null)
                {
                    audioSource.PlayOneShot(clip);
                }
            }
        }



        if(fadeImage != null)
        {
            fadeImage.gameObject.SetActive(true);
        }



        Color transparentStart = firstColor;
        transparentStart.a = 0f;


        fadeImage.color = transparentStart;



        // =========================
        // 透明 -> 第一颜色
        // =========================

        float timer = 0f;


        while(timer < fadeDuration)
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
        // 第一颜色 -> 黑色
        // =========================

        timer = 0f;


        while(timer < fadeDuration)
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
        // 黑屏保持
        // =========================

        yield return new WaitForSeconds(fadeOutDelay);



        // =========================
        // 黑色 -> 透明
        // =========================

        timer = 0f;


        Color finalTransparent = secondColor;
        finalTransparent.a = 0f;


        while(timer < fadeOutDuration)
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



        fadeImage.gameObject.SetActive(false);


        isTransitioning = false;
    }
}