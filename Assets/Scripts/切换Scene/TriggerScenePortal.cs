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
    [SerializeField, FormerlySerializedAs("objectToShow")]
    private GameObject promptPanel;


    public AudioSource audioSource;


    private bool playerInside;
    private bool isChangingScene;


    private void Start()
    {
        if (transitionController == null)
        {
            transitionController = FindObjectOfType<MainMenuTransitionController>();
        }

        if (transitionController == null)
        {
            Debug.LogWarning("没有找到 MainMenuTransitionController");
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
        // =========================
        // 开始播放转场
        // =========================

        if (transitionController != null)
        {
            transitionController.StartTransition();
        }


        // =========================
        // 等待转场黑屏
        // =========================

        yield return new WaitForSeconds(sceneLoadDelay);



        // =========================
        // 保存
        // =========================

        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.CaptureAndSave();
        }



        // =========================
        // 停止音频
        // =========================

        if (AudioSFXManager.Instance != null)
        {
            AudioSFXManager.Instance.StopAllAudioImmediately();
        }



        // =========================
        // 异步加载场景
        // =========================

        AsyncOperation asyncLoad =
            SceneManager.LoadSceneAsync(targetSceneIndex);


        // 等待场景加载完成

        while (!asyncLoad.isDone)
        {
            yield return null;
        }
    }




    private void SetPromptPanelVisible(bool visible)
    {
        ResolvePromptPanel();

        if(promptPanel != null)
        {
            promptPanel.SetActive(visible);
        }
    }



    private void ResolvePromptPanel()
    {
        if(promptPanel == null ||
           promptPanel.GetComponent<TMP_Text>() == null)
        {
            return;
        }


        Transform parent = promptPanel.transform.parent;


        if(parent != null)
        {
            promptPanel = parent.gameObject;
        }
    }
}