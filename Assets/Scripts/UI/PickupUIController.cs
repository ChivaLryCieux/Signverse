using System;
using System.Collections.Generic;
using UnityEngine;

public class PickupUIController : MonoBehaviour
{
    [Serializable]
    public class PickupUiEntry
    {
        public PickupItemId id;
        public string displayName;
        public Sprite icon;
        [Tooltip("右上角用于点击装备的槽位。")]
        public PickupUISlotView unlockSlot;
    }

    public static PickupUIController Instance { get; private set; }

    [Header("5 种拾取物 UI")]
    [SerializeField] private PickupUiEntry[] entries = new PickupUiEntry[5];

    [Header("左上角装备栏")]
    [Tooltip("按顺序拖入左上角 5 个装备槽。")]
    [SerializeField] private PickupUISlotView[] equippedSlots = new PickupUISlotView[5];

    [Header("行为")]
    [SerializeField] private bool hideLockedSlotsOnStart = true;

    public event Action<PickupItemId> ItemUnlocked;
    public event Action<PickupItemId> ItemEquipped;
    public event Action<PickupItemId> ItemUnequipped;

    private readonly Dictionary<PickupItemId, PickupUiEntry> entryById = new Dictionary<PickupItemId, PickupUiEntry>();
    private readonly HashSet<PickupItemId> unlockedItems = new HashSet<PickupItemId>();
    private readonly List<PickupItemId> equippedItems = new List<PickupItemId>(5);

    public IReadOnlyList<PickupItemId> EquippedItems => equippedItems;

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
        RefreshAllSlots();
        RefreshUnlockedSlots();
        RefreshEquippedSlots();
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

    public void Equip(PickupItemId id)
    {
        if (!unlockedItems.Contains(id) || equippedItems.Contains(id))
        {
            return;
        }

        int maxEquippedCount = equippedSlots != null ? equippedSlots.Length : 0;
        if (equippedItems.Count >= maxEquippedCount)
        {
            Debug.LogWarning("左上角装备栏已满，无法继续装备。", this);
            return;
        }

        if (!entryById.ContainsKey(id))
        {
            return;
        }

        equippedItems.Add(id);
        RefreshUnlockedSlots();
        RefreshEquippedSlots();

        ItemEquipped?.Invoke(id);
    }

    public void UnequipAt(int equippedIndex)
    {
        if (equippedIndex < 0 || equippedIndex >= equippedItems.Count)
        {
            return;
        }

        PickupItemId removedItem = equippedItems[equippedIndex];
        equippedItems.RemoveAt(equippedIndex);

        RefreshUnlockedSlots();
        RefreshEquippedSlots();

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

            bool visible = unlockedItems.Contains(entry.id) && !equippedItems.Contains(entry.id);
            if (!hideLockedSlotsOnStart && !unlockedItems.Contains(entry.id))
            {
                visible = true;
            }

            entry.unlockSlot.gameObject.SetActive(visible);
            if (visible)
            {
                entry.unlockSlot.InitializeUnlockSlot(this, entry.id, entry.icon);
            }
        }
    }

    private void RefreshEquippedSlots()
    {
        if (equippedSlots == null)
        {
            return;
        }

        for (int i = 0; i < equippedSlots.Length; i++)
        {
            PickupUISlotView slot = equippedSlots[i];
            if (slot == null)
            {
                continue;
            }

            slot.InitializeEquippedSlot(this, i);
            if (i >= equippedItems.Count)
            {
                slot.ClearIcon();
                continue;
            }

            PickupItemId itemId = equippedItems[i];
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
}
