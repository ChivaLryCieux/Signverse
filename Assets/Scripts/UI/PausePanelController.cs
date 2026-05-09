using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PausePanelController : MonoBehaviour
{
    private static PausePanelController activePanel;

    [Header("Panel")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private GameObject tipPanel;
    [SerializeField] private bool hideOnStart = true;

    [Header("Pause")]
    [SerializeField] private bool pauseGameWhenOpen;
    [SerializeField] private bool controlCursor = true;

    [Header("Actions")]
    [SerializeField] private PlayerDeath playerDeath;
    [SerializeField] private string mainMenuSceneName = "开始界面";

    [Header("Placed Images")]
    [SerializeField] private Image rewindImage;
    [SerializeField] private Image continueImage;
    [SerializeField] private Image exitImage;
    [SerializeField] private Button checkpointButton;
    [SerializeField] private Button continueButton;
    [SerializeField] private Button mainMenuButton;

    private float previousTimeScale = 1f;
    private CanvasGroup panelCanvasGroup;
    private CanvasGroup tipPanelCanvasGroup;

    private bool UsesCanvasGroupVisibility => panelRoot == gameObject;

    public static bool IsPaused => activePanel != null && (activePanel.IsOpen || activePanel.IsTipPanelOpen());

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
        SetupActionButtons();

        if (hideOnStart)
        {
            Hide();
        }
        else
        {
            SetTipPanelVisible(false);
        }
    }

    private void Update()
    {
        if (PickupUIController.BlocksPauseEscape)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (IsTipPanelOpen())
            {
                CloseTipPanel();
                return;
            }

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
        SetTipPanelVisible(false);
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

    public void RespawnAtLastCheckpoint()
    {
        ResolvePlayerDeath();
        RestoreTimeScaleBeforeAction();
        Hide();

        if (playerDeath != null)
        {
            playerDeath.RespawnAtLastCheckpoint();
        }
        else
        {
            Debug.LogWarning("PausePanelController 没有找到 PlayerDeath，无法返回上一个 checkpoint。", this);
        }
    }

    public void ContinueGame()
    {
        Hide();
    }

    public void OpenTipPanel()
    {
        if (tipPanel == null)
        {
            ResolveTipPanel();
        }

        if (tipPanel == null)
        {
            Debug.LogWarning("PausePanelController 没有找到 TipPanel，无法打开提示面板。", this);
            return;
        }

        SetPanelVisible(false);
        SetTipPanelVisible(true);
        activePanel = this;
    }

    public void CloseTipPanel()
    {
        SetTipPanelVisible(false);
        SetPanelVisible(true);
        activePanel = this;
    }

    public void LoadMainMenuScene()
    {
        if (string.IsNullOrWhiteSpace(mainMenuSceneName))
        {
            Debug.LogWarning("PausePanelController 没有设置主菜单 Scene 名称。", this);
            return;
        }

        RestoreTimeScaleBeforeAction();
        activePanel = null;
        SceneManager.LoadScene(mainMenuSceneName);
    }

    private void OnDestroy()
    {
        if (pauseGameWhenOpen && (IsOpen || IsTipPanelOpen()))
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

        ResolvePlayerDeath();
        ResolveTipPanel();
    }

    private void SetupActionButtons()
    {
        checkpointButton = ResolvePlacedImageButton(rewindImage, checkpointButton);
        continueButton = ResolvePlacedImageButton(continueImage, continueButton);
        mainMenuButton = ResolvePlacedImageButton(exitImage, mainMenuButton);

        BindButton(checkpointButton, RespawnAtLastCheckpoint);
        BindButton(continueButton, OpenTipPanel);
        BindButton(mainMenuButton, LoadMainMenuScene);
    }

    private Button ResolvePlacedImageButton(Image image, Button existingButton)
    {
        if (existingButton != null)
        {
            if (existingButton.targetGraphic == null && image != null)
            {
                existingButton.targetGraphic = image;
            }

            return existingButton;
        }

        if (image == null)
        {
            return null;
        }

        image.raycastTarget = true;
        if (!image.TryGetComponent(out Button button))
        {
            button = image.gameObject.AddComponent<Button>();
        }

        button.targetGraphic = image;
        return button;
    }

    private void BindButton(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null || action == null)
        {
            return;
        }

        button.onClick.RemoveListener(action);
        button.onClick.AddListener(action);
    }

    private void ResolvePlayerDeath()
    {
        if (playerDeath != null && playerDeath.gameObject.activeInHierarchy)
        {
            return;
        }

        GameObject taggedPlayer = GameObject.FindGameObjectWithTag("Player");
        if (taggedPlayer != null)
        {
            playerDeath = taggedPlayer.GetComponentInParent<PlayerDeath>();
            if (playerDeath == null)
            {
                playerDeath = taggedPlayer.GetComponentInChildren<PlayerDeath>(true);
            }
        }

        if (playerDeath != null)
        {
            return;
        }

        PlayerDeath[] playerDeaths = FindObjectsOfType<PlayerDeath>(true);
        for (int i = 0; i < playerDeaths.Length; i++)
        {
            if (playerDeaths[i] != null && playerDeaths[i].gameObject.activeInHierarchy)
            {
                playerDeath = playerDeaths[i];
                return;
            }
        }
    }

    private void ResolveTipPanel()
    {
        if (tipPanel != null)
        {
            CacheTipPanelCanvasGroup();
            return;
        }

        GameObject[] sceneObjects = Resources.FindObjectsOfTypeAll<GameObject>();
        for (int i = 0; i < sceneObjects.Length; i++)
        {
            GameObject sceneObject = sceneObjects[i];
            if (sceneObject == null || sceneObject.name != "TipPanel" || !sceneObject.scene.IsValid())
            {
                continue;
            }

            tipPanel = sceneObject;
            CacheTipPanelCanvasGroup();
            return;
        }
    }

    private void RestoreTimeScaleBeforeAction()
    {
        if (pauseGameWhenOpen)
        {
            Time.timeScale = previousTimeScale;
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

    private bool IsTipPanelOpen()
    {
        if (tipPanel == null)
        {
            return false;
        }

        if (tipPanelCanvasGroup != null)
        {
            return tipPanel.activeInHierarchy && tipPanelCanvasGroup.alpha > 0f;
        }

        return tipPanel.activeSelf;
    }

    private void SetTipPanelVisible(bool visible)
    {
        if (tipPanel == null)
        {
            ResolveTipPanel();
        }

        if (tipPanel == null)
        {
            return;
        }

        CacheTipPanelCanvasGroup();
        if (visible && !tipPanel.activeSelf)
        {
            tipPanel.SetActive(true);
        }

        if (tipPanelCanvasGroup != null)
        {
            tipPanelCanvasGroup.alpha = visible ? 1f : 0f;
            tipPanelCanvasGroup.interactable = visible;
            tipPanelCanvasGroup.blocksRaycasts = visible;
            return;
        }

        tipPanel.SetActive(visible);
    }

    private void CacheTipPanelCanvasGroup()
    {
        if (tipPanel == null)
        {
            tipPanelCanvasGroup = null;
            return;
        }

        tipPanelCanvasGroup = tipPanel.GetComponent<CanvasGroup>();
    }
}
