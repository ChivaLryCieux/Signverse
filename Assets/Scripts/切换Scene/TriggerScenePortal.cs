using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

public class TriggerScenePortal : MonoBehaviour
{
    [Header("切换目标 Scene Index")]
    public int targetSceneIndex = 1;


    [Header("转场")]
    [Tooltip("引用全局转场Canvas控制器")]
    [SerializeField] private MainMenuTransitionController transitionController;

    [Tooltip("转场完成后等待时间")]
    [SerializeField] private float sceneLoadDelay = 0.5f;



    [Header("进入 Trigger 时显示的提示 Panel")]
    [Tooltip("拖入完整Panel物体，不要拖TMP文字")]
    [SerializeField, FormerlySerializedAs("objectToShow")]
    private GameObject promptPanel;



    [Header("音效")]
    [SerializeField] private AudioSource audioSource;



    private bool playerInside;
    private bool isChangingScene;



    private void Start()
    {
        if (transitionController == null)
        {
            transitionController =
                FindObjectOfType<MainMenuTransitionController>();
        }


        if (transitionController == null)
        {
            Debug.LogWarning(
                "没有找到 MainMenuTransitionController"
            );
        }


        if(audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }


        if(promptPanel != null)
        {
            promptPanel.SetActive(false);
        }
    }



    private void Update()
    {
        if (!playerInside || isChangingScene)
        {
            return;
        }


        if ((Keyboard.current != null &&
             Keyboard.current.eKey.wasPressedThisFrame)
             ||
             MobileInputManager.ConsumeInteractPressed())
        {
            ChangeScene();
        }
    }



    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
        {
            return;
        }


        playerInside = true;


        SetPromptPanelVisible(true);
    }



    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player"))
        {
            return;
        }


        playerInside = false;


        SetPromptPanelVisible(false);
    }



    private void ChangeScene()
    {
        if(isChangingScene)
        {
            return;
        }


        isChangingScene = true;


        SetPromptPanelVisible(false);


        StartCoroutine(ChangeSceneCoroutine());
    }



    private IEnumerator ChangeSceneCoroutine()
    {
        // 播放转场

        if(transitionController != null)
        {
            transitionController.StartTransition();
        }



        // 播放传送音效（如果有）

        if(audioSource != null)
        {
            audioSource.Play();
        }



        yield return new WaitForSeconds(
            sceneLoadDelay
        );



        // 保存

        if(SaveManager.Instance != null)
        {
            SaveManager.Instance.CaptureAndSave();
        }



        // 停止全局SFX

        if(AudioSFXManager.Instance != null)
        {
            AudioSFXManager.Instance.StopAllAudioImmediately();
        }



        // 异步加载场景

        AsyncOperation asyncLoad =
            SceneManager.LoadSceneAsync(
                targetSceneIndex
            );


        while(!asyncLoad.isDone)
        {
            yield return null;
        }
    }



    private void SetPromptPanelVisible(bool visible)
    {
        if(promptPanel == null)
        {
            Debug.LogWarning(
                "Prompt Panel没有绑定"
            );

            return;
        }


        promptPanel.SetActive(visible);
    }
}