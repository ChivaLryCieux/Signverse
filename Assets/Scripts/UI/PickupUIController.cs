using System;
using System.Collections;
using System.Collections.Generic;
using Skills;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PickupUIController : MonoBehaviour
{
    [Serializable]
    public class PickupUiEntry
    {
        public PickupItemId id;
        public string displayName;
        [Tooltip("右上角技能序号。不填或小于 1 时，默认使用 Item1=1、Item2=2 的枚举顺序。")]
        public int rightSideIndex;
        [Tooltip("用于匹配技能名后缀，例如 m/j/d/c。可留空，留空时只按 13-/24- 这类前缀匹配。")]
        public string comboCode;
        public Sprite icon;
        [Tooltip("右上角用于点击装备的槽位。")]
        public PickupUISlotView unlockSlot;
        [Tooltip("右键技能图标时显示的详情面板。为空时使用通用 DetailPanel。")]
        public GameObject detailPanel;
    }

    public static PickupUIController Instance { get; private set; }

    [Header("5 种拾取物 UI")]
    [SerializeField] private PickupUiEntry[] entries = new PickupUiEntry[5];

    [Header("模仿者 UI")]
    [Tooltip("5 号模仿者选中后，右上角 1/2/3/4 变成的黑色版本；模仿成功后 5 号也会显示对应黑色版本。数组下标 0=1，1=2，2=3，3=4。")]
    [SerializeField] private Sprite[] mimicTargetDarkIcons = new Sprite[4];

    [SerializeField] private AudioClip mimicSuccessSfx;
    [SerializeField] private AudioClip mimicExitSfx;
    [SerializeField, Range(0f, 1f)] private float mimicSfxVolume = 1f;

    [Header("技能装卸音效")]
    [SerializeField] private AudioClip unlockedSkillSelectSfx;
    [SerializeField] private AudioClip equipSuccessSfx;
    [SerializeField] private AudioClip equippedSkillSelectSfx;
    [SerializeField, Range(0f, 1f)] private float skillLoadoutSfxVolume = 1f;

    [Header("技能图标跟随")]
    [SerializeField, Min(0f)] private float selectedIconFollowSpeedOffset;

    [SerializeField] private AudioSource fallbackAudioSource;
    [SerializeField] private AudioSource constantSoundAudioSource;

    [Header("左上角装备栏")]
    [Tooltip("按界面位置顺序拖入左上角 5 个装备槽。装备不会自动补位，槽位允许空缺。")]
    [SerializeField] private PickupUISlotView[] equippedSlots = new PickupUISlotView[5];

    [Header("装备动效")]
    [Tooltip("5 个装备槽各自对应的 image 物体（如 Image(1,1)、Image(2,1) 等）。按槽位顺序拖入。")]
    [SerializeField] private GameObject[] equippedSlotImages = new GameObject[5];

    [Header("第五装备槽")]
    [Tooltip("左上角第 5 个装备槽是否已解锁。未解锁时不能安装技能，可通过 Trigger Pickup 按 E 解锁。")]
    [SerializeField] private bool fifthEquippedSlotUnlocked;

    [Header("调试（仅编辑器测试用）")]
    [Tooltip("勾选后在游戏启动时获得全部螺栓。")]
    [SerializeField] private bool debugUnlockAllBolts;
    [Tooltip("填入要提前解锁的技能序号（1-5），多个用逗号分隔。例如：1,2,3")]
    [SerializeField] private string debugUnlockSkills = "";

    [Header("行为")]
    [SerializeField] private bool hideLockedSlotsOnStart = true;

    [Header("HUD 显示")]
    [SerializeField] private GameObject leftPanel;
    [SerializeField] private GameObject rightPanel;
    [SerializeField] private GameObject boltHudPanel;

    [Header("技能详情")]
    [SerializeField] private GameObject detailPanel;

    [Header("技能装卸警告")]
    [SerializeField] private GameObject warnPanel;
    [SerializeField, Min(0f)] private float warnPanelVisibleDuration = 2f;

    [Header("联动技能")]
    [SerializeField] private PlayerCC player;
    [SerializeField] private SkillDatabase skillDatabase;
    [SerializeField] private bool syncLinkedSkillsToPlayer = true;
    [SerializeField] private bool removePreviousLinkedSkills = true;
    [SerializeField] private bool allowPrefixFallback = true;

    [Header("Bolt 点数")]
    [SerializeField] private BoltPanelController boltPanel;
    [SerializeField] private int firstSkillBoltCost = 1;
    [SerializeField] private int secondSkillBoltCost = 1;
    [SerializeField] private int thirdSkillBoltCost = 2;
    [SerializeField] private int fourthSkillBoltCost = 3;

    public event Action<PickupItemId> ItemUnlocked;
    public event Action<PickupItemId> ItemEquipped;
    public event Action<PickupItemId> ItemUnequipped;

    private readonly Dictionary<PickupItemId, PickupUiEntry> entryById = new Dictionary<PickupItemId, PickupUiEntry>();
    private readonly HashSet<PickupItemId> unlockedItems = new HashSet<PickupItemId>();
    private PickupItemId[] equippedSlotItems = Array.Empty<PickupItemId>();
    private bool[] equippedSlotOccupied = Array.Empty<bool>();
    private bool selectingMimicTarget;
    private bool hasMimicTarget;
    private int mimicTargetRightSideIndex;
    private string mimicTargetComboCode;
    private readonly List<SkillBase> appliedLinkedSkills = new List<SkillBase>();
    private InputAction toggleHudAction;
    private InputAction closeDetailAction;
    private InputAction mimicAction;
    private bool isHudVisible = true;
    private bool isDetailPanelOpen;
    private bool suppressSaveOnSync;
    private PickupItemId currentDetailItem;
    private GameObject activeDetailPanel;
    private int detailPanelClosedFrame = -1;
    private float warnPanelTimer;
    private RectTransform floatingSelectedIcon;
    private Image floatingSelectedIconImage;

    // ── 拖动状态 ──
    private enum DragSource { None, UnlockSlot, EquippedSlot }
    private DragSource dragSource;
    private PickupItemId dragSourceItem;
    private int dragSourceEquippedIndex = -1;
    private Vector3 dragReturnPosition;
    private bool isReturningDrag;
    private int dropTargetSlotIndex = -1;

    // 装备槽组合后真正生效的 SkillBase 快照。它会同步到 PlayerCC.equippedSkills，供动画层读取当前 loadout。
    private readonly List<SkillBase> equippedSkillSnapshot = new List<SkillBase>();

    public bool IsHudVisible => isHudVisible;
    public PickupItemId CurrentDetailItem => currentDetailItem;
    public static bool BlocksPauseEscape => Instance != null &&
        (Instance.isDetailPanelOpen || Instance.detailPanelClosedFrame == Time.frameCount);

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("场景中存在多个 PickupUIController，只会使用最先初始化的一个。", this);
        }
        else
        {
            Instance = this;
        }

        BuildEntries();
        InitializeHudToggleInput();
        InitializeDetailInput();
        InitializeMimicInput();
        ResolveHudPanels();
        ResolveDetailPanel();
        ResolveWarnPanel();
        isHudVisible = true;
        ApplyHudVisibility();
        HideDetailPanel();
        HideWarnPanel();
        ResolveSkillReferences();
        ResolveBoltPanel();
        RefreshAllSlots();
        RefreshUnlockedSlots();
        RefreshEquippedSlots();
        suppressSaveOnSync = true;
        SyncBoltSpend();
        SyncLinkedSkills();
        suppressSaveOnSync = false;
        InitializeEquipEffects();
        InitializeDebugOptions();
    }

    // 启动时隐藏所有装备动效物体，仅在装备瞬间由 PlayEquipEffect 激活显示。
    private void InitializeEquipEffects()
    {
        // 子 prefab 的 active 状态由用户自行控制，代码不做初始化隐藏。
    }

    // 调试选项：在 Inspector 中勾选后，游戏启动时自动解锁装备槽或获得全部螺栓。
    private void InitializeDebugOptions()
    {
        if (debugUnlockAllBolts && boltPanel != null)
        {
            boltPanel.SetUnlockedCount(boltPanel.MaxUnlockedCount);
            Debug.Log("[调试] 已获得全部螺栓。");
        }

        if (!string.IsNullOrEmpty(debugUnlockSkills))
        {
            UnlockSkillsByDebug(debugUnlockSkills);
        }
    }

    // 解析调试输入的技能序号（1-5），解锁对应技能。
    private void UnlockSkillsByDebug(string input)
    {
        string[] parts = input.Split(',');
        foreach (string part in parts)
        {
            string trimmed = part.Trim();
            if (int.TryParse(trimmed, out int index) && index >= 1 && index <= 5)
            {
                PickupItemId itemId = (PickupItemId)(index - 1);
                Unlock(itemId);
                Debug.Log($"[调试] 已解锁技能 {itemId}（序号 {index}）。");
            }
            else
            {
                Debug.LogWarning($"[调试] 无效的技能序号：{trimmed}（应为 1-5）。");
            }
        }
    }

    private void OnEnable()
    {
        if (toggleHudAction != null)
        {
            toggleHudAction.Enable();
        }

        if (closeDetailAction != null)
        {
            closeDetailAction.Enable();
        }

        if (mimicAction != null)
        {
            mimicAction.Enable();
        }
    }

    private void OnDisable()
    {
        if (toggleHudAction != null)
        {
            toggleHudAction.Disable();
        }

        if (closeDetailAction != null)
        {
            closeDetailAction.Disable();
        }

        if (mimicAction != null)
        {
            mimicAction.Disable();
        }
    }

    private void OnDestroy()
    {
        StopSelectedIconFollow();

        if (toggleHudAction != null)
        {
            toggleHudAction.performed -= OnToggleHudPerformed;
            toggleHudAction.Dispose();
            toggleHudAction = null;
        }

        if (closeDetailAction != null)
        {
            closeDetailAction.performed -= OnCloseDetailPerformed;
            closeDetailAction.Dispose();
            closeDetailAction = null;
        }

        if (mimicAction != null)
        {
            mimicAction.performed -= OnMimicTogglePerformed;
            mimicAction.Dispose();
            mimicAction = null;
        }

        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void Unlock(PickupItemId id)
    {
        if (!entryById.ContainsKey(id))
        {
            Debug.LogWarning($"PickupUIController 没有配置拾取物 {id}。", this);
            return;
        }

        if (!unlockedItems.Add(id))
        {
            return;
        }

        RefreshUnlockedSlots();

        // 新解锁的右侧槽播放淡入弹出动画（仅在槽位可见时）
        if (entryById.TryGetValue(id, out PickupUiEntry entry) && entry.unlockSlot != null && entry.unlockSlot.gameObject.activeSelf)
        {
            entry.unlockSlot.PlayUnlockAppearAnimation();
        }

        ItemUnlocked?.Invoke(id);
    }

    public void ToggleHudVisibility()
    {
        SetHudVisibility(!isHudVisible);
    }

    public void SetHudVisibility(bool visible)
    {
        if (isHudVisible == visible)
        {
            ApplyHudVisibility();
            return;
        }

        isHudVisible = visible;
        ApplyHudVisibility();
    }

    public void ShowDetailPanel(PickupItemId id)
    {
        if (!unlockedItems.Contains(id) || !entryById.TryGetValue(id, out PickupUiEntry entry))
        {
            return;
        }

        HideDetailPanel();

        currentDetailItem = id;
        isDetailPanelOpen = true;
        ResolveDetailPanel();

        activeDetailPanel = entry.detailPanel != null ? entry.detailPanel : detailPanel;
        if (detailPanel != null && activeDetailPanel != null && activeDetailPanel.transform.IsChildOf(detailPanel.transform))
        {
            detailPanel.SetActive(true);
        }

        SetDetailPanelActive(activeDetailPanel, true);
    }

    public void HideDetailPanel()
    {
        bool wasOpen = isDetailPanelOpen;
        isDetailPanelOpen = false;
        if (wasOpen)
        {
            detailPanelClosedFrame = Time.frameCount;
        }

        ResolveDetailPanel();

        SetDetailPanelActive(activeDetailPanel, false);
        activeDetailPanel = null;

        if (detailPanel != null)
        {
            detailPanel.SetActive(false);
        }
    }

    private void Update()
    {
        UpdateDrag();
        TickDragReturn();
        TickWarnPanel();
    }

    /// <summary>
    /// 模仿目标选择状态下，左键点击某个解锁槽：直接确认目标。
    /// 返回 true 表示已经处理（无论成功还是失败），调用方应停止后续逻辑。
    /// </summary>
    public bool TryCompleteMimicTargetSelection(PickupItemId id)
    {
        if (!selectingMimicTarget)
        {
            return false;
        }

        if (!entryById.TryGetValue(id, out PickupUiEntry entry) || entry == null)
        {
            return false;
        }

        if (IsMimicIndex(GetRightSideIndex(entry)))
        {
            return false;
        }

        CompleteMimicTargetSelection(entry);
        return true;
    }

    public void UnequipAt(int equippedIndex)
    {
        if (!IsValidEquippedIndex(equippedIndex) || !equippedSlotOccupied[equippedIndex])
        {
            return;
        }

        if (!CanModifySkillLoadout())
        {
            return;
        }

        PickupItemId removedItem = equippedSlotItems[equippedIndex];
        equippedSlotOccupied[equippedIndex] = false;

        RefreshUnlockedSlots();
        RefreshEquippedSlots();
        SyncBoltSpend();
        SyncLinkedSkills();
        PlaySkillLoadoutSfx(equippedSkillSelectSfx);

        ItemUnequipped?.Invoke(removedItem);
    }

    public void UnlockFifthEquippedSlot()
    {
        if (fifthEquippedSlotUnlocked)
        {
            RefreshEquippedSlots();
            return;
        }

        fifthEquippedSlotUnlocked = true;
        RefreshEquippedSlots();
    }

    public bool IsEquippedSlotUnlocked(int equippedIndex)
    {
        return equippedIndex != 4 || fifthEquippedSlotUnlocked;
    }

    private bool CanEquipItemAtSlot(PickupItemId itemId, int equippedIndex)
    {
        if (equippedIndex != 4)
        {
            return true;
        }

        if (!entryById.TryGetValue(itemId, out PickupUiEntry entry) || entry == null)
        {
            return false;
        }

        int rightSideIndex = GetEffectiveRightSideIndex(entry);
        return rightSideIndex >= 1 && rightSideIndex <= 4;
    }

    public bool IsUnlocked(PickupItemId id)
    {
        return unlockedItems.Contains(id);
    }

    private void BuildEntries()
    {
        entryById.Clear();

        if (entries == null)
        {
            entries = Array.Empty<PickupUiEntry>();
            return;
        }

        for (int i = 0; i < entries.Length; i++)
        {
            PickupUiEntry entry = entries[i];
            if (entry == null)
            {
                continue;
            }

            if (!entryById.ContainsKey(entry.id))
            {
                entryById.Add(entry.id, entry);
            }
        }
    }

    private void ResolveSkillReferences()
    {
        if (player == null)
        {
            player = FindObjectOfType<PlayerCC>();
        }

        if (skillDatabase == null && player != null)
        {
            skillDatabase = player.masterDatabase;
        }
    }

    private void ResolveBoltPanel()
    {
        if (boltPanel != null)
        {
            return;
        }

        boltPanel = BoltPanelController.Instance;

        if (boltPanel == null)
        {
            boltPanel = FindObjectOfType<BoltPanelController>();
        }
    }

    private void InitializeHudToggleInput()
    {
        if (toggleHudAction != null)
        {
            return;
        }

        toggleHudAction = new InputAction("ToggleSkillHud", InputActionType.Button, "<Keyboard>/tab");
        toggleHudAction.performed += OnToggleHudPerformed;
    }

    private void InitializeDetailInput()
    {
        if (closeDetailAction != null)
        {
            return;
        }

        closeDetailAction = new InputAction("CloseSkillDetail", InputActionType.Button, "<Keyboard>/escape");
        closeDetailAction.performed += OnCloseDetailPerformed;
    }

    private void InitializeMimicInput()
    {
        if (mimicAction != null)
        {
            return;
        }

        mimicAction = new InputAction("ToggleMimicTarget", InputActionType.Button, "<Keyboard>/m");
        mimicAction.performed += OnMimicTogglePerformed;
    }

    private void OnToggleHudPerformed(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            ToggleHudVisibility();
        }
    }

    private void OnCloseDetailPerformed(InputAction.CallbackContext context)
    {
        if (context.performed && isDetailPanelOpen)
        {
            HideDetailPanel();
        }
    }

    private void OnMimicTogglePerformed(InputAction.CallbackContext context)
    {
        if (!context.performed)
        {
            return;
        }

        if (!TryGetMimicItemId(out PickupItemId mimicId) || !unlockedItems.Contains(mimicId))
        {
            return;
        }

        // 模仿者装备到左上角时，M 键无效；必须先卸下回到右上角
        if (IsEquipped(mimicId))
        {
            return;
        }

        if (selectingMimicTarget)
        {
            // 正在选择目标但没点中 → 取消选择（不要求在 Nature/Water 地面上，纯 UI 状态重置）
            ClearMimicTarget(requireGround: false);
        }
        else if (hasMimicTarget)
        {
            // 已在模仿（且未装备）→ 退出模仿
            ClearMimicTarget();
        }
        else
        {
            // 未模仿 → 进入目标选择
            BeginMimicTargetSelection();
        }
    }

    private bool TryGetMimicItemId(out PickupItemId mimicId)
    {
        foreach (PickupUiEntry entry in entryById.Values)
        {
            if (entry != null && IsMimicIndex(GetRightSideIndex(entry)))
            {
                mimicId = entry.id;
                return true;
            }
        }

        mimicId = default(PickupItemId);
        return false;
    }

    private void ResolveHudPanels()
    {
        if (leftPanel == null)
        {
            leftPanel = FindHudPanel("LeftPanel");
        }

        if (rightPanel == null)
        {
            rightPanel = FindHudPanel("RightPanel");
        }

        if (boltHudPanel == null)
        {
            boltHudPanel = FindHudPanel("BoltPanel");
        }
    }

    private void ResolveDetailPanel()
    {
        if (detailPanel == null)
        {
            detailPanel = FindHudPanel("DetailPanel");
        }
    }

    private void ResolveWarnPanel()
    {
        if (warnPanel == null)
        {
            warnPanel = FindHudPanel("WarnPanel");
        }
    }

    private GameObject FindHudPanel(string panelName)
    {
        Transform searchRoot = transform.root != null ? transform.root : transform;
        Transform[] children = searchRoot.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i] != null && children[i].name == panelName)
            {
                return children[i].gameObject;
            }
        }

        return null;
    }

    private void ApplyHudVisibility()
    {
        ResolveHudPanels();
        SetHudPanelActive(leftPanel, isHudVisible);
        SetHudPanelActive(rightPanel, isHudVisible);
        SetHudPanelActive(boltHudPanel, isHudVisible);
    }

    private static void SetHudPanelActive(GameObject panel, bool active)
    {
        if (panel != null && panel.activeSelf != active)
        {
            panel.SetActive(active);
        }
    }

    private static void SetDetailPanelActive(GameObject panel, bool active)
    {
        if (panel != null && panel.activeSelf != active)
        {
            panel.SetActive(active);
        }
    }

    private void ShowWarnPanel()
    {
        ResolveWarnPanel();
        warnPanelTimer = warnPanelVisibleDuration;

        if (warnPanel != null && !warnPanel.activeSelf)
        {
            warnPanel.SetActive(true);
        }
    }

    private void HideWarnPanel()
    {
        warnPanelTimer = 0f;
        ResolveWarnPanel();

        if (warnPanel != null && warnPanel.activeSelf)
        {
            warnPanel.SetActive(false);
        }
    }

    private void TickWarnPanel()
    {
        if (warnPanel == null || warnPanelTimer <= 0f)
        {
            return;
        }

        warnPanelTimer -= Time.unscaledDeltaTime;
        if (warnPanelTimer <= 0f)
        {
            HideWarnPanel();
        }
    }

    private void RefreshAllSlots()
    {
        foreach (PickupUiEntry entry in entryById.Values)
        {
            if (entry.unlockSlot == null)
            {
                continue;
            }

            entry.unlockSlot.InitializeUnlockSlot(this, entry.id, entry.icon);
        }

        if (equippedSlots == null)
        {
            equippedSlots = Array.Empty<PickupUISlotView>();
        }

        EnsureEquippedStateArrays();

        for (int i = 0; i < equippedSlots.Length; i++)
        {
            if (equippedSlots[i] != null)
            {
                equippedSlots[i].InitializeEquippedSlot(this, i);
            }
        }
    }

    private void RefreshUnlockedSlots()
    {
        foreach (PickupUiEntry entry in entryById.Values)
        {
            if (entry.unlockSlot == null)
            {
                continue;
            }

            bool visible = unlockedItems.Contains(entry.id) && !IsEquipped(entry.id);
            if (!hideLockedSlotsOnStart && !unlockedItems.Contains(entry.id))
            {
                visible = true;
            }

            entry.unlockSlot.gameObject.SetActive(visible);
            if (visible)
            {
                entry.unlockSlot.InitializeUnlockSlot(this, entry.id, GetUnlockIcon(entry));
                bool selected = selectingMimicTarget && IsMimicIndex(GetRightSideIndex(entry));
                entry.unlockSlot.SetSelected(selected);
            }
        }
    }

    private void RefreshEquippedSlots()
    {
        if (equippedSlots == null)
        {
            return;
        }

        EnsureEquippedStateArrays();

        for (int i = 0; i < equippedSlots.Length; i++)
        {
            PickupUISlotView slot = equippedSlots[i];
            if (slot == null)
            {
                continue;
            }

            slot.InitializeEquippedSlot(this, i);
            if (!IsEquippedSlotUnlocked(i))
            {
                slot.ClearIcon();
                continue;
            }

            if (!equippedSlotOccupied[i])
            {
                slot.ClearIcon();
                continue;
            }

            PickupItemId itemId = equippedSlotItems[i];
            if (entryById.TryGetValue(itemId, out PickupUiEntry entry))
            {
                slot.SetItem(itemId, GetEquippedIcon(i, entry));
            }
            else
            {
                slot.ClearIcon();
            }
        }
    }

    private void SaveCurrentState()
    {
        SaveManager.Instance?.CaptureAndSave();
    }

    // 采集当前装备/拾取 UI 状态用于存档。
    public PickupSaveState CaptureState()
    {
        PickupSaveState state = new PickupSaveState();

        state.unlockedItems = new List<PickupItemId>(unlockedItems);

        EnsureEquippedStateArrays();
        state.equippedSlotItems = new List<PickupItemId>(equippedSlotItems);
        state.equippedSlotOccupied = new List<bool>(equippedSlotOccupied);

        state.fifthEquippedSlotUnlocked = fifthEquippedSlotUnlocked;
        state.hasMimicTarget = hasMimicTarget;
        state.mimicTargetRightSideIndex = mimicTargetRightSideIndex;
        state.mimicTargetComboCode = mimicTargetComboCode;

        return state;
    }

    // 「继续游戏」时恢复装备/拾取 UI 状态。内部刷新与 SyncLinkedSkills 不会触发存档写入。
    public void ApplyState(PickupSaveState state)
    {
        if (state == null)
        {
            return;
        }

        unlockedItems.Clear();
        if (state.unlockedItems != null)
        {
            for (int i = 0; i < state.unlockedItems.Count; i++)
            {
                unlockedItems.Add(state.unlockedItems[i]);
            }
        }

        EnsureEquippedStateArrays();
        for (int i = 0; i < equippedSlotItems.Length; i++)
        {
            equippedSlotItems[i] = (state.equippedSlotItems != null && i < state.equippedSlotItems.Count)
                ? state.equippedSlotItems[i]
                : default(PickupItemId);
            equippedSlotOccupied[i] = (state.equippedSlotOccupied != null && i < state.equippedSlotOccupied.Count)
                && state.equippedSlotOccupied[i];
        }

        fifthEquippedSlotUnlocked = state.fifthEquippedSlotUnlocked;
        hasMimicTarget = state.hasMimicTarget;
        mimicTargetRightSideIndex = state.mimicTargetRightSideIndex;
        mimicTargetComboCode = state.mimicTargetComboCode;
        selectingMimicTarget = false;

        suppressSaveOnSync = true;
        RefreshUnlockedSlots();
        RefreshEquippedSlots();
        SyncBoltSpend();
        SyncLinkedSkills();
        suppressSaveOnSync = false;
    }

    private void SyncLinkedSkills()
    {
        if (!syncLinkedSkillsToPlayer)
        {
            return;
        }

        ResolveSkillReferences();

        if (player == null || skillDatabase == null)
        {
            return;
        }

        if (player.unlockedSkills == null)
        {
            player.unlockedSkills = new List<SkillBase>();
        }

        // 每次装备栏变化时重建装备技能快照，保证 PlayerCC.equippedSkills 与 UI 装备槽保持一致。
        equippedSkillSnapshot.Clear();

        if (removePreviousLinkedSkills)
        {
            for (int i = 0; i < appliedLinkedSkills.Count; i++)
            {
                SkillBase oldSkill = appliedLinkedSkills[i];
                if (oldSkill != null)
                {
                    player.unlockedSkills.Remove(oldSkill);
                }
            }

            appliedLinkedSkills.Clear();
        }

        AddStandaloneSkillForSlot(1);
        AddLinkedSkillForPair(2, 3);
        AddLinkedSkillForPair(4, 5);

        // 把装备槽推导出的技能同步到 PlayerCC。动画脚本不再需要读取 UI 私有状态，只读取 PlayerCC.equippedSkills。
        player.SetEquippedSkills(equippedSkillSnapshot);

        if (!suppressSaveOnSync)
        {
            SaveCurrentState();
        }
    }

    private void SyncBoltSpend()
    {
        ResolveBoltPanel();
        if (boltPanel == null)
        {
            return;
        }

        boltPanel.SetSpentCount(CalculateEquippedBoltCost());
    }

    private void AddLinkedSkillForPair(int mainSlotNumber, int subSlotNumber)
    {
        if (!TryGetEquippedEntry(mainSlotNumber, out PickupUiEntry mainEntry))
        {
            return;
        }

        bool hasSubEntry = TryGetEquippedEntry(subSlotNumber, out PickupUiEntry subEntry);
        if (!TryBuildLinkedSkillLookup(mainEntry, hasSubEntry ? subEntry : null, out string exactId, out string prefix))
        {
            return;
        }

        SkillBase skill = FindSkill(exactId, prefix);

        if (skill == null)
        {
            Debug.LogWarning($"没有在 SkillDatabase 中找到联动技能：{exactId} 或前缀 {prefix}", this);
            return;
        }

        if (!player.unlockedSkills.Contains(skill))
        {
            player.unlockedSkills.Add(skill);
            appliedLinkedSkills.Add(skill);
        }

        // 无论该技能之前是否已在 unlockedSkills 中，都应该进入当前装备快照；unlocked 与 equipped 是两个不同生命周期的集合。
        if (!equippedSkillSnapshot.Contains(skill))
        {
            equippedSkillSnapshot.Add(skill);
        }
    }

    private void AddStandaloneSkillForSlot(int slotNumber)
    {
        if (!TryGetEquippedEntry(slotNumber, out PickupUiEntry entry))
        {
            return;
        }

        int mainIndex = GetEffectiveRightSideIndex(entry);
        if (mainIndex < 1 || mainIndex > 4)
        {
            return;
        }

        string mainCode = GetEffectiveComboCode(entry);
        string prefix = mainIndex + "0-";
        string exactId = BuildLinkedSkillId(prefix, mainCode, null);
        SkillBase skill = FindSkill(exactId, prefix);

        if (skill == null)
        {
            Debug.LogWarning($"没有在 SkillDatabase 中找到基础技能：{exactId} 或前缀 {prefix}", this);
            return;
        }

        if (!player.unlockedSkills.Contains(skill))
        {
            player.unlockedSkills.Add(skill);
            appliedLinkedSkills.Add(skill);
        }

        if (!equippedSkillSnapshot.Contains(skill))
        {
            equippedSkillSnapshot.Add(skill);
        }
    }

    private bool TryBuildLinkedSkillLookup(PickupUiEntry mainEntry, PickupUiEntry subEntry, out string exactId, out string prefix)
    {
        exactId = null;
        prefix = null;

        if (mainEntry == null)
        {
            return false;
        }

        int mainIndex = GetEffectiveRightSideIndex(mainEntry);
        int subIndex = subEntry != null ? GetEffectiveRightSideIndex(subEntry) : 0;
        string mainCode = GetEffectiveComboCode(mainEntry);
        string subCode = subEntry != null ? GetEffectiveComboCode(subEntry) : null;

        if (IsMimicIndex(mainIndex))
        {
            return false;
        }

        if (IsMimicIndex(subIndex))
        {
            return false;
        }

        prefix = mainIndex.ToString() + subIndex + "-";
        exactId = BuildLinkedSkillId(prefix, mainCode, subCode);
        return true;
    }

    private Sprite GetEquippedIcon(int equippedIndex, PickupUiEntry entry)
    {
        if (entry == null || !IsMimicIndex(GetRightSideIndex(entry)))
        {
            return entry != null ? entry.icon : null;
        }

        if (!hasMimicTarget)
        {
            return entry.icon;
        }

        Sprite mimicIcon = GetMimicResultIcon(mimicTargetRightSideIndex);
        return mimicIcon != null ? mimicIcon : entry.icon;
    }

    private static bool IsMimicIndex(int rightSideIndex)
    {
        return rightSideIndex == 5;
    }

    private bool TryGetEquippedEntry(int slotNumber, out PickupUiEntry entry)
    {
        entry = null;

        int index = slotNumber - 1;
        if (!IsValidEquippedIndex(index) || !equippedSlotOccupied[index])
        {
            return false;
        }

        PickupItemId itemId = equippedSlotItems[index];
        return entryById.TryGetValue(itemId, out entry) && entry != null;
    }

    private void EnsureEquippedStateArrays()
    {
        int slotCount = equippedSlots != null ? equippedSlots.Length : 0;
        if (equippedSlotItems.Length == slotCount && equippedSlotOccupied.Length == slotCount)
        {
            return;
        }

        PickupItemId[] oldItems = equippedSlotItems;
        bool[] oldOccupied = equippedSlotOccupied;

        equippedSlotItems = new PickupItemId[slotCount];
        equippedSlotOccupied = new bool[slotCount];

        int copyCount = Mathf.Min(slotCount, oldItems.Length, oldOccupied.Length);
        for (int i = 0; i < copyCount; i++)
        {
            equippedSlotItems[i] = oldItems[i];
            equippedSlotOccupied[i] = oldOccupied[i];
        }
    }

    private bool IsValidEquippedIndex(int index)
    {
        return equippedSlotItems != null &&
               equippedSlotOccupied != null &&
               index >= 0 &&
               index < equippedSlotItems.Length &&
               index < equippedSlotOccupied.Length;
    }

    private bool IsEquipped(PickupItemId id)
    {
        for (int i = 0; i < equippedSlotItems.Length; i++)
        {
            if (equippedSlotOccupied[i] && equippedSlotItems[i].Equals(id))
            {
                return true;
            }
        }

        return false;
    }

    private static int GetRightSideIndex(PickupUiEntry entry)
    {
        if (entry == null)
        {
            return 0;
        }

        return entry.rightSideIndex > 0 ? entry.rightSideIndex : ((int)entry.id + 1);
    }

    private void PreviewBoltCost(PickupUiEntry entry)
    {
        int cost = GetBoltCost(entry);
        if (cost <= 0)
        {
            return;
        }

        ResolveBoltPanel();
        if (boltPanel == null)
        {
            return;
        }

        if (cost > boltPanel.AvailableCount)
        {
            boltPanel.ShowInsufficient();
            return;
        }

        boltPanel.PreviewCost(cost);
    }

    private int CalculateEquippedBoltCost()
    {
        int total = 0;
        EnsureEquippedStateArrays();

        for (int i = 0; i < equippedSlotItems.Length; i++)
        {
            if (equippedSlotOccupied[i])
            {
                total += GetEquippedItemBoltCost(equippedSlotItems[i]);
            }
        }

        return total;
    }

    private int GetEquippedItemBoltCost(PickupItemId id)
    {
        return entryById.TryGetValue(id, out PickupUiEntry entry) ? GetBoltCost(entry) : 0;
    }

    private int GetBoltCost(PickupUiEntry entry)
    {
        int rightSideIndex = GetEffectiveRightSideIndex(entry);
        switch (rightSideIndex)
        {
            case 1:
                return Mathf.Max(0, firstSkillBoltCost);
            case 2:
                return Mathf.Max(0, secondSkillBoltCost);
            case 3:
                return Mathf.Max(0, thirdSkillBoltCost);
            case 4:
                return Mathf.Max(0, fourthSkillBoltCost);
            default:
                return 0;
        }
    }

    private void StopSelectedIconFollow()
    {
        if (floatingSelectedIcon != null)
        {
            Destroy(floatingSelectedIcon.gameObject);
            floatingSelectedIcon = null;
            floatingSelectedIconImage = null;
        }
    }

    private void UpdateFloatingIconPosition(bool snapToMouse)
    {
        if (floatingSelectedIcon == null)
        {
            return;
        }

        Canvas canvas = floatingSelectedIcon.GetComponentInParent<Canvas>();
        RectTransform canvasRect = canvas != null ? canvas.transform as RectTransform : null;
        if (canvas == null || canvasRect == null)
        {
            return;
        }

        Camera eventCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;

        // 磁吸：图标压在合法装备槽上时，目标位置切换为该槽中心，而非鼠标位置。
        Vector2 targetLocal = default;
        bool haveTarget = false;
        if (!snapToMouse && dropTargetSlotIndex >= 0 && IsValidEquippedIndex(dropTargetSlotIndex) && equippedSlots[dropTargetSlotIndex] != null)
        {
            RectTransform slotRect = equippedSlots[dropTargetSlotIndex].GetComponent<RectTransform>();
            if (slotRect != null)
            {
                Vector3[] corners = new Vector3[4];
                slotRect.GetWorldCorners(corners);
                Vector3 worldCenter = (corners[0] + corners[2]) * 0.5f;
                Vector2 screenCenter = RectTransformUtility.WorldToScreenPoint(eventCamera, worldCenter);
                haveTarget = RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenCenter, eventCamera, out targetLocal);
            }
        }

        if (!haveTarget)
        {
            if (!TryGetPointerPosition(out Vector2 screenPosition))
            {
                return;
            }

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPosition, eventCamera, out targetLocal))
            {
                return;
            }
        }

        if (snapToMouse || selectedIconFollowSpeedOffset <= 0f)
        {
            floatingSelectedIcon.anchoredPosition = targetLocal;
            return;
        }

        float followT = 1f - Mathf.Exp(-selectedIconFollowSpeedOffset * Time.unscaledDeltaTime);
        floatingSelectedIcon.anchoredPosition = Vector2.Lerp(floatingSelectedIcon.anchoredPosition, targetLocal, followT);
    }

    // ══════════════════════════════════════════════════════
    //  统一指针输入（鼠标 + 触屏）
    // ══════════════════════════════════════════════════════

    private static bool TryGetPointerPosition(out Vector2 screenPos)
    {
        if (Mouse.current != null)
        {
            screenPos = Mouse.current.position.ReadValue();
            return true;
        }

        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
        {
            screenPos = Touchscreen.current.primaryTouch.position.ReadValue();
            return true;
        }

        screenPos = Vector2.zero;
        return false;
    }

    private static bool IsPointerDown()
    {
        return (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            || (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame);
    }

    private static bool IsPointerReleased()
    {
        return (Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame)
            || (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasReleasedThisFrame);
    }

    // ══════════════════════════════════════════════════════
    //  拖动系统
    // ══════════════════════════════════════════════════════

    private void RestorePreviousDragSource()
    {
        if (dragSource == DragSource.None)
        {
            return;
        }

        if (isReturningDrag)
        {
            // 回弹动画被打断，直接恢复源槽位图标
            if (dragSource == DragSource.UnlockSlot)
            {
                RefreshUnlockedSlots();
            }
            else if (dragSource == DragSource.EquippedSlot && IsValidEquippedIndex(dragSourceEquippedIndex))
            {
                equippedSlots[dragSourceEquippedIndex].SetIconVisualVisible(true);
            }
        }
        else
        {
            // 拖拽正在进行中被打断（理论上不应该发生，但保险起见）
            CancelDrag();
        }
    }

    public bool BeginDragFromUnlockSlot(PickupItemId id, Vector3 sourceScreenPosition)
    {
        if (!unlockedItems.Contains(id) || IsEquipped(id))
        {
            return false;
        }

        if (!entryById.TryGetValue(id, out PickupUiEntry entry) || entry == null)
        {
            return false;
        }

        bool isMimic = IsMimicIndex(GetRightSideIndex(entry));

        // 模仿目标选择状态：把"按下 + 微移"识别为点击（避免和拖拽冲突）
        if (selectingMimicTarget)
        {
            CompleteMimicTargetSelection(entry);
            return false;
        }

        // 模仿者在未模仿状态下不能拖动；成功模仿后变成目标技能，可以像其他技能一样装卸
        if (isMimic && !hasMimicTarget)
        {
            return false;
        }

        RestorePreviousDragSource();

        dragSource = DragSource.UnlockSlot;
        dragSourceItem = id;
        dragSourceEquippedIndex = -1;
        dragReturnPosition = sourceScreenPosition;
        isReturningDrag = false;

        StopSelectedIconFollow();
        PreviewBoltCost(entry);
        entry.unlockSlot.SetIconVisualVisible(false);
        StartDragFloatingIcon(entry.unlockSlot);
        return true;
    }

    public bool BeginDragFromEquippedSlot(int equippedIndex, Vector3 sourceScreenPosition)
    {
        if (!IsValidEquippedIndex(equippedIndex) || !equippedSlotOccupied[equippedIndex])
        {
            return false;
        }

        if (!IsEquippedSlotUnlocked(equippedIndex))
        {
            return false;
        }

        PickupItemId itemId = equippedSlotItems[equippedIndex];
        if (!entryById.TryGetValue(itemId, out PickupUiEntry entry) || entry == null)
        {
            return false;
        }

        RestorePreviousDragSource();

        dragSource = DragSource.EquippedSlot;
        dragSourceItem = itemId;
        dragSourceEquippedIndex = equippedIndex;
        dragReturnPosition = sourceScreenPosition;
        isReturningDrag = false;

        equippedSlots[equippedIndex].SetIconVisualVisible(false);
        StartDragFloatingIcon(equippedSlots[equippedIndex]);
        return true;
    }

    private void StartDragFloatingIcon(PickupUISlotView sourceSlot)
    {
        StopSelectedIconFollow();

        if (sourceSlot == null)
        {
            return;
        }

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            return;
        }

        RectTransform canvasRect = canvas.transform as RectTransform;
        if (canvasRect == null)
        {
            return;
        }

        GameObject followObject = new GameObject("Drag Skill Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        followObject.transform.SetParent(canvas.transform, false);
        followObject.transform.SetAsLastSibling();

        floatingSelectedIcon = followObject.GetComponent<RectTransform>();
        floatingSelectedIconImage = followObject.GetComponent<Image>();

        // 获取源槽位的图标
        Sprite followSprite = null;
        Image sourceImage = sourceSlot.GetComponent<Image>();
        if (sourceImage != null && sourceImage.sprite != null)
        {
            followSprite = sourceImage.sprite;
        }

        if (followSprite == null)
        {
            Destroy(followObject);
            return;
        }

        floatingSelectedIconImage.sprite = followSprite;
        floatingSelectedIconImage.raycastTarget = false;
        floatingSelectedIconImage.preserveAspect = true;

        Vector2 iconSize = sourceSlot.GetIconSize();
        if (iconSize.x <= 0f || iconSize.y <= 0f)
        {
            iconSize = new Vector2(64f, 64f);
        }

        floatingSelectedIcon.sizeDelta = iconSize;

        // 立即将图标放置到指针位置，避免在屏幕中心闪一帧
        if (TryGetPointerPosition(out Vector2 screenPos))
        {
            Camera eventCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPos, eventCamera, out Vector2 localPos))
            {
                floatingSelectedIcon.anchoredPosition = localPos;
            }
        }
    }

    private void UpdateDrag()
    {
        if (dragSource == DragSource.None || isReturningDrag)
        {
            return;
        }

        // 安全网：鼠标已松开但 OnEndDrag 未触发（指针离开窗口、Canvas 被遮挡等），
        // 自动触发回弹动画，防止浮动图标永远跟随鼠标
        if (IsPointerReleased())
        {
            AnimateDragReturn();
            return;
        }

        UpdateDropTargetHighlight();
        UpdateFloatingIconPosition(false);
    }

    // 检测鼠标当前压在哪个合法装备槽上，切换磁吸高亮。
    private void UpdateDropTargetHighlight()
    {
        int newTarget = FindDropTargetSlot();
        if (newTarget == dropTargetSlotIndex)
        {
            return;
        }

        if (dropTargetSlotIndex >= 0 && IsValidEquippedIndex(dropTargetSlotIndex) && equippedSlots[dropTargetSlotIndex] != null)
        {
            equippedSlots[dropTargetSlotIndex].SetDropTarget(false);
        }

        dropTargetSlotIndex = newTarget;

        if (dropTargetSlotIndex >= 0 && equippedSlots[dropTargetSlotIndex] != null)
        {
            equippedSlots[dropTargetSlotIndex].SetDropTarget(true);
        }
    }

    private int FindDropTargetSlot()
    {
        if (!TryGetPointerPosition(out Vector2 screenPos))
        {
            return -1;
        }

        for (int i = 0; i < equippedSlots.Length; i++)
        {
            if (equippedSlots[i] == null || i == dragSourceEquippedIndex)
            {
                continue;
            }

            RectTransform slotRect = equippedSlots[i].GetComponent<RectTransform>();
            if (slotRect == null)
            {
                continue;
            }

            if (!RectTransformUtility.RectangleContainsScreenPoint(slotRect, screenPos))
            {
                continue;
            }

            if (!IsEquippedSlotUnlocked(i))
            {
                continue;
            }

            if (!CanEquipItemAtSlot(dragSourceItem, i))
            {
                continue;
            }

            return i;
        }

        return -1;
    }

    private void ClearDropTargetHighlight()
    {
        if (dropTargetSlotIndex >= 0 && IsValidEquippedIndex(dropTargetSlotIndex) && equippedSlots[dropTargetSlotIndex] != null)
        {
            equippedSlots[dropTargetSlotIndex].SetDropTarget(false);
        }
        dropTargetSlotIndex = -1;
    }

    public void EndDrag(Vector2 screenPos)
    {
        if (dragSource == DragSource.None || isReturningDrag)
        {
            return;
        }

        // 详细面板打开时不处理
        if (isDetailPanelOpen)
        {
            CancelDrag();
            return;
        }

        if (!CanModifySkillLoadout())
        {
            AnimateDragReturn();
            return;
        }

        if (dragSource == DragSource.UnlockSlot)
        {
            EndDragFromUnlockSlot(screenPos);
        }
        else if (dragSource == DragSource.EquippedSlot)
        {
            EndDragFromEquippedSlot(screenPos);
        }
    }

    private void EndDragFromUnlockSlot(Vector2 screenPos)
    {
        // 检测是否拖到了某个装备槽上
        for (int i = 0; i < equippedSlots.Length; i++)
        {
            if (equippedSlots[i] == null)
            {
                continue;
            }

            RectTransform slotRect = equippedSlots[i].GetComponent<RectTransform>();
            if (slotRect == null)
            {
                continue;
            }

            if (!RectTransformUtility.RectangleContainsScreenPoint(slotRect, screenPos))
            {
                continue;
            }

            // 找到目标槽位
            if (!IsEquippedSlotUnlocked(i))
            {
                AnimateDragReturn();
                return;
            }

            if (!CanEquipItemAtSlot(dragSourceItem, i))
            {
                Debug.Log("左上角第 5 个技能槽只能装备 10、20、30、40 系列的基础技能。", this);
                AnimateDragReturn();
                return;
            }

            // 检查螺栓消耗
            int itemCost = GetEquippedItemBoltCost(dragSourceItem);
            int replacedCost = equippedSlotOccupied[i] ? GetEquippedItemBoltCost(equippedSlotItems[i]) : 0;

            ResolveBoltPanel();
            if (boltPanel != null && itemCost > boltPanel.AvailableCount + replacedCost)
            {
                boltPanel.ShowInsufficient();
                AnimateDragReturn();
                return;
            }

            // 先卸下目标槽位的旧技能（如果有）
            if (equippedSlotOccupied[i])
            {
                PickupItemId replacedItem = equippedSlotItems[i];
                equippedSlotOccupied[i] = false;
                ItemUnequipped?.Invoke(replacedItem);
            }

            // 装备新技能
            equippedSlotItems[i] = dragSourceItem;
            equippedSlotOccupied[i] = true;

            StopDrag();
            RefreshUnlockedSlots();
            RefreshEquippedSlots();
            SyncBoltSpend();
            SyncLinkedSkills();
            PlaySkillLoadoutSfx(equipSuccessSfx);
            PlayEquipEffect(i);
            ItemEquipped?.Invoke(dragSourceItem);
            return;
        }

        // 未拖到任何装备槽，弹回
        AnimateDragReturn();
    }

    private void EndDragFromEquippedSlot(Vector2 screenPos)
    {
        // 检测是否拖到了另一个装备槽上
        for (int i = 0; i < equippedSlots.Length; i++)
        {
            if (equippedSlots[i] == null || i == dragSourceEquippedIndex)
            {
                continue;
            }

            RectTransform slotRect = equippedSlots[i].GetComponent<RectTransform>();
            if (slotRect == null)
            {
                continue;
            }

            if (!RectTransformUtility.RectangleContainsScreenPoint(slotRect, screenPos))
            {
                continue;
            }

            // 找到目标槽位
            if (!IsEquippedSlotUnlocked(i))
            {
                AnimateDragReturn();
                return;
            }

            if (!CanEquipItemAtSlot(dragSourceItem, i))
            {
                AnimateDragReturn();
                return;
            }

            // 在装备槽之间移动
            MoveEquippedItem(dragSourceEquippedIndex, i);
            return;
        }

        // 检测是否拖到了右侧解锁区域（卸下）
        ResolveHudPanels();
        if (rightPanel != null)
        {
            RectTransform rightRect = rightPanel.GetComponent<RectTransform>();
            if (rightRect != null && RectTransformUtility.RectangleContainsScreenPoint(rightRect, screenPos))
            {
                int sourceIndex = dragSourceEquippedIndex;
                StopDrag();
                UnequipAt(sourceIndex);
                return;
            }
        }

        // 未拖到有效位置，弹回
        AnimateDragReturn();
    }

    private void MoveEquippedItem(int fromIndex, int toIndex)
    {
        if (!IsValidEquippedIndex(fromIndex) || !IsValidEquippedIndex(toIndex))
        {
            AnimateDragReturn();
            return;
        }

        if (!equippedSlotOccupied[fromIndex])
        {
            AnimateDragReturn();
            return;
        }

        PickupItemId sourceItem = equippedSlotItems[fromIndex];

        // 目标槽位有技能时交换，空槽时直接移入
        bool wasSwap = equippedSlotOccupied[toIndex];
        if (wasSwap)
        {
            PickupItemId targetItem = equippedSlotItems[toIndex];
            equippedSlotItems[fromIndex] = targetItem;
            equippedSlotItems[toIndex] = sourceItem;
            // occupied 状态不变，两个槽都仍然有技能
        }
        else
        {
            equippedSlotItems[fromIndex] = default;
            equippedSlotOccupied[fromIndex] = false;
            equippedSlotItems[toIndex] = sourceItem;
            equippedSlotOccupied[toIndex] = true;
        }

        StopDrag();
        RefreshUnlockedSlots();
        RefreshEquippedSlots();
        SyncBoltSpend();
        SyncLinkedSkills();
        PlaySkillLoadoutSfx(equipSuccessSfx);

        // 交换时两槽内容都变了，都播放动效；纯移动只播放目标槽
        PlayEquipEffect(toIndex);
        if (wasSwap)
        {
            PlayEquipEffect(fromIndex);
        }
    }

    // 在指定装备槽对应的 image 物体的子 prefab 上播放"单个圆形旋转"动效。
    // 从 image 物体的子物体中查找 Animator，触发 "Reveal" 动画，播完后隐藏子物体。
    private void PlayEquipEffect(int slotIndex)
    {
        if (!IsValidEquippedIndex(slotIndex))
        {
            return;
        }

        if (equippedSlotImages == null || slotIndex >= equippedSlotImages.Length)
        {
            return;
        }

        GameObject imageObj = equippedSlotImages[slotIndex];
        if (imageObj == null)
        {
            return;
        }

        // 从 image 物体的子物体中查找 Animator（includeInactive=true 确保能找到被隐藏的子 prefab）
        Animator anim = imageObj.GetComponentInChildren<Animator>(true);
        if (anim == null)
        {
            return;
        }

        // 重新激活子 prefab 物体（上次播完可能已被隐藏），再触发旋转动画
        anim.gameObject.SetActive(true);
        anim.SetTrigger("Reveal");
        StartCoroutine(HideEquipEffectWhenDone(anim));
    }

    // 等待 Animator 进入旋转状态并播完后，隐藏物体。
    private IEnumerator HideEquipEffectWhenDone(Animator anim)
    {
        int stateHash = Animator.StringToHash("单技能圆形旋转锁定");

        // 先等进入旋转状态（过渡可能需要一帧）
        int waitFrames = 0;
        while (!anim.GetCurrentAnimatorStateInfo(0).shortNameHash.Equals(stateHash))
        {
            waitFrames++;
            if (waitFrames > 30)
            {
                // 安全兜底：长时间未进入状态则放弃，避免协程泄漏
                yield break;
            }
            yield return null;
        }

        // 在旋转状态中等到 normalizedTime >= 1（播完）
        while (anim.GetCurrentAnimatorStateInfo(0).shortNameHash.Equals(stateHash) &&
               anim.GetCurrentAnimatorStateInfo(0).normalizedTime < 1f)
        {
            yield return null;
        }

        anim.gameObject.SetActive(false);
    }

    private void CancelDrag()
    {
        // 恢复源槽位图标
        if (dragSource == DragSource.UnlockSlot)
        {
            RefreshUnlockedSlots();
        }
        else if (dragSource == DragSource.EquippedSlot && IsValidEquippedIndex(dragSourceEquippedIndex))
        {
            equippedSlots[dragSourceEquippedIndex].SetIconVisualVisible(true);
        }

        ClearDropTargetHighlight();
        StopSelectedIconFollow();
        dragSource = DragSource.None;
        isReturningDrag = false;
    }

    private void AnimateDragReturn()
    {
        isReturningDrag = true;
        ClearDropTargetHighlight();

        // 隐藏源槽位图标，等动画完成后恢复
        if (dragSource == DragSource.UnlockSlot)
        {
            // unlock 槽的图标已通过浮动图标显示，保持隐藏
        }
    }

    private void TickDragReturn()
    {
        if (!isReturningDrag || floatingSelectedIcon == null)
        {
            return;
        }

        Canvas canvas = floatingSelectedIcon.GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            // Canvas 丢失，直接清理
            if (dragSource == DragSource.UnlockSlot)
            {
                RefreshUnlockedSlots();
            }
            else if (dragSource == DragSource.EquippedSlot && IsValidEquippedIndex(dragSourceEquippedIndex))
            {
                equippedSlots[dragSourceEquippedIndex].SetIconVisualVisible(true);
            }

            ClearDropTargetHighlight();
            StopSelectedIconFollow();
            dragSource = DragSource.None;
            isReturningDrag = false;
            return;
        }

        RectTransform canvasRect = canvas.transform as RectTransform;
        Camera eventCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, dragReturnPosition, eventCamera, out Vector2 targetLocal))
        {
            return;
        }

        // 平滑 Lerp 回原始位置
        float returnSpeed = 12f;
        float step = 1f - Mathf.Exp(-returnSpeed * Time.unscaledDeltaTime);
        floatingSelectedIcon.anchoredPosition = Vector2.Lerp(floatingSelectedIcon.anchoredPosition, targetLocal, step);

        // 到达阈值时完成
        float distSq = (floatingSelectedIcon.anchoredPosition - targetLocal).sqrMagnitude;
        if (distSq < 1f)
        {
            if (dragSource == DragSource.UnlockSlot)
            {
                RefreshUnlockedSlots();
            }
            else if (dragSource == DragSource.EquippedSlot && IsValidEquippedIndex(dragSourceEquippedIndex))
            {
                equippedSlots[dragSourceEquippedIndex].SetIconVisualVisible(true);
            }

            ClearDropTargetHighlight();
            StopSelectedIconFollow();
            dragSource = DragSource.None;
            isReturningDrag = false;
        }
    }

    private void StopDrag()
    {
        ClearDropTargetHighlight();
        StopSelectedIconFollow();
        dragSource = DragSource.None;
        isReturningDrag = false;
    }

    private void BeginMimicTargetSelection()
    {
        if (selectingMimicTarget)
        {
            return;
        }

        StopSelectedIconFollow();
        selectingMimicTarget = true;
        ResolveBoltPanel();
        if (boltPanel != null)
        {
            boltPanel.ClearPreview();
        }

        RefreshUnlockedSlots();
        PlaySkillLoadoutSfx(unlockedSkillSelectSfx);
        if (constantSoundAudioSource != null)
        {
            constantSoundAudioSource.Play();
        }
    }

    private void CompleteMimicTargetSelection(PickupUiEntry targetEntry)
    {
        if (targetEntry == null)
        {
            return;
        }

        if (!CanModifySkillLoadout())
        {
            return;
        }

        int targetIndex = GetRightSideIndex(targetEntry);
        if (IsMimicIndex(targetIndex))
        {
            return;
        }

        mimicTargetRightSideIndex = targetIndex;
        mimicTargetComboCode = targetEntry.comboCode;
        hasMimicTarget = true;
        selectingMimicTarget = false;

        PlayMimicSfx(mimicSuccessSfx);
        if (constantSoundAudioSource != null)
        {
            constantSoundAudioSource.Stop();
        }

        RefreshUnlockedSlots();
        RefreshEquippedSlots();
        SyncBoltSpend();
        SyncLinkedSkills();
    }

    private void ClearMimicTarget(bool requireGround = true)
    {
        if (requireGround && !CanModifySkillLoadout())
        {
            return;
        }

        hasMimicTarget = false;
        mimicTargetRightSideIndex = 0;
        mimicTargetComboCode = null;
        selectingMimicTarget = false;
        StopSelectedIconFollow();

        // 退出模仿时停止音效
        if (constantSoundAudioSource != null)
        {
            constantSoundAudioSource.Stop();
        }

        ResolveBoltPanel();
        if (boltPanel != null)
        {
            boltPanel.ClearPreview();
        }

        RefreshUnlockedSlots();
        RefreshEquippedSlots();
        SyncBoltSpend();
        SyncLinkedSkills();
    }

    private Sprite GetUnlockIcon(PickupUiEntry entry)
    {
        if (entry == null)
        {
            return null;
        }

        int rightSideIndex = GetRightSideIndex(entry);
        if (IsMimicIndex(rightSideIndex))
        {
            if (hasMimicTarget)
            {
                Sprite mimicIcon = GetMimicResultIcon(mimicTargetRightSideIndex);
                return mimicIcon != null ? mimicIcon : entry.icon;
            }

            return entry.icon;
        }

        if (selectingMimicTarget)
        {
            Sprite darkIcon = GetMimicTargetDarkIcon(rightSideIndex);
            return darkIcon != null ? darkIcon : entry.icon;
        }

        return entry.icon;
    }

    private int GetEffectiveRightSideIndex(PickupUiEntry entry)
    {
        int rightSideIndex = GetRightSideIndex(entry);
        if (IsMimicIndex(rightSideIndex) && hasMimicTarget)
        {
            return mimicTargetRightSideIndex;
        }

        return rightSideIndex;
    }

    private string GetEffectiveComboCode(PickupUiEntry entry)
    {
        if (entry == null)
        {
            return null;
        }

        if (IsMimicIndex(GetRightSideIndex(entry)) && hasMimicTarget)
        {
            return mimicTargetComboCode;
        }

        return entry.comboCode;
    }

    private bool CanModifySkillLoadout()
    {
        ResolveSkillReferences();
        if (player != null && player.CanModifySkillLoadout())
        {
            return true;
        }

        ShowWarnPanel();
        Debug.Log("只有站在 Nature 标签的物体上时，才能装备或卸下技能。", this);
        return false;
    }

    private bool IsMimicItem(PickupItemId id)
    {
        return entryById.TryGetValue(id, out PickupUiEntry entry) &&
               entry != null &&
               IsMimicIndex(GetRightSideIndex(entry));
    }

    private Sprite GetMimicTargetDarkIcon(int rightSideIndex)
    {
        int iconIndex = rightSideIndex - 1;
        if (mimicTargetDarkIcons == null || iconIndex < 0 || iconIndex >= mimicTargetDarkIcons.Length)
        {
            return null;
        }

        return mimicTargetDarkIcons[iconIndex];
    }

    private Sprite GetMimicResultIcon(int rightSideIndex)
    {
        return GetMimicTargetDarkIcon(rightSideIndex);
    }

    private void PlayMimicSfx(AudioClip clip)
    {
        PlaySfx(clip, mimicSfxVolume);
    }

    private void PlaySkillLoadoutSfx(AudioClip clip)
    {
        PlaySfx(clip, skillLoadoutSfxVolume);
    }

    private void PlaySfx(AudioClip clip, float volume)
    {
        if (clip == null)
        {
            return;
        }

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(clip, volume);
            return;
        }

        if (fallbackAudioSource != null)
        {
            fallbackAudioSource.PlayOneShot(clip, volume);
        }
    }

    private SkillBase FindSkill(string exactId, string prefix)
    {
        if (skillDatabase == null || skillDatabase.allSkills == null)
        {
            return null;
        }

        for (int i = 0; i < skillDatabase.allSkills.Count; i++)
        {
            SkillBase skill = skillDatabase.allSkills[i];
            if (skill != null && string.Equals(GetSkillKey(skill), exactId, StringComparison.OrdinalIgnoreCase))
            {
                return skill;
            }
        }

        if (!allowPrefixFallback)
        {
            return null;
        }

        for (int i = 0; i < skillDatabase.allSkills.Count; i++)
        {
            SkillBase skill = skillDatabase.allSkills[i];
            if (skill != null && GetSkillKey(skill).StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return skill;
            }
        }

        return null;
    }

    private static string BuildLinkedSkillId(string prefix, string firstCode, string secondCode)
    {
        string a = string.IsNullOrWhiteSpace(firstCode) ? "x" : firstCode.Trim().ToLowerInvariant();
        string b = string.IsNullOrWhiteSpace(secondCode) ? "x" : secondCode.Trim().ToLowerInvariant();
        return prefix + a + b;
    }

    private static string GetSkillKey(SkillBase skill)
    {
        if (skill == null)
        {
            return string.Empty;
        }

        return string.IsNullOrWhiteSpace(skill.skillID) ? skill.name : skill.skillID.Trim();
    }
}
