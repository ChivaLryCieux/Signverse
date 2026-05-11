using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TriggerScenePortal : MonoBehaviour
{
    [Header("切换目标 Scene Index")]
    public int targetSceneIndex = 1;

    [Header("进入 Trigger 时显示的物体")]
    public GameObject objectToShow;

    public AudioSource audioSource;

    private bool playerInside;

    // ------------------------
    // Update
    // ------------------------

    void Update()
    {
        if (!playerInside)
        {
            return;
        }

        // 按下 E 切换场景
        if (!CartoonPanelController.IsPlaying && Input.GetKeyDown(KeyCode.E))
        {
            ChangeScene();
        }
    }

    // ------------------------
    // 进入 Trigger
    // ------------------------

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
        {
            return;
        }

        playerInside = true;

        // 显示提示物体
        if (objectToShow != null)
        {
            objectToShow.SetActive(true);
        }
    }

    // ------------------------
    // 离开 Trigger
    // ------------------------

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player"))
        {
            return;
        }

        playerInside = false;

        // 隐藏提示物体
        if (objectToShow != null)
        {
            objectToShow.SetActive(false);
        }
    }

    // ------------------------
    // 切换场景
    // ------------------------

    private void ChangeScene()
    {
        // 停止所有 SFX
        if (AudioSFXManager.Instance != null)
        {
            AudioSFXManager.Instance.StopAllAudioImmediately();
        }
        objectToShow.gameObject.SetActive(false);

        SceneManager.LoadScene(targetSceneIndex);
    }
}
