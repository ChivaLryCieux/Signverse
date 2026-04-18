using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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
    [SerializeField] private GameObject equippedRoot;
    [SerializeField] private Image equippedIcon;

    [Header("行为")]
    [SerializeField] private bool hideLockedSlotsOnStart = true;

    public event Action<PickupItemId> ItemUnlocked;
    public event Action<PickupItemId> ItemEquipped;

    private readonly Dictionary<PickupItemId, PickupUiEntry> entryById = new Dictionary<PickupItemId, PickupUiEntry>();
    private readonly HashSet<PickupItemId> unlockedItems = new HashSet<PickupItemId>();
    private bool hasEquippedItem;
    private PickupItemId equippedItem;

    public bool HasEquippedItem => hasEquippedItem;
    public PickupItemId EquippedItem => equippedItem;

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
        RefreshEquippedSlot();
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
        if (!entryById.TryGetValue(id, out PickupUiEntry entry))
        {
            Debug.LogWarning($"PickupUIController 没有配置拾取物 {id}。", this);
            return;
        }

        if (!unlockedItems.Add(id))
        {
            return;
        }

        if (entry.unlockSlot != null)
        {
            entry.unlockSlot.gameObject.SetActive(true);
            entry.unlockSlot.Initialize(this, id, entry.icon);
        }

        ItemUnlocked?.Invoke(id);
    }

    public void Equip(PickupItemId id)
    {
        if (!unlockedItems.Contains(id))
        {
            return;
        }

        if (!entryById.TryGetValue(id, out PickupUiEntry entry))
        {
            return;
        }

        hasEquippedItem = true;
        equippedItem = id;

        if (equippedRoot != null)
        {
            equippedRoot.SetActive(true);
        }

        if (equippedIcon != null)
        {
            equippedIcon.sprite = entry.icon;
            equippedIcon.enabled = entry.icon != null;
        }

        ItemEquipped?.Invoke(id);
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

            entry.unlockSlot.Initialize(this, entry.id, entry.icon);
            if (hideLockedSlotsOnStart)
            {
                entry.unlockSlot.gameObject.SetActive(unlockedItems.Contains(entry.id));
            }
        }
    }

    private void RefreshEquippedSlot()
    {
        if (equippedRoot != null)
        {
            equippedRoot.SetActive(hasEquippedItem);
        }

        if (equippedIcon != null)
        {
            equippedIcon.enabled = hasEquippedItem && equippedIcon.sprite != null;
        }
    }
}
