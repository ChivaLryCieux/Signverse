using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.Events;

// 主菜单存档面板：「开始新游戏（清档）」与「继续游戏」。
// 触发方式二选一或并存：
//   1. UI Button（可选）：在 Inspector 绑定 newGameButton / continueButton。
//   2. 3D 物体（可选）：在 Inspector 绑定 newGameCollider / continueCollider，
//      本脚本在 Update 中用射线检测点击，命中后触发对应功能（复用 MainMenuScenePortal 的射线模式）。
// TMP 文本（continueButtonLabel）为可选：绑定则根据存档点更新文案，不绑则跳过。
// 与现有 MainMenuScenePortal 并存：portal 仍是快速启动入口，本面板提供存档读写入口。
public class SavePanelController : MonoBehaviour
{
    [Header("UI 按钮（可选）")]
    [SerializeField] private Button newGameButton;
    [SerializeField] private Button continueButton;

    [Header("3D 物体（可选，需带 Collider）")]
    [Tooltip("点击该 3D 物体触发「开始新游戏」。需挂 Collider。")]
    [SerializeField] private Collider newGameCollider;
    [Tooltip("点击该 3D 物体触发「继续游戏」。需挂 Collider。")]
    [SerializeField] private Collider continueCollider;

    [Header("TMP（可选）")]
    [Tooltip("继续按钮的文本，绑定后会根据存档点更新为 \"Resume N\"；不绑则跳过文本更新。")]
    [SerializeField] private TMP_Text continueButtonLabel;

    [Header("射线检测")]
    [Tooltip("射线检测使用的相机。留空则使用 Camera.main。")]
    [SerializeField] private Camera raycastCamera;
    [Tooltip("射线检测的最大距离。")]
    [SerializeField] private float maxRayDistance = 100f;

    [Header("转场时间")]
    [SerializeField] private float transitionTime = 1f;


    public UnityEvent OnNewGameEvent = new UnityEvent();
    public UnityEvent OnContinueGameEvent = new UnityEvent();

    private Camera mainCamera;
    private bool continueEnabled = true;
    private bool isChangingScene;

    private void Start()
    {
        EnsureSaveManager();

        // 继续按钮是否可用取决于是否存在存档
        continueEnabled = SaveManager.HasSave;

        if (continueButton != null)
        {
            continueButton.interactable = continueEnabled;
            continueButton.onClick.AddListener(OnContinueClicked);
        }

        if (newGameButton != null)
        {
            newGameButton.onClick.AddListener(OnNewGameClicked);
        }

        UpdateContinueLabel();
    }

    private void Update()
    {
        // 任一 3D 碰撞体未绑定时无需做射线检测
        if (newGameCollider == null && continueCollider == null)
        {
            return;
        }

        if (isChangingScene)
        {
            return;
        }

        // 鼠标左键点击 / 触屏点击 / 移动端交互按钮
        bool tapped = (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                      || (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
                      || MobileInputManager.InteractPressed;

        if (!tapped)
        {
            return;
        }

        MobileInputManager.ConsumeInteractPressed();

        Detect3DClick();
    }

    private void Detect3DClick()
    {
        if (mainCamera == null)
        {
            mainCamera = raycastCamera != null ? raycastCamera : Camera.main;
        }

        if (mainCamera == null)
        {
            return;
        }

        Vector2 screenPos = Mouse.current != null
            ? Mouse.current.position.ReadValue()
            : (Touchscreen.current != null
                ? (Vector2)Touchscreen.current.primaryTouch.position.ReadValue()
                : Vector2.zero);

        Ray ray = mainCamera.ScreenPointToRay(screenPos);

        // 优先检测「开始新游戏」碰撞体，再检测「继续游戏」碰撞体
        if (newGameCollider != null && newGameCollider.Raycast(ray, out _, maxRayDistance))
        {
            OnNewGameClicked();
            return;
        }

        if (continueEnabled && continueCollider != null && continueCollider.Raycast(ray, out _, maxRayDistance))
        {
            OnContinueClicked();
        }
    }

    private void UpdateContinueLabel()
    {
        if (continueButtonLabel == null)
        {
            return;
        }

        SaveData save = SaveManager.LoadSave();
        if (save != null && save.checkpointIndex >= 0)
        {
            continueButtonLabel.text = $"Resume {save.checkpointIndex}";
        }
        else
        {
            continueButtonLabel.text = "Resume";
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

    // ── 公开入口：可供 UI Button.onClick、3D 射线检测、或外部脚本调用 ──

    public void OnNewGameClicked()
    {
        if (isChangingScene)
        {
            return;
        }

        if (SaveManager.Instance != null)
        {
            isChangingScene = true;
            StartCoroutine(NewGameTransition());
        }
    }
    //使用协程，处理转场停留时间、音效、标识响应动画
    private IEnumerator NewGameTransition()
    {
        OnNewGameEvent?.Invoke();
        yield return new WaitForSeconds(transitionTime);

        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.StartNewGame();
        }
    }

    public void OnContinueClicked()
    {
        if (isChangingScene || !continueEnabled)
        {
            return;
        }

        if (SaveManager.Instance != null)
        {
            isChangingScene = true;
            StartCoroutine(ContinueGameTransition());
        }
    }
    //使用协程，处理转场停留时间、音效、标识响应动画
    private IEnumerator ContinueGameTransition()
    {
        OnContinueGameEvent?.Invoke();
        yield return new WaitForSeconds(transitionTime);

        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.ContinueGame();
        }
    }
}
