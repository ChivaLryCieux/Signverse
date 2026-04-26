using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InteractionPanelController : MonoBehaviour
{
    public static InteractionPanelController Instance { get; private set; }

    [Header("Panel")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private TMP_Text fixedText;
    [SerializeField] private TMP_Text tmpBodyText;
    [SerializeField] private Text legacyBodyText;

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

    public void ShowFixed(InteractionPanelTrigger trigger)
    {
        activeTrigger = trigger;
        isShowingDetail = false;

        if (backgroundImage != null)
        {
            backgroundImage.enabled = false;
        }

        SetFixedTextVisible(true);
        SetBodyTextVisible(false);

        if (panelRoot != null)
        {
            panelRoot.SetActive(true);
        }
    }

    public void ShowDetail(InteractionPanelTrigger trigger, Sprite background, string bodyText)
    {
        activeTrigger = trigger;
        isShowingDetail = true;

        if (backgroundImage != null)
        {
            backgroundImage.sprite = background;
            backgroundImage.enabled = background != null;
        }

        SetFixedTextVisible(false);
        SetBodyText(bodyText);
        SetBodyTextVisible(true);

        if (panelRoot != null)
        {
            panelRoot.SetActive(true);
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
    }

    private void SetBodyText(string bodyText)
    {
        if (tmpBodyText != null)
        {
            tmpBodyText.text = bodyText;
        }

        if (legacyBodyText != null)
        {
            legacyBodyText.text = bodyText;
        }
    }

    private void SetFixedTextVisible(bool visible)
    {
        if (fixedText != null)
        {
            fixedText.gameObject.SetActive(visible);
        }
    }

    private void SetBodyTextVisible(bool visible)
    {
        if (tmpBodyText != null)
        {
            tmpBodyText.gameObject.SetActive(visible);
        }

        if (legacyBodyText != null)
        {
            legacyBodyText.gameObject.SetActive(visible);
        }
    }

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

        if (tmpBodyText == null)
        {
            TMP_Text[] tmpTexts = GetComponentsInChildren<TMP_Text>(true);
            if (tmpTexts.Length > 0)
            {
                tmpBodyText = tmpTexts[0];
            }

            if (fixedText == null && tmpTexts.Length > 1)
            {
                fixedText = tmpTexts[1];
            }
        }

        if (legacyBodyText == null)
        {
            Text[] legacyTexts = GetComponentsInChildren<Text>(true);
            if (legacyTexts.Length > 0)
            {
                legacyBodyText = legacyTexts[0];
            }
        }
    }
}
