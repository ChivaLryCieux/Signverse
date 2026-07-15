using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.Events;

// 主菜单存档面板：
// 1. UI Button 调用
// 2. 3D Collider 射线点击调用
// 3. Quit Game Collider 退出游戏

public class SavePanelController : MonoBehaviour
{
    [Header("UI 按钮（可选）")]
    [SerializeField] private Button newGameButton;
    [SerializeField] private Button continueButton;


    [Header("3D 物体（可选，需带 Collider）")]
    [Tooltip("点击该3D物体触发开始新游戏")]
    [SerializeField] private Collider newGameCollider;

    [Tooltip("点击该3D物体触发继续游戏")]
    [SerializeField] private Collider continueCollider;

    [Tooltip("点击该3D物体退出游戏")]
    [SerializeField] private Collider quitGameCollider;


    [Header("TMP（可选）")]
    [Tooltip("继续按钮文本")]
    [SerializeField] private TMP_Text continueButtonLabel;


    [Header("射线检测")]
    [SerializeField] private Camera raycastCamera;

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
        if (newGameCollider == null &&
            continueCollider == null &&
            quitGameCollider == null)
        {
            return;
        }


        if (isChangingScene)
        {
            return;
        }


        bool tapped =
            (Mouse.current != null &&
             Mouse.current.leftButton.wasPressedThisFrame)
             ||
            (Touchscreen.current != null &&
             Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
             ||
            MobileInputManager.InteractPressed;


        if (!tapped)
        {
            return;
        }


        MobileInputManager.ConsumeInteractPressed();


        Detect3DClick();

        // Q+U+I+T 退出游戏
        if (Keyboard.current.qKey.isPressed &&
            Keyboard.current.uKey.isPressed &&
            Keyboard.current.iKey.isPressed &&
            Keyboard.current.tKey.wasPressedThisFrame)
        {
            Application.Quit();
        }
    }



    private void Detect3DClick()
    {
        if (mainCamera == null)
        {
            mainCamera =
                raycastCamera != null
                ? raycastCamera
                : Camera.main;
        }


        if (mainCamera == null)
        {
            return;
        }


        Vector2 screenPos =
            Mouse.current != null
            ?
            Mouse.current.position.ReadValue()
            :
            (
                Touchscreen.current != null
                ?
                (Vector2)Touchscreen.current.primaryTouch.position.ReadValue()
                :
                Vector2.zero
            );


        Ray ray = mainCamera.ScreenPointToRay(screenPos);



        // 开始新游戏
        if (newGameCollider != null &&
            newGameCollider.Raycast(ray, out _, maxRayDistance))
        {
            OnNewGameClicked();
            return;
        }



        // 继续游戏
        if (continueEnabled &&
            continueCollider != null &&
            continueCollider.Raycast(ray, out _, maxRayDistance))
        {
            OnContinueClicked();
            return;
        }



        // 退出游戏
        if (quitGameCollider != null &&
            quitGameCollider.Raycast(ray, out _, maxRayDistance))
        {
            QuitGame();
        }
    }



    private void UpdateContinueLabel()
    {
        if (continueButtonLabel == null)
        {
            return;
        }


        SaveData save = SaveManager.LoadSave();


        if (save != null &&
            save.checkpointIndex >= 0)
        {
            continueButtonLabel.text =
                $"Resume {save.checkpointIndex}";
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
            GameObject go =
                new GameObject("SaveManager");

            go.AddComponent<SaveManager>();
        }
    }



    // ==========================
    // 开始新游戏
    // ==========================

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



    private IEnumerator NewGameTransition()
    {
        OnNewGameEvent?.Invoke();


        yield return new WaitForSeconds(
            transitionTime
        );


        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.StartNewGame();
        }
    }



    // ==========================
    // 继续游戏
    // ==========================

    public void OnContinueClicked()
    {
        if (isChangingScene ||
            !continueEnabled)
        {
            return;
        }


        if (SaveManager.Instance != null)
        {
            isChangingScene = true;

            StartCoroutine(ContinueGameTransition());
        }
    }



    private IEnumerator ContinueGameTransition()
    {
        OnContinueGameEvent?.Invoke();


        yield return new WaitForSeconds(
            transitionTime
        );


        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.ContinueGame();
        }
    }



    // ==========================
    // 退出游戏
    // ==========================

    public void QuitGame()
    {
        Debug.Log("Quit Game");
        Application.Quit();

    }
}