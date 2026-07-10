# 存档系统设计方案（已确认决策）

## 目标
主菜单「存档 Panel」含两个按钮：
- **开始新游戏**：清档 + 正常重新开始（加载主关卡、默认状态）
- **继续游戏**：加载存档所在场景 -> 在存档点处重生 -> 恢复已解锁技能与全部装备状态

## 已确认决策
1. **存档范围 = 完整状态**（检查点 + 技能 + 装备槽 + 螺栓 + 第五槽 + 模仿），无缝继续。
2. **主菜单 = 两者并存**：保留现有 3D `MainMenuScenePortal`，新增 `SavePanel` UI。
3. **点新游戏即清档**：`StartNewGame` 删除存档文件，`HasSave=false`；新进度首次存档时重建。

## 关键事实（已确认）
- `PickupUIController` / `BoltPanelController` / `SkillController` / `PlayerCC`/`PlayerDeath` 均**场景级**，切场景重置 -> 存档是唯一跨会话持久化。
- 检查点：`Checkpoint.OnTriggerEnter` -> `player.SetCheckpoint(pos)` -> `PlayerDeath.currentCheckpoint`。
- `SpawnSpot.Awake` 放玩家到出生点；`PlayerDeath.Awake` 据此设 `currentCheckpoint`。
- `PickupUIController.Awake` 已用默认状态完成全部初始化（含 `SyncLinkedSkills`）-> 可在其 `Start` 之上叠加存档。
- `PickupItemId` 是 enum（Item1–5），可序列化。
- 暂停菜单 `LoadMainMenuScene()` 返回主菜单（场景名 "开始界面"）。
- 现无任何存档代码。

---

## 1. 存档数据模型（`SaveData`，`[Serializable]`）

```
SaveData {
  int sceneBuildIndex;            // 存档所在场景（继续时加载它）
  float checkpointX/Y/Z;          // 存档点位置
  List<PickupItemId> unlockedItems;        // 已拾取的 5 种道具
  List<PickupItemId> equippedSlotItems;    // 5 个装备槽（空槽用 occupied=false 区分）
  List<bool> equippedSlotOccupied;
  bool fifthEquippedSlotUnlocked;
  int boltUnlockedCount;          // 螺栓解锁数（spentCount 由装备槽重算，无需存）
  bool hasMimicTarget;
  int mimicTargetRightSideIndex;
  string mimicTargetComboCode;
  List<string> unlockedSkillIds;  // SkillController.unlockedSkills 的全部 skillID
}
```
存储：`Application.persistentDataPath + "/save.json"`，`JsonUtility` 读写。单存档位。

---

## 2. 新增文件（`Assets/Scripts/Save/`）

### `SaveManager.cs`
`DontDestroyOnLoad` 单例。主菜单场景里放一个。
- `public static bool HasSave` -> `File.Exists(path)`
- `public enum LoadMode { None, NewGame, Continue }`；`public LoadMode PendingMode`
- `public SaveData LoadedSave { get; }`（Continue 时供 Applier 读取）
- `public void StartNewGame()` -> **删档**（`File.Delete`）-> `PendingMode=NewGame` -> `SceneManager.LoadScene(newGameSceneIndex)`（序列化字段，默认主关卡 index）
- `public void ContinueGame()` -> 读存档 -> `PendingMode=Continue` + 缓存 `LoadedSave` -> `SceneManager.LoadScene(save.sceneBuildIndex)`；无存档忽略
- `public void CaptureAndSave()` -> 从各单例采集状态写 JSON
- `public void ClearPending()` -> `PendingMode=None`（Applier 应用完调用）
- `OnApplicationQuit` / `OnApplicationPause(true)` -> `CaptureAndSave()`
- 采集源：`PlayerDeath.CurrentCheckpoint`、`PickupUIController.Instance.CaptureState()`、`BoltPanelController.Instance.UnlockedCount`、`PlayerCC` 的 `SkillController`、`SceneManager.GetActiveScene().buildIndex`

### `SaveLoadApplier.cs`
挂在**每个游戏关卡场景**里（玩家上或空 Manager）。`Start()`：
- `Continue` -> `PickupUIController.ApplySaveState` + `BoltPanelController.SetUnlockedCount` + `PlayerDeath.PlaceAtCheckpoint` + `SkillController` 补技能 -> 禁用已拾取的道具/第五槽触发器 -> `SaveManager.ClearPending()`
- `NewGame` / `None` -> 不做事
- `Start` 天然在所有 `Awake` 之后，默认初始化已完成

