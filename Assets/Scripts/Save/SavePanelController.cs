using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 主菜单存档面板：「开始新游戏（清档）」与「继续游戏」。
/// 放在主菜单场景的任意 GameObject 上。
/// 若在 Inspector 指定了两个按钮，则使用它们；否则自动生成一个最简 Canvas+面板+按钮，免场景搭建。
/// 与现有 MainMenuScenePortal 并存：portal 仍是快速启动入口，本面板提供存档读写入口。
/// </summary>
public class SavePanelController : MonoBehaviour
{
    [Header("按钮（可选，留空则自动生成）")]
    [SerializeField] private Button newGameButton;
    [SerializeField] private Button continueButton;

    [Header("自动生成样式")]
    [SerializeField] private string newGameLabel = "开始新游戏";
    [SerializeField] private string continueLabel = "继续游戏";
    [SerializeField] private float panelWidth = 360f;
    [SerializeField] private float panelHeight = 240f;
    [SerializeField] private float buttonHeight = 64f;

    private void Start()
    {
        EnsureSaveManager();

        if (newGameButton == null || continueButton == null)
        {
            BuildProceduralUI();
        }

        if (continueButton != null)
        {
            continueButton.interactable = SaveManager.HasSave;
            continueButton.onClick.AddListener(OnContinueClicked);
        }

        if (newGameButton != null)
        {
            newGameButton.onClick.AddListener(OnNewGameClicked);
        }
    }

    private void EnsureSaveManager()
    {
        if (SaveManager.Instance == null)
        {
            GameObject go = new GameObject("SaveManager");
            go.AddComponent<SaveManager>();
        }
    }

    private void OnNewGameClicked()
    {
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.StartNewGame();
        }
    }

    private void OnContinueClicked()
    {
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.ContinueGame();
        }
    }

    // ── 过程化 UI 兜底（未在 Inspector 指定按钮时使用）──

    private void BuildProceduralUI()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            GameObject canvasGo = new GameObject("SaveCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
        }

        if (FindFirstObjectByType<EventSystem>() == null)
        {
            GameObject esGo = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        GameObject panelGo = new GameObject("SavePanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panelGo.transform.SetParent(canvas.transform, false);
        RectTransform panelRect = panelGo.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(panelWidth, panelHeight);
        panelRect.anchoredPosition = Vector2.zero;
        panelGo.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.85f);

        newGameButton = CreateButton(panelRect, newGameLabel, new Vector2(0f, buttonHeight * 0.6f));
        continueButton = CreateButton(panelRect, continueLabel, new Vector2(0f, -buttonHeight * 0.6f));
    }

    private Button CreateButton(RectTransform parent, string label, Vector2 offset)
    {
        GameObject btnGo = new GameObject("Button_" + label, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        btnGo.transform.SetParent(parent, false);
        RectTransform rt = btnGo.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(panelWidth * 0.8f, buttonHeight);
        rt.anchoredPosition = offset;
        btnGo.GetComponent<Image>().color = new Color(0.2f, 0.2f, 0.25f, 1f);

        GameObject textGo = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        textGo.transform.SetParent(btnGo.transform, false);
        RectTransform textRt = textGo.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.pivot = new Vector2(0.5f, 0.5f);
        textRt.sizeDelta = Vector2.zero;
        textRt.anchoredPosition = Vector2.zero;
        Text text = textGo.GetComponent<Text>();
        text.text = label;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 28;

        return btnGo.GetComponent<Button>();
    }
}
