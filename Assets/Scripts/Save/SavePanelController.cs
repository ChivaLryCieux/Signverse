using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 主菜单存档面板：「开始新游戏（清档）」与「继续游戏」。
// 在 Inspector 中指定两个 Button，本脚本仅负责接线与 SaveManager 兜底创建。
// 与现有 MainMenuScenePortal 并存：portal 仍是快速启动入口，本面板提供存档读写入口。
public class SavePanelController : MonoBehaviour
{
    [Header("按钮")]
    [SerializeField] private Button newGameButton;
    [SerializeField] private Button continueButton;

    [Header("TMP")]
    [SerializeField] private TMP_Text continueButtonLabel;

    private void Start()
    {
        EnsureSaveManager();

        if (continueButton != null)
        {
            continueButton.interactable = SaveManager.HasSave;
            continueButton.onClick.AddListener(OnContinueClicked);
            UpdateContinueLabel();
        }

        if (newGameButton != null)
        {
            newGameButton.onClick.AddListener(OnNewGameClicked);
        }
    }

    private void UpdateContinueLabel()
    {
        if (continueButtonLabel == null) return;

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
}
