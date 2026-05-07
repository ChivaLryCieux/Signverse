using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InteractionPanelController : MonoBehaviour
{
    public static InteractionPanelController Instance { get; private set; }

    [Header("Panel")]
    [SerializeField] private GameObject panelRoot;

    [Header("Background")]
    [SerializeField] private Image backgroundImage;

    [Header("Content States")]
    [SerializeField] private GameObject fixedContent;

    [SerializeField] private GameObject detailContent;

    [Header("Detail Text")]
    [SerializeField] private TMP_Text detailText;

    [Header("Audio")]
    public CanvasAudio canvasAudio;

    private InteractionPanelTrigger activeTrigger;
    private bool isShowingDetail;

    public bool IsOpen => panelRoot != null && panelRoot.activeSelf;

    public bool IsShowingDetail => IsOpen && isShowingDetail;

    private void Reset()
    {
        ResolveReferences();
    }

    private void Awake()
    {
        ResolveReferences();

        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("场景中存在多个 InteractionPanelController，只会使用最先初始化的一个。", this);
        }
        else
        {
            Instance = this;
        }

        Hide();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    /// <summary>
    /// 显示固定提示状态
    /// </summary>
    public void ShowFixed(InteractionPanelTrigger trigger)
    {
        activeTrigger = trigger;
        isShowingDetail = false;

        if (backgroundImage != null)
        {
            backgroundImage.enabled = false;
        }

        SetFixedContentVisible(true);
        SetDetailContentVisible(false);

        if (panelRoot != null)
        {
            panelRoot.SetActive(true);
        }
    }

    /// <summary>
    /// 显示详细内容状态
    /// </summary>
    public void ShowDetail(InteractionPanelTrigger trigger, Sprite background, string bodyText)
    {
        activeTrigger = trigger;
        isShowingDetail = true;

        if (backgroundImage != null)
        {
            backgroundImage.sprite = background;
            backgroundImage.enabled = background != null;
        }

        SetFixedContentVisible(false);

        SetBodyText(bodyText);

        SetDetailContentVisible(true);

        if (panelRoot != null)
        {
            panelRoot.SetActive(true);

            if (canvasAudio != null)
            {
                canvasAudio.showNarrative();
            }
        }
    }

    public bool IsDetailOpenFor(InteractionPanelTrigger trigger)
    {
        return activeTrigger == trigger && IsShowingDetail;
    }

    public void HideFromTrigger(InteractionPanelTrigger trigger)
    {
        if (activeTrigger == trigger)
        {
            Hide();
        }
    }

    public void Hide()
    {
        activeTrigger = null;
        isShowingDetail = false;

        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }

        if (canvasAudio != null)
        {
            canvasAudio.closeNarrative();
        }
    }

    /// <summary>
    /// 设置正文文字
    /// </summary>
    private void SetBodyText(string bodyText)
    {
        if (detailText != null)
        {
            detailText.text = bodyText;
        }
    }

    /// <summary>
    /// 固定提示内容开关
    /// </summary>
    private void SetFixedContentVisible(bool visible)
    {
        if (fixedContent != null)
        {
            fixedContent.SetActive(visible);
        }
    }

    /// <summary>
    /// 详细内容开关
    /// </summary>
    private void SetDetailContentVisible(bool visible)
    {
        if (detailContent != null)
        {
            detailContent.SetActive(visible);
        }
    }

    /// <summary>
    /// 自动补引用
    /// </summary>
    private void ResolveReferences()
    {
        if (panelRoot == null)
        {
            panelRoot = gameObject;
        }

        if (backgroundImage == null)
        {
            backgroundImage = GetComponentInChildren<Image>(true);
        }
    }
}