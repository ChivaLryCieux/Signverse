using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

public class TriggerScenePortal : MonoBehaviour
{
    [Header("切换目标 Scene Index")]
    public int targetSceneIndex = 1;

    [Header("进入 Trigger 时显示的提示 Panel")]
    [SerializeField, FormerlySerializedAs("objectToShow")] private GameObject promptPanel;

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

        SetPromptPanelVisible(true);
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

        SetPromptPanelVisible(false);
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
        SetPromptPanelVisible(false);

        SceneManager.LoadScene(targetSceneIndex);
    }

    private void SetPromptPanelVisible(bool visible)
    {
        ResolvePromptPanel();
        if (promptPanel != null)
        {
            promptPanel.SetActive(visible);
        }
    }

    private void ResolvePromptPanel()
    {
        if (promptPanel == null || promptPanel.GetComponent<TMP_Text>() == null)
        {
            return;
        }

        Transform parent = promptPanel.transform.parent;
        if (parent != null)
        {
            promptPanel = parent.gameObject;
        }
    }
}
