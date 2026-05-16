using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

public class DoorPickup : MonoBehaviour
{
    private bool playerInside;
    private PlayerCC currentPlayer;

    [Header("进入 Trigger 时显示的提示 Panel")]
    [SerializeField, FormerlySerializedAs("objectToShow")] private GameObject promptPanel;

    void Update()
    {
        if (!playerInside || currentPlayer == null)
        {
            return;
        }

        if (!CartoonPanelController.IsPlaying && Input.GetKeyDown(KeyCode.E))
        {
            currentPlayer.hasDoorPickup = true;
            Debug.Log("picked door");

            gameObject.SetActive(false);

            SetPromptPanelVisible(false);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerCC player = other.GetComponent<PlayerCC>();

        if (player == null)
            return;

        playerInside = true;
        currentPlayer = player;

        SetPromptPanelVisible(true);
    }

    private void OnTriggerExit(Collider other)
    {
        PlayerCC player = other.GetComponent<PlayerCC>();

        if (player == null)
            return;

        playerInside = false;
        currentPlayer = null;

        SetPromptPanelVisible(false);
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
