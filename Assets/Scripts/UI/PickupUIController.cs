using System;
using System.Collections.Generic;
using Skills;
using UnityEngine;

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
    }

    public static PickupUIController Instance { get; private set; }

    [Header("5 种拾取物 UI")]
    [SerializeField] private PickupUiEntry[] entries = new PickupUiEntry[5];

    [Header("左上角装备栏")]
    [Tooltip("按界面位置顺序拖入左上角 5 个装备槽。装备不会自动补位，槽位允许空缺。")]
    [SerializeField] private PickupUISlotView[] equippedSlots = new PickupUISlotView[5];

    [Header("行为")]
    [SerializeField] private bool hideLockedSlotsOnStart = true;

    [Header("联动技能")]
    [SerializeField] private PlayerCC player;
    [SerializeField] private SkillDatabase skillDatabase;
    [SerializeField] private bool syncLinkedSkillsToPlayer = true;
    [SerializeField] private bool removePreviousLinkedSkills = true;
    [SerializeField] private bool allowPrefixFallback = true;

    public event Action<PickupItemId> ItemUnlocked;
    public event Action<PickupItemId> ItemEquipped;
    public event Action<PickupItemId> ItemUnequipped;

    private readonly Dictionary<PickupItemId, PickupUiEntry> entryById = new Dictionary<PickupItemId, PickupUiEntry>();
    private readonly HashSet<PickupItemId> unlockedItems = new HashSet<PickupItemId>();
    private PickupItemId[] equippedSlotItems = Array.Empty<PickupItemId>();
    private bool[] equippedSlotOccupied = Array.Empty<bool>();
    private bool hasSelectedUnlockItem;
    private PickupItemId selectedUnlockItem;
    private readonly List<SkillBase> appliedLinkedSkills = new List<SkillBase>();

    // Lry的修改：装备槽组合后真正生效的 SkillBase 快照。它会同步到 PlayerCC.equippedSkills，供动画层读取当前 loadout。
    private readonly List<SkillBase> equippedSkillSnapshot = new List<SkillBase>();

    public bool HasSelectedUnlockItem => hasSelectedUnlockItem;

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
        ResolveSkillReferences();
        RefreshAllSlots();
        RefreshUnlockedSlots();
        RefreshEquippedSlots();
        SyncLinkedSkills();
    }

    private void OnDestroy()
    {
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

    public void SelectForEquip(PickupItemId id)
    {
        if (!unlockedItems.Contains(id) || IsEquipped(id))
        {
            return;
        }

        if (!entryById.ContainsKey(id))
        {
            return;
        }

        if (hasSelectedUnlockItem && selectedUnlockItem.Equals(id))
        {
            ClearSelectedUnlockItem();
            return;
        }

        hasSelectedUnlockItem = true;
        selectedUnlockItem = id;
        RefreshUnlockedSlots();
    }

    public void OnEquippedSlotClicked(int equippedIndex)
    {
        if (!IsValidEquippedIndex(equippedIndex))
        {
            return;
        }

        if (hasSelectedUnlockItem)
        {
            EquipSelectedAt(equippedIndex);
            return;
        }

        if (equippedSlotOccupied[equippedIndex])
        {
            UnequipAt(equippedIndex);
        }
    }

    private void EquipSelectedAt(int equippedIndex)
    {
        if (!hasSelectedUnlockItem || !IsValidEquippedIndex(equippedIndex))
        {
            return;
        }

        PickupItemId itemToEquip = selectedUnlockItem;
        if (!unlockedItems.Contains(itemToEquip) || IsEquipped(itemToEquip))
        {
            ClearSelectedUnlockItem();
            return;
        }

        bool replacedExistingItem = equippedSlotOccupied[equippedIndex];
        PickupItemId replacedItem = replacedExistingItem ? equippedSlotItems[equippedIndex] : default(PickupItemId);

        equippedSlotItems[equippedIndex] = itemToEquip;
        equippedSlotOccupied[equippedIndex] = true;

        ClearSelectedUnlockItem();
        RefreshEquippedSlots();
        SyncLinkedSkills();

        if (replacedExistingItem)
        {
            ItemUnequipped?.Invoke(replacedItem);
        }

        ItemEquipped?.Invoke(itemToEquip);
    }

    public void UnequipAt(int equippedIndex)
    {
        if (!IsValidEquippedIndex(equippedIndex) || !equippedSlotOccupied[equippedIndex])
        {
            return;
        }

        PickupItemId removedItem = equippedSlotItems[equippedIndex];
        equippedSlotOccupied[equippedIndex] = false;

        RefreshUnlockedSlots();
        RefreshEquippedSlots();
        SyncLinkedSkills();

        ItemUnequipped?.Invoke(removedItem);
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
                entry.unlockSlot.InitializeUnlockSlot(this, entry.id, entry.icon);
                entry.unlockSlot.SetSelected(hasSelectedUnlockItem && selectedUnlockItem.Equals(entry.id));
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
            if (!equippedSlotOccupied[i])
            {
                slot.ClearIcon();
                continue;
            }

            PickupItemId itemId = equippedSlotItems[i];
            if (entryById.TryGetValue(itemId, out PickupUiEntry entry))
            {
                slot.SetItem(itemId, entry.icon);
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

        // Lry的修改：把装备槽推导出的技能同步到 PlayerCC。动画脚本不再需要读取 UI 私有状态，只读取 PlayerCC.equippedSkills。
        player.SetEquippedSkills(equippedSkillSnapshot);
    }

    private void AddLinkedSkillForPair(int mainSlotNumber, int subSlotNumber)
    {
        if (!TryGetEquippedEntry(mainSlotNumber, out PickupUiEntry mainEntry))
        {
            return;
        }

        bool hasSubEntry = TryGetEquippedEntry(subSlotNumber, out PickupUiEntry subEntry);
        if (!hasSubEntry)
        {
            subEntry = mainEntry;
        }

        string prefix = GetRightSideIndex(mainEntry).ToString() + GetRightSideIndex(subEntry) + "-";
        string exactId = BuildLinkedSkillId(prefix, mainEntry.comboCode, subEntry.comboCode);
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

    private void ClearSelectedUnlockItem()
    {
        hasSelectedUnlockItem = false;
        RefreshUnlockedSlots();
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
