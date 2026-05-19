using System;
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

    [SerializeField] private AudioSource fallbackAudioSource;
    [SerializeField] private AudioSource constantSoundAudioSource;

    [Header("左上角装备栏")]
    [Tooltip("按界面位置顺序拖入左上角 5 个装备槽。装备不会自动补位，槽位允许空缺。")]
    [SerializeField] private PickupUISlotView[] equippedSlots = new PickupUISlotView[5];

    [Header("第五装备槽")]
    [Tooltip("左上角第 5 个装备槽是否已解锁。未解锁时不能安装技能，可通过 Trigger Pickup 按 E 解锁。")]
    [SerializeField] private bool fifthEquippedSlotUnlocked;

    [Header("行为")]
    [SerializeField] private bool hideLockedSlotsOnStart = true;

    [Header("HUD 显示")]
    [SerializeField] private GameObject leftPanel;
    [SerializeField] private GameObject rightPanel;
    [SerializeField] private GameObject boltHudPanel;

    [Header("技能详情")]
    [SerializeField] private GameObject detailPanel;

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
    private bool hasSelectedUnlockItem;
    private PickupItemId selectedUnlockItem;
    private bool selectingMimicTarget;
    private bool hasMimicTarget;
    private int mimicTargetRightSideIndex;
    private string mimicTargetComboCode;
    private readonly List<SkillBase> appliedLinkedSkills = new List<SkillBase>();
    private InputAction toggleHudAction;
    private InputAction closeDetailAction;
    private bool isHudVisible = true;
    private bool isDetailPanelOpen;
    private PickupItemId currentDetailItem;
    private GameObject activeDetailPanel;
    private int detailPanelClosedFrame = -1;
    private RectTransform floatingSelectedIcon;
    private Image floatingSelectedIconImage;
    private PickupUISlotView selectedUnlockSlotView;
    private int selectionStartedFrame = -1;
    private int lastSkillUiClickFrame = -1;

    // Lry的修改：装备槽组合后真正生效的 SkillBase 快照。它会同步到 PlayerCC.equippedSkills，供动画层读取当前 loadout。
    private readonly List<SkillBase> equippedSkillSnapshot = new List<SkillBase>();

    public bool HasSelectedUnlockItem => hasSelectedUnlockItem;
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
        ResolveHudPanels();
        ResolveDetailPanel();
        isHudVisible = true;
        ApplyHudVisibility();
        HideDetailPanel();
        ResolveSkillReferences();
        ResolveBoltPanel();
        RefreshAllSlots();
        RefreshUnlockedSlots();
        RefreshEquippedSlots();
        SyncBoltSpend();
        SyncLinkedSkills();
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
        UpdateSelectedIconFollow();
    }

    private void LateUpdate()
    {
        CancelSelectedSkillWhenClickingElsewhere();
    }

    public void OnUnlockSlotClicked(PickupItemId id, int clickCount)
    {
        lastSkillUiClickFrame = Time.frameCount;

        if (!unlockedItems.Contains(id) || IsEquipped(id))
        {
            return;
        }

        if (!entryById.TryGetValue(id, out PickupUiEntry entry))
        {
            return;
        }

        int rightSideIndex = GetRightSideIndex(entry);
        if (IsMimicIndex(rightSideIndex))
        {
            if (clickCount >= 2 && hasMimicTarget)
            {
                if (!CanModifySkillLoadout())
                {
                    return;
                }

                ClearMimicTarget();
                PlayMimicSfx(mimicExitSfx);
                return;
            }

            if (!hasMimicTarget)
            {
                BeginMimicTargetSelection();
                return;
            }

            SelectForEquip(id);
            return;
        }

        if (selectingMimicTarget)
        {
            CompleteMimicTargetSelection(entry);
            return;
        }

        SelectForEquip(id);
    }

    public void SelectForEquip(PickupItemId id)
    {
        if (!unlockedItems.Contains(id) || IsEquipped(id))
        {
            return;
        }

        if (!entryById.TryGetValue(id, out PickupUiEntry entry))
        {
            return;
        }

        if (IsMimicIndex(GetRightSideIndex(entry)) && !hasMimicTarget)
        {
            BeginMimicTargetSelection();
            return;
        }

        if (hasSelectedUnlockItem && selectedUnlockItem.Equals(id))
        {
            ClearSelectedUnlockItem();
            return;
        }

        PreviewBoltCost(entry);

        hasSelectedUnlockItem = true;
        selectedUnlockItem = id;
        selectingMimicTarget = false;
        PlaySkillLoadoutSfx(unlockedSkillSelectSfx);
        RefreshUnlockedSlots();
        StartSelectedIconFollow(entry);
    }

    public void OnEquippedSlotClicked(int equippedIndex, int clickCount = 1)
    {
        lastSkillUiClickFrame = Time.frameCount;

        if (!IsValidEquippedIndex(equippedIndex))
        {
            CancelSelectedUnlockItem();
            return;
        }

        if (!IsEquippedSlotUnlocked(equippedIndex))
        {
            Debug.Log("左上角第 5 个技能槽尚未解锁，需要先拾取对应道具。", this);
            CancelSelectedUnlockItem();
            return;
        }

        if (clickCount >= 2 && equippedSlotOccupied[equippedIndex] && IsMimicItem(equippedSlotItems[equippedIndex]) && hasMimicTarget)
        {
            if (!CanModifySkillLoadout())
            {
                return;
            }

            ClearMimicTarget();
            PlayMimicSfx(mimicExitSfx);
            return;
        }

        if (hasSelectedUnlockItem)
        {
            if (!EquipSelectedAt(equippedIndex))
            {
                CancelSelectedUnlockItem();
            }

            return;
        }

        if (equippedSlotOccupied[equippedIndex])
        {
            UnequipAt(equippedIndex);
        }
    }

    private bool EquipSelectedAt(int equippedIndex)
    {
        if (!hasSelectedUnlockItem || !IsValidEquippedIndex(equippedIndex))
        {
            return false;
        }

        if (!IsEquippedSlotUnlocked(equippedIndex))
        {
            Debug.Log("左上角第 5 个技能槽尚未解锁，需要先拾取对应道具。", this);
            return false;
        }

        if (!CanModifySkillLoadout())
        {
            return false;
        }

        PickupItemId itemToEquip = selectedUnlockItem;
        if (!unlockedItems.Contains(itemToEquip) || IsEquipped(itemToEquip))
        {
            ClearSelectedUnlockItem();
            return false;
        }

        if (!CanEquipItemAtSlot(itemToEquip, equippedIndex))
        {
            Debug.Log("左上角第 5 个技能槽只能装备 10、20、30、40 系列的基础技能。", this);
            return false;
        }

        bool replacedExistingItem = equippedSlotOccupied[equippedIndex];
        PickupItemId replacedItem = replacedExistingItem ? equippedSlotItems[equippedIndex] : default(PickupItemId);
        int replacedCost = replacedExistingItem ? GetEquippedItemBoltCost(replacedItem) : 0;
        int itemCost = GetEquippedItemBoltCost(itemToEquip);

        ResolveBoltPanel();
        if (boltPanel != null && itemCost > boltPanel.AvailableCount + replacedCost)
        {
            boltPanel.ShowInsufficient();
            RefreshUnlockedSlots();
            return false;
        }

        equippedSlotItems[equippedIndex] = itemToEquip;
        equippedSlotOccupied[equippedIndex] = true;

        ClearSelectedUnlockItem();
        RefreshEquippedSlots();
        SyncBoltSpend();
        SyncLinkedSkills();
        PlaySkillLoadoutSfx(equipSuccessSfx);

        if (replacedExistingItem)
        {
            ItemUnequipped?.Invoke(replacedItem);
        }

        ItemEquipped?.Invoke(itemToEquip);
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
                bool selected = hasSelectedUnlockItem && selectedUnlockItem.Equals(entry.id);
                if (selectingMimicTarget && IsMimicIndex(GetRightSideIndex(entry)))
                {
                    selected = true;
                }

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

        // Lry的修改：每次装备栏变化时重建装备技能快照，保证 PlayerCC.equippedSkills 与 UI 装备槽保持一致。
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

        AddLinkedSkillForPair(1, 2);
        AddLinkedSkillForPair(3, 4);
        AddStandaloneSkillForSlot(5);

        // Lry的修改：把装备槽推导出的技能同步到 PlayerCC。动画脚本不再需要读取 UI 私有状态，只读取 PlayerCC.equippedSkills。
        player.SetEquippedSkills(equippedSkillSnapshot);
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

        // Lry的修改：无论该技能之前是否已在 unlockedSkills 中，都应该进入当前装备快照；unlocked 与 equipped 是两个不同生命周期的集合。
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

    private void ClearSelectedUnlockItem()
    {
        hasSelectedUnlockItem = false;
        StopSelectedIconFollow();
        ResolveBoltPanel();
        if (boltPanel != null)
        {
            boltPanel.ClearPreview();
        }

        RefreshUnlockedSlots();
    }

    private void CancelSelectedUnlockItem()
    {
        if (!hasSelectedUnlockItem)
        {
            return;
        }

        ClearSelectedUnlockItem();
    }

    private void StartSelectedIconFollow(PickupUiEntry entry)
    {
        StopSelectedIconFollow();

        Sprite followSprite = GetUnlockIcon(entry);
        if (entry == null || entry.unlockSlot == null || followSprite == null)
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

        GameObject followObject = new GameObject("Selected Skill Follow Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        followObject.transform.SetParent(canvas.transform, false);
        followObject.transform.SetAsLastSibling();

        floatingSelectedIcon = followObject.GetComponent<RectTransform>();
        floatingSelectedIconImage = followObject.GetComponent<Image>();
        floatingSelectedIconImage.sprite = followSprite;
        floatingSelectedIconImage.raycastTarget = false;
        floatingSelectedIconImage.preserveAspect = true;

        Vector2 iconSize = entry.unlockSlot.GetIconSize();
        if (iconSize.x <= 0f || iconSize.y <= 0f)
        {
            iconSize = new Vector2(64f, 64f);
        }

        floatingSelectedIcon.sizeDelta = iconSize;
        selectedUnlockSlotView = entry.unlockSlot;
        selectedUnlockSlotView.SetIconVisualVisible(false);
        selectionStartedFrame = Time.frameCount;
        UpdateSelectedIconFollow();
    }

    private void StopSelectedIconFollow()
    {
        if (selectedUnlockSlotView != null)
        {
            selectedUnlockSlotView.SetIconVisualVisible(true);
            selectedUnlockSlotView = null;
        }

        if (floatingSelectedIcon != null)
        {
            Destroy(floatingSelectedIcon.gameObject);
            floatingSelectedIcon = null;
            floatingSelectedIconImage = null;
        }

        selectionStartedFrame = -1;
    }

    private void UpdateSelectedIconFollow()
    {
        if (!hasSelectedUnlockItem || floatingSelectedIcon == null || Mouse.current == null)
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
        Vector2 screenPosition = Mouse.current.position.ReadValue();
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPosition, eventCamera, out Vector2 localPosition))
        {
            floatingSelectedIcon.anchoredPosition = localPosition;
        }
    }

    private void CancelSelectedSkillWhenClickingElsewhere()
    {
        if (!hasSelectedUnlockItem || Mouse.current == null || !Mouse.current.leftButton.wasReleasedThisFrame)
        {
            return;
        }

        if (Time.frameCount <= selectionStartedFrame || lastSkillUiClickFrame == Time.frameCount)
        {
            return;
        }

        CancelSelectedUnlockItem();
    }

    private void BeginMimicTargetSelection()
    {
        hasSelectedUnlockItem = false;
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
        hasSelectedUnlockItem = false;

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

    private void ClearMimicTarget()
    {
        if (!CanModifySkillLoadout())
        {
            return;
        }

        hasMimicTarget = false;
        mimicTargetRightSideIndex = 0;
        mimicTargetComboCode = null;
        selectingMimicTarget = false;
        hasSelectedUnlockItem = false;
        StopSelectedIconFollow();

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
