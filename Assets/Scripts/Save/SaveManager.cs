using System.Collections.Generic;
using System.IO;
using Skills;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 存档管理器：DontDestroyOnLoad 单例。负责读写单存档位 JSON，
/// 协调「开始新游戏（清档）」与「继续游戏（读档）」两种加载模式。
/// 通过 SceneManager.sceneLoaded 在关卡场景 Awake 初始化后自动应用存档，
/// 因此无需在每个关卡单独放置 Applier 组件。
/// </summary>
public class SaveManager : MonoBehaviour
{
    public enum LoadMode
    {
        None,
        NewGame,
        Continue
    }

    public static SaveManager Instance { get; private set; }

    [Header("场景")]
    [Tooltip("开始新游戏时加载的关卡场景路径。")]
    [SerializeField] private string newGameScenePath = "Assets/Scenes/主scene/1夜晚林间.unity";

    private const string SaveFileName = "save.json";

    public LoadMode PendingMode { get; private set; } = LoadMode.None;

    /// <summary>
    /// Continue 模式下缓存已读取的存档，供场景加载后应用。
    /// </summary>
    public SaveData LoadedSave { get; private set; }

    private static string SavePath => Path.Combine(Application.persistentDataPath, SaveFileName);

    public static bool HasSave => File.Exists(SavePath);

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnApplicationQuit()
    {
        CaptureAndSave();
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            CaptureAndSave();
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        LoadMode pending = PendingMode;

        if (pending == LoadMode.Continue)
        {
            ApplyLoadedSave(LoadedSave);
        }

        if (pending != LoadMode.None)
        {
            ClearPending();
        }
    }

    // ── 主菜单按钮入口 ──

    /// <summary>
    /// 开始新游戏：清档 + 以默认状态加载主关卡。
    /// </summary>
    public void StartNewGame()
    {
        DeleteSave();
        LoadedSave = null;
        PendingMode = LoadMode.NewGame;
        SceneManager.LoadScene(newGameScenePath);
    }

    /// <summary>
    /// 继续游戏：读取存档，加载存档所在场景。无存档时忽略。
    /// </summary>
    public void ContinueGame()
    {
        SaveData data = LoadSave();
        if (data == null)
        {
            Debug.LogWarning("[SaveManager] 没有可用的存档，无法继续游戏。", this);
            return;
        }

        LoadedSave = data;
        PendingMode = LoadMode.Continue;
        if (data.sceneBuildIndex >= 0)
        {
            SceneManager.LoadScene(data.sceneBuildIndex);
        }
        else
        {
            SceneManager.LoadScene(newGameScenePath);
        }
    }

    /// <summary>
    /// 应用完存档后调用，回到 None 模式。
    /// </summary>
    public void ClearPending()
    {
        PendingMode = LoadMode.None;
    }

    // ── 存档读写 ──

    public static void DeleteSave()
    {
        try
        {
            if (File.Exists(SavePath))
            {
                File.Delete(SavePath);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SaveManager] 删除存档失败: {e.Message}");
        }
    }