### `SavePanelController.cs`（主菜单存档面板）
- 持「开始新游戏」「继续游戏」两 `Button`
- `Start()`：`continueButton.interactable = SaveManager.HasSave`
- 接线：新游戏 -> `SaveManager.Instance.StartNewGame()`；继续 -> `SaveManager.Instance.ContinueGame()`
- 与现有 `MainMenuScenePortal` 并存（portal 仍是快速启动入口）

---

## 3. 现有文件改动（集成点）

### `PlayerDeath.cs`
- 新增 `public Vector3 CurrentCheckpoint => currentCheckpoint;`
- 新增 `public void PlaceAtCheckpoint(Vector3 pos)`：设 `currentCheckpoint`、禁 `CharacterController`、`transform.position=(x,y,0)`、恢复 CC、`SetVerticalVelocity(-2f)`（复用 `RespawnAtCheckpoint` 传送套路，不走死亡流程）
- `SetCheckpoint(...)` 末尾 -> `SaveManager.Instance?.CaptureAndSave()`（**存档主触发点**）

### `PickupUIController.cs`
- 新增 `public PickupSaveState CaptureState()` / `public void ApplyState(PickupSaveState)`（恢复 unlockedItems/equippedSlotItems/occupied/fifth/mimic -> `RefreshUnlockedSlots`/`RefreshEquippedSlots`/`SyncBoltSpend`/`SyncLinkedSkills`）
- 装备/卸下/交换/模仿变更后调 `SyncLinkedSkills` 的几处末尾 -> `SaveManager.Instance?.CaptureAndSave()`

### `BoltPanelController.cs`
- 已有 `SetUnlockedCount(int)` / `UnlockedCount`，直接用

### `SkillController.cs`（经 `PlayerCC`）
- 采集 `unlockedSkills` 的 skillID 列表；恢复 `foreach id -> UnlockNewSkill(id)`（combo 由 `SyncLinkedSkills` 重建，直接解锁的补回，`Contains` 去重）

### `PickupCollectible.cs` / `BoltPickupTrigger.cs` / `FifthEquippedSlotPickupTrigger.cs`
- 各 `TryCollect()` 成功后 -> `SaveManager.Instance?.CaptureAndSave()`

### `PausePanelController.cs`
- `LoadMainMenuScene()` 在 `LoadScene` 前 -> `SaveManager.Instance?.CaptureAndSave()`

### `TriggerScenePortal.cs`
- `ChangeScene()` 在 `LoadScene` 前 -> `SaveManager.Instance?.CaptureAndSave()`（跨场景保进度）

### 主菜单场景 `开始界面.unity`
- 新增 SavePanel UI（Canvas + 两 Button + SavePanelController），与 portal 并存

---

## 4. 流程

**开始新游戏**：点按钮 -> 删档 -> `PendingMode=NewGame` -> 加载主关卡 -> `SaveLoadApplier.Start` 见 NewGame -> 不应用 -> 默认状态游玩。首个检查点/拾取触发 `CaptureAndSave` 重建存档。

**继续游戏**：点按钮 -> 读存档 -> `PendingMode=Continue`+缓存 -> 加载 `save.sceneBuildIndex` -> 场景默认 Awake 初始化 -> `SaveLoadApplier.Start` 见 Continue -> 应用状态 + 传送玩家到检查点 + 禁用已捡道具 -> `ClearPending` -> 游玩。

**死亡重生**：不变（仍用 `PlayerDeath.RespawnAtCheckpoint`），重生不触发存档。

---

## 5. 已知限制（先不做）
1. **已捡拾取物重新出现**：场景重载后物体仍在；重新捡 `Unlock` 幂等无副作用。**默认会对** `itemId∈unlockedItems` 的 `PickupCollectible`、已解锁的 `FifthEquippedSlotPickupTrigger` **做禁用**；螺栓残留/轻微超量接受（按稳定 ID 跟踪是较大改动，暂不做）。
2. **跨场景进度**：存档系统恢复"存档所在场景"的状态；进入新场景后首个检查点更新存档到新场景。新场景未触发检查点就退出，继续会回到上一场景存档点（可接受）。
