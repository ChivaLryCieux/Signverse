using TMPro;
using UnityEngine;

public class BoltPickupTipController : MonoBehaviour
{
    public static BoltPickupTipController Instance { get; private set; }

    [SerializeField] private GameObject promptPanel;
    [SerializeField] private TMP_Text promptText;

    private int showRequestCount;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("场景中存在多个 BoltPickupTipController，只会使用最先初始化的一个。", this);
        }
        else
        {
            Instance = this;
        }

        HideImmediate();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void Show()
    {
        showRequestCount++;
        SetVisible(true);
    }

    public void Hide()
    {
        showRequestCount = Mathf.Max(0, showRequestCount - 1);
        if (showRequestCount == 0)
        {
            SetVisible(false);
        }
    }

    public void HideImmediate()
    {
        showRequestCount = 0;
        SetVisible(false);
    }

    private void SetVisible(bool visible)
    {
        ResolvePromptPanel();

        if (promptPanel != null)
        {
            promptPanel.SetActive(visible);
        }
    }

    private void ResolvePromptPanel()
    {
        if (promptPanel != null || promptText == null)
        {
            return;
        }

        Transform parent = promptText.transform.parent;
        promptPanel = parent != null && parent != transform ? parent.gameObject : promptText.gameObject;
    }
}