    public static SaveData LoadSave()
    {
        if (!File.Exists(SavePath))
        {
            return null;
        }

        try
        {
            string json = File.ReadAllText(SavePath);
            return JsonUtility.FromJson<SaveData>(json);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SaveManager] 读取存档失败: {e.Message}");
            return null;
        }
    }

    /// <summary>
    /// 从当前关卡场景的各单例采集状态并写入存档。
    /// 在主菜单（无 PlayerCC）等非关卡场景下不会写入。
    /// </summary>
    public void CaptureAndSave()
    {
        SaveData data = CaptureState();
        if (data == null)
        {
            return;
        }

        try
        {
            string json = JsonUtility.ToJson(data, true);
            Directory.CreateDirectory(Application.persistentDataPath);
            File.WriteAllText(SavePath, json);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SaveManager] 写入存档失败: {e.Message}");
        }
    }

    private SaveData CaptureState()
    {
        PlayerCC player = FindFirstObjectByType<PlayerCC>();
        if (player == null)
        {
            // 不在关卡场景（如主菜单），不采集。
            return null;
        }

        SaveData data = new SaveData
        {
            sceneBuildIndex = SceneManager.GetActiveScene().buildIndex
        };

        PlayerDeath death = player.GetComponent<PlayerDeath>();
        if (death != null)
        {
            Vector3 cp = death.CurrentCheckpoint;
            data.checkpointX = cp.x;
            data.checkpointY = cp.y;
            data.checkpointZ = cp.z;
        }

        if (PickupUIController.Instance != null)
        {
            data.pickup = PickupUIController.Instance.CaptureState();
        }

        if (BoltPanelController.Instance != null)
        {
            data.boltUnlockedCount = BoltPanelController.Instance.UnlockedCount;
        }

        List<SkillBase> unlocked = player.unlockedSkills;
        if (unlocked != null)
        {
            data.unlockedSkillIds = new List<string>(unlocked.Count);
            for (int i = 0; i < unlocked.Count; i++)
            {
                SkillBase skill = unlocked[i];
                if (skill != null && !string.IsNullOrEmpty(skill.skillID))
                {
                    data.unlockedSkillIds.Add(skill.skillID);
                }
            }
        }

        return data;
    }

    // ── 继续游戏：把存档应用到当前场景 ──

    private void ApplyLoadedSave(SaveData data)
    {
        if (data == null)
        {
            return;
        }

        PlayerCC player = FindFirstObjectByType<PlayerCC>();
        if (player == null)
        {
            return;
        }

        // 1. 螺栓解锁数（先恢复，ApplyState 内 SyncBoltSpend 才能用正确的上限重算 spentCount）
        if (BoltPanelController.Instance != null)
        {
            BoltPanelController.Instance.SetUnlockedCount(data.boltUnlockedCount);
        }

        // 2. 装备/拾取 UI 状态（内部会 Refresh + SyncLinkedSkills 重建 combo 技能 + SetEquippedSkills）
        if (PickupUIController.Instance != null && data.pickup != null)
        {
            PickupUIController.Instance.ApplyState(data.pickup);
        }

        // 3. 补回所有已解锁技能（combo 已由 SyncLinkedSkills 重建，此处 Contains 去重，补回直接解锁/初始技能）
        if (data.unlockedSkillIds != null)
        {
            for (int i = 0; i < data.unlockedSkillIds.Count; i++)
            {
                player.UnlockNewSkill(data.unlockedSkillIds[i]);
            }
        }

        // 4. 传送玩家到存档点
        PlayerDeath death = player.GetComponent<PlayerDeath>();
        if (death != null)
        {
            Vector3 cp = new Vector3(data.checkpointX, data.checkpointY, data.checkpointZ);
            death.PlaceAtCheckpoint(cp);
        }

        // 5. 禁用已经捡过的道具/第五槽触发器，避免重复拾取
        DisableCollectedPickups(data);
    }

    private void DisableCollectedPickups(SaveData data)
    {
        if (data.pickup != null && data.pickup.unlockedItems != null && data.pickup.unlockedItems.Count > 0)
        {
            HashSet<PickupItemId> collected = new HashSet<PickupItemId>(data.pickup.unlockedItems);
            PickupCollectible[] pickups = FindObjectsOfType<PickupCollectible>();
            for (int i = 0; i < pickups.Length; i++)
            {
                if (pickups[i] != null && collected.Contains(pickups[i].ItemId))
                {
                    pickups[i].gameObject.SetActive(false);
                }
            }
        }

        if (data.pickup != null && data.pickup.fifthEquippedSlotUnlocked)
        {
            FifthEquippedSlotPickupTrigger[] fifths = FindObjectsOfType<FifthEquippedSlotPickupTrigger>();
            for (int i = 0; i < fifths.Length; i++)
            {
                if (fifths[i] != null)
                {
                    fifths[i].gameObject.SetActive(false);
                }
            }
        }
    }
}
