using UnityEngine;

public class PausePanelController : MonoBehaviour
{
    private static PausePanelController activePanel;

    [Header("Panel")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private bool hideOnStart = true;

    [Header("Pause")]
    [SerializeField] private bool pauseGameWhenOpen;
    [SerializeField] private bool controlCursor = true;

    private float previousTimeScale = 1f;
    private CanvasGroup panelCanvasGroup;

    private bool UsesCanvasGroupVisibility => panelRoot == gameObject;

    public static bool IsPaused => activePanel != null && activePanel.IsOpen;

    public bool IsOpen
    {
        get
        {
            if (panelRoot == null)
            {
                return false;
            }

            if (UsesCanvasGroupVisibility)
            {
                return panelCanvasGroup == null || panelCanvasGroup.alpha > 0f;
            }

            return panelRoot.activeSelf;
        }
    }

    private void Reset()
    {
        panelRoot = gameObject;
    }

    private void Awake()
    {
        ResolveReferences();

        if (hideOnStart)
        {
            Hide();
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Toggle();
        }
    }

    public void Toggle()
    {
        if (IsOpen)
        {
            Hide();
            return;
        }

        Show();
    }

    public void Show()
    {
        if (panelRoot == null)
        {
            return;
        }

        if (!IsOpen)
        {
            previousTimeScale = Time.timeScale;
        }

        SetPanelVisible(true);
        activePanel = this;

        if (pauseGameWhenOpen)
        {
            Time.timeScale = 0f;
        }

        if (controlCursor)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
    }

    public void Hide()
    {
        SetPanelVisible(false);

        if (activePanel == this)
        {
            activePanel = null;
        }

        if (pauseGameWhenOpen)
        {
            Time.timeScale = previousTimeScale;
        }
    }

    private void OnDestroy()
    {
        if (pauseGameWhenOpen && IsOpen)
        {
            Time.timeScale = previousTimeScale;
        }

        if (activePanel == this)
        {
            activePanel = null;
        }
    }

    private void ResolveReferences()
    {
        if (panelRoot == null)
        {
            panelRoot = gameObject;
        }

        if (UsesCanvasGroupVisibility)
        {
            panelCanvasGroup = panelRoot.GetComponent<CanvasGroup>();
            if (panelCanvasGroup == null)
            {
                panelCanvasGroup = panelRoot.AddComponent<CanvasGroup>();
            }
        }
    }

    private void SetPanelVisible(bool visible)
    {
        if (panelRoot == null)
        {
            return;
        }

        if (UsesCanvasGroupVisibility)
        {
            if (panelCanvasGroup == null)
            {
                panelCanvasGroup = panelRoot.GetComponent<CanvasGroup>();
            }

            if (panelCanvasGroup != null)
            {
                panelCanvasGroup.alpha = visible ? 1f : 0f;
                panelCanvasGroup.interactable = visible;
                panelCanvasGroup.blocksRaycasts = visible;
            }

            return;
        }

        panelRoot.SetActive(visible);
    }
}
