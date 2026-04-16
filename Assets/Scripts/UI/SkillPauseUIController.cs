using System;
using System.Collections.Generic;
using Skills;
using UnityEngine;
using UnityEngine.UI;

public class SkillPauseUIController : MonoBehaviour
{
    [Serializable]
    public class SkillUiEntry
    {
        [Tooltip("唯一键，不填会尝试使用 Skill.skillID")]
        public string id;
        public string displayName;
        public SkillBase skill;
        [Tooltip("用于主副技能组合解析，例如 m/j/d/c")]
        public string comboCode = "m";
        [Min(1)] public int boltCost = 1;

        [Header("图标")]
        public Sprite backpackSprite;
        public Sprite backpackSelectedSprite;
        public Sprite configSprite;

        [Header("详情页")]
        public Sprite detailSprite;
        [TextArea(2, 5)] public string detailDescLine1;
        [TextArea(2, 5)] public string detailDescLine2;
    }

    [Serializable]
    public class ConfigRow
    {
        public SkillScreenSlotView mainSlot;
        public SkillScreenSlotView subSlot;
    }

    [Serializable]
    public class ComboResult
    {
        public string mainCode = "m";
        public string subCode = "m";
        public SkillBase resultSkill;
    }

    [Header("基础结构")]
    [SerializeField] private SkillScreenSlotView[] backpackSlots;
    [SerializeField] private ConfigRow[] configRows;
    [SerializeField] private SkillUiEntry[] skillEntries;

    [Header("螺栓点数")]
    [SerializeField, Min(1)] private int maxBolts = 3;
    [SerializeField] private Image[] boltIcons;
    [SerializeField] private Sprite boltConfiguredSprite;
    [SerializeField] private Sprite boltEmptySprite;
    [SerializeField] private Sprite boltWarningSprite;
    [SerializeField] private float boltBlinkSpeed = 6f;

    [Header("技能组合")]
    [SerializeField] private ComboResult[] comboResults;

    [Header("技能系统接入（可选）")]
    [SerializeField] private PlayerCC player;
    [SerializeField] private bool syncResolvedSkillsToPlayerUnlockedList = true;
    [SerializeField] private bool replaceUnlockedListWithResolvedSkills = true;

    [Header("技能详情页（可选）")]
    [SerializeField] private GameObject detailPanel;
    [SerializeField] private Image detailImage;
    [SerializeField] private Text detailTitleText;
    [SerializeField] private Text detailDescLine1Text;
    [SerializeField] private Text detailDescLine2Text;

    public event Action<IReadOnlyList<SkillBase>> ResolvedSkillsChanged;

    private class RowRuntime
    {
        public SkillUiEntry main;
        public SkillUiEntry sub;
    }

    private readonly Dictionary<string, SkillUiEntry> entryById = new Dictionary<string, SkillUiEntry>();
    private readonly Dictionary<SkillBase, SkillUiEntry> entryBySkill = new Dictionary<SkillBase, SkillUiEntry>();
    private readonly Dictionary<SkillScreenSlotView, SkillUiEntry> backpackEntryBySlot = new Dictionary<SkillScreenSlotView, SkillUiEntry>();
    private readonly Dictionary<string, SkillScreenSlotView> backpackSlotById = new Dictionary<string, SkillScreenSlotView>();
    private readonly Dictionary<string, SkillBase> comboLookup = new Dictionary<string, SkillBase>();
    private readonly List<SkillBase> resolvedSkillsCache = new List<SkillBase>();

    private RowRuntime[] rowRuntime;
    private SkillUiEntry selectedEntry;
    private SkillScreenSlotView selectedBackpackSlot;
    private bool selectedEntryCanAfford;

    private void Awake()
    {
        BuildLookups();
        InitializeSlots();
        RebuildResolvedSkills();
        RefreshSelectionVisual();
        UpdateBoltBarVisual();
        CloseDetailPanel();
    }

    private void Update()
    {
        if (detailPanel != null && detailPanel.activeSelf && Input.GetKeyDown(KeyCode.Escape))
        {
            CloseDetailPanel();
            return;
        }

        if (selectedEntry != null)
        {
            UpdateBoltBarVisual();
        }
    }

    public void OnSlotLeftClick(SkillScreenSlotView slot)
    {
        if (slot == null)
        {
            return;
        }

        if (slot.Role == SkillScreenSlotView.SlotRole.Backpack)
        {
            HandleBackpackClick(slot);
            return;
        }

        HandleConfigLeftClick(slot);
    }

    public void OnSlotRightClick(SkillScreenSlotView slot)
    {
        if (slot == null || slot.Role != SkillScreenSlotView.SlotRole.Config)
        {
            return;
        }

        HandleConfigRightClick(slot);
    }

    public void OnSlotMiddleClick(SkillScreenSlotView slot)
    {
        if (slot == null || slot.Role != SkillScreenSlotView.SlotRole.Config)
        {
            return;
        }

        if (TryGetConfigEntry(slot, out SkillUiEntry entry))
        {
            OpenDetailPanel(entry);
        }
    }

    public void OnSlotDoubleClick(SkillScreenSlotView slot)
    {
        OnSlotMiddleClick(slot);
    }

    public void CloseDetailPanel()
    {
        if (detailPanel != null)
        {
            detailPanel.SetActive(false);
        }
    }

    private void BuildLookups()
    {
        entryById.Clear();
        entryBySkill.Clear();
        comboLookup.Clear();

        if (skillEntries == null)
        {
            skillEntries = Array.Empty<SkillUiEntry>();
        }

        for (int i = 0; i < skillEntries.Length; i++)
        {
            SkillUiEntry entry = skillEntries[i];
            if (entry == null)
            {
                continue;
            }

            NormalizeEntry(entry);

            if (!string.IsNullOrWhiteSpace(entry.id) && !entryById.ContainsKey(entry.id))
            {
                entryById.Add(entry.id, entry);
            }

            if (entry.skill != null && !entryBySkill.ContainsKey(entry.skill))
            {
                entryBySkill.Add(entry.skill, entry);
            }
        }

        if (comboResults == null)
        {
            comboResults = Array.Empty<ComboResult>();
        }

        for (int i = 0; i < comboResults.Length; i++)
        {
            ComboResult combo = comboResults[i];
            if (combo == null || combo.resultSkill == null)
            {
                continue;
            }

            string key = BuildComboKey(combo.mainCode, combo.subCode);
            if (!comboLookup.ContainsKey(key))
            {
                comboLookup.Add(key, combo.resultSkill);
            }
        }
    }

    private void InitializeSlots()
    {
        backpackEntryBySlot.Clear();
        backpackSlotById.Clear();

        if (backpackSlots == null)
        {
            backpackSlots = Array.Empty<SkillScreenSlotView>();
        }

        for (int i = 0; i < backpackSlots.Length; i++)
        {
            SkillScreenSlotView slot = backpackSlots[i];
            if (slot == null)
            {
                continue;
            }

            slot.Initialize(this, SkillScreenSlotView.SlotRole.Backpack, -1, SkillScreenSlotView.SlotColumn.Main);

            SkillUiEntry entry = i < skillEntries.Length ? skillEntries[i] : null;
            if (entry == null)
            {
                slot.SetVisible(false);
                continue;
            }

            backpackEntryBySlot[slot] = entry;
            if (!backpackSlotById.ContainsKey(entry.id))
            {
                backpackSlotById.Add(entry.id, slot);
            }

            slot.SetBackpackSprites(entry.backpackSprite, entry.backpackSelectedSprite);
            slot.SetVisible(true);
            slot.SetSelected(false);
        }

        if (configRows == null)
        {
            configRows = Array.Empty<ConfigRow>();
        }

        rowRuntime = new RowRuntime[configRows.Length];

        for (int i = 0; i < configRows.Length; i++)
        {
            rowRuntime[i] = new RowRuntime();
            ConfigRow row = configRows[i];
            if (row == null)
            {
                continue;
            }

            if (row.mainSlot != null)
            {
                row.mainSlot.Initialize(this, SkillScreenSlotView.SlotRole.Config, i, SkillScreenSlotView.SlotColumn.Main);
                row.mainSlot.ClearConfigSprite();
                row.mainSlot.SetSelected(false);
            }

            if (row.subSlot != null)
            {
                row.subSlot.Initialize(this, SkillScreenSlotView.SlotRole.Config, i, SkillScreenSlotView.SlotColumn.Sub);
                row.subSlot.ClearConfigSprite();
                row.subSlot.SetSelected(false);
            }
        }
    }

    private void HandleBackpackClick(SkillScreenSlotView slot)
    {
        if (!backpackEntryBySlot.TryGetValue(slot, out SkillUiEntry entry))
        {
            return;
        }

        if (!slot.gameObject.activeInHierarchy)
        {
            return;
        }

        if (selectedEntry == entry)
        {
            ClearSelection();
            return;
        }

        selectedEntry = entry;
        selectedBackpackSlot = slot;
        selectedEntryCanAfford = GetUsedBoltCount() + Mathf.Max(1, selectedEntry.boltCost) <= maxBolts;

        RefreshSelectionVisual();
        UpdateBoltBarVisual();
    }

    private void HandleConfigLeftClick(SkillScreenSlotView slot)
    {
        if (selectedEntry == null)
        {
            return;
        }

        if (TryGetConfigEntry(slot, out _))
        {
            ClearSelection();
            return;
        }

        if (!selectedEntryCanAfford)
        {
            ClearSelection();
            return;
        }

        if (!TryGetRow(slot.RowIndex, out RowRuntime rowRuntimeData))
        {
            ClearSelection();
            return;
        }

        if (slot.Column == SkillScreenSlotView.SlotColumn.Sub && rowRuntimeData.main == null)
        {
            ClearSelection();
            return;
        }

        AssignToConfig(slot.RowIndex, slot.Column, selectedEntry);
        SetBackpackVisible(selectedEntry, false);

        ClearSelection();
        RebuildResolvedSkills();
        UpdateBoltBarVisual();
    }

    private void HandleConfigRightClick(SkillScreenSlotView slot)
    {
        if (!TryGetRow(slot.RowIndex, out RowRuntime rowData))
        {
            return;
        }

        if (slot.Column == SkillScreenSlotView.SlotColumn.Main)
        {
            if (rowData.main == null)
            {
                return;
            }

            SkillUiEntry removedMain = rowData.main;
            SkillUiEntry removedSub = rowData.sub;

            rowData.main = null;
            rowData.sub = null;

            SetConfigVisual(slot.RowIndex, SkillScreenSlotView.SlotColumn.Main, null);
            SetConfigVisual(slot.RowIndex, SkillScreenSlotView.SlotColumn.Sub, null);

            SetBackpackVisible(removedMain, true);
            if (removedSub != null)
            {
                SetBackpackVisible(removedSub, true);
            }
        }
        else
        {
            if (rowData.sub == null)
            {
                return;
            }

            SkillUiEntry removedSub = rowData.sub;
            rowData.sub = null;
            SetConfigVisual(slot.RowIndex, SkillScreenSlotView.SlotColumn.Sub, null);
            SetBackpackVisible(removedSub, true);
        }

        ClearSelection();
        RebuildResolvedSkills();
        UpdateBoltBarVisual();
    }

    private void AssignToConfig(int rowIndex, SkillScreenSlotView.SlotColumn column, SkillUiEntry entry)
    {
        if (!TryGetRow(rowIndex, out RowRuntime rowData) || entry == null)
        {
            return;
        }

        if (column == SkillScreenSlotView.SlotColumn.Main)
        {
            rowData.main = entry;
            SetConfigVisual(rowIndex, SkillScreenSlotView.SlotColumn.Main, entry);
            return;
        }

        rowData.sub = entry;
        SetConfigVisual(rowIndex, SkillScreenSlotView.SlotColumn.Sub, entry);
    }

    private void SetConfigVisual(int rowIndex, SkillScreenSlotView.SlotColumn column, SkillUiEntry entry)
    {
        SkillScreenSlotView slot = GetConfigSlot(rowIndex, column);
        if (slot == null)
        {
            return;
        }

        if (entry == null)
        {
            slot.ClearConfigSprite();
            return;
        }

        slot.SetConfigSprite(entry.id, entry.configSprite);
    }

    private void SetBackpackVisible(SkillUiEntry entry, bool visible)
    {
        if (entry == null)
        {
            return;
        }

        if (backpackSlotById.TryGetValue(entry.id, out SkillScreenSlotView slot) && slot != null)
        {
            slot.SetVisible(visible);
            slot.SetSelected(false);
        }
    }

    private bool TryGetConfigEntry(SkillScreenSlotView slot, out SkillUiEntry entry)
    {
        entry = null;

        if (slot == null || slot.Role != SkillScreenSlotView.SlotRole.Config)
        {
            return false;
        }

        if (!TryGetRow(slot.RowIndex, out RowRuntime rowData))
        {
            return false;
        }

        entry = slot.Column == SkillScreenSlotView.SlotColumn.Main ? rowData.main : rowData.sub;
        return entry != null;
    }

    private bool TryGetRow(int rowIndex, out RowRuntime rowData)
    {
        rowData = null;
        if (rowRuntime == null || rowIndex < 0 || rowIndex >= rowRuntime.Length)
        {
            return false;
        }

        rowData = rowRuntime[rowIndex];
        return rowData != null;
    }

    private SkillScreenSlotView GetConfigSlot(int rowIndex, SkillScreenSlotView.SlotColumn column)
    {
        if (configRows == null || rowIndex < 0 || rowIndex >= configRows.Length)
        {
            return null;
        }

        ConfigRow row = configRows[rowIndex];
        if (row == null)
        {
            return null;
        }

        return column == SkillScreenSlotView.SlotColumn.Main ? row.mainSlot : row.subSlot;
    }

    private void RebuildResolvedSkills()
    {
        resolvedSkillsCache.Clear();

        if (rowRuntime != null)
        {
            for (int i = 0; i < rowRuntime.Length; i++)
            {
                RowRuntime row = rowRuntime[i];
                if (row == null || row.main == null)
                {
                    continue;
                }

                SkillUiEntry subEntry = row.sub != null ? row.sub : row.main;
                SkillBase resolved = ResolveComboSkill(row.main, subEntry);
                if (resolved != null && !resolvedSkillsCache.Contains(resolved))
                {
                    resolvedSkillsCache.Add(resolved);
                }
            }
        }

        ApplyResolvedSkillsToPlayer();
        ResolvedSkillsChanged?.Invoke(resolvedSkillsCache);
    }

    private SkillBase ResolveComboSkill(SkillUiEntry mainEntry, SkillUiEntry subEntry)
    {
        if (mainEntry == null)
        {
            return null;
        }

        string mainCode = string.IsNullOrWhiteSpace(mainEntry.comboCode) ? "m" : mainEntry.comboCode;
        string subCode = subEntry != null && !string.IsNullOrWhiteSpace(subEntry.comboCode)
            ? subEntry.comboCode
            : mainCode;

        string comboKey = BuildComboKey(mainCode, subCode);
        if (comboLookup.TryGetValue(comboKey, out SkillBase mappedSkill) && mappedSkill != null)
        {
            return mappedSkill;
        }

        return mainEntry.skill;
    }

    private void ApplyResolvedSkillsToPlayer()
    {
        if (!syncResolvedSkillsToPlayerUnlockedList || player == null)
        {
            return;
        }

        if (player.unlockedSkills == null)
        {
            player.unlockedSkills = new List<SkillBase>();
        }

        if (replaceUnlockedListWithResolvedSkills)
        {
            player.unlockedSkills.Clear();
            for (int i = 0; i < resolvedSkillsCache.Count; i++)
            {
                SkillBase skill = resolvedSkillsCache[i];
                if (skill != null)
                {
                    player.unlockedSkills.Add(skill);
                }
            }

            return;
        }

        for (int i = 0; i < resolvedSkillsCache.Count; i++)
        {
            SkillBase skill = resolvedSkillsCache[i];
            if (skill != null && !player.unlockedSkills.Contains(skill))
            {
                player.unlockedSkills.Add(skill);
            }
        }
    }

    private int GetUsedBoltCount()
    {
        int used = 0;

        if (rowRuntime == null)
        {
            return 0;
        }

        for (int i = 0; i < rowRuntime.Length; i++)
        {
            RowRuntime row = rowRuntime[i];
            if (row == null)
            {
                continue;
            }

            if (row.main != null)
            {
                used += Mathf.Max(1, row.main.boltCost);
            }

            if (row.sub != null)
            {
                used += Mathf.Max(1, row.sub.boltCost);
            }
        }

        return used;
    }

    private void UpdateBoltBarVisual()
    {
        if (boltIcons == null || boltIcons.Length == 0)
        {
            return;
        }

        int used = GetUsedBoltCount();
        float blinkAlpha = 0.45f + Mathf.Abs(Mathf.Sin(Time.unscaledTime * boltBlinkSpeed)) * 0.55f;

        if (selectedEntry == null)
        {
            for (int i = 0; i < boltIcons.Length; i++)
            {
                SetBoltIconVisual(i, i < used ? boltConfiguredSprite : boltEmptySprite, 1f);
            }

            return;
        }

        int selectedCost = Mathf.Max(1, selectedEntry.boltCost);

        if (!selectedEntryCanAfford)
        {
            int warningCount = Mathf.Min(selectedCost, boltIcons.Length);
            for (int i = 0; i < boltIcons.Length; i++)
            {
                if (i < warningCount)
                {
                    SetBoltIconVisual(i, boltWarningSprite, blinkAlpha);
                }
                else
                {
                    SetBoltIconVisual(i, boltEmptySprite, 1f);
                }
            }

            return;
        }

        int previewEnd = used + selectedCost;
        for (int i = 0; i < boltIcons.Length; i++)
        {
            if (i < used)
            {
                SetBoltIconVisual(i, boltConfiguredSprite, 1f);
                continue;
            }

            if (i < previewEnd)
            {
                SetBoltIconVisual(i, boltConfiguredSprite, blinkAlpha);
                continue;
            }

            SetBoltIconVisual(i, boltEmptySprite, 1f);
        }
    }

    private void SetBoltIconVisual(int index, Sprite sprite, float alpha)
    {
        Image image = boltIcons[index];
        if (image == null)
        {
            return;
        }

        if (sprite != null)
        {
            image.sprite = sprite;
        }

        Color color = image.color;
        color.a = alpha;
        image.color = color;
    }

    private void OpenDetailPanel(SkillUiEntry entry)
    {
        if (entry == null)
        {
            return;
        }

        if (detailPanel != null)
        {
            detailPanel.SetActive(true);
        }

        if (detailImage != null)
        {
            detailImage.sprite = entry.detailSprite != null ? entry.detailSprite : entry.configSprite;
        }

        if (detailTitleText != null)
        {
            detailTitleText.text = !string.IsNullOrWhiteSpace(entry.displayName)
                ? entry.displayName
                : (entry.skill != null ? entry.skill.skillName : entry.id);
        }

        if (detailDescLine1Text != null)
        {
            detailDescLine1Text.text = entry.detailDescLine1;
        }

        if (detailDescLine2Text != null)
        {
            detailDescLine2Text.text = entry.detailDescLine2;
        }
    }

    private void RefreshSelectionVisual()
    {
        if (backpackSlots == null)
        {
            return;
        }

        for (int i = 0; i < backpackSlots.Length; i++)
        {
            SkillScreenSlotView slot = backpackSlots[i];
            if (slot == null)
            {
                continue;
            }

            bool selected = slot == selectedBackpackSlot && selectedEntry != null;
            slot.SetSelected(selected);
        }
    }

    private void ClearSelection()
    {
        selectedEntry = null;
        selectedBackpackSlot = null;
        selectedEntryCanAfford = false;
        RefreshSelectionVisual();
        UpdateBoltBarVisual();
    }

    private void NormalizeEntry(SkillUiEntry entry)
    {
        if (entry == null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(entry.id))
        {
            if (entry.skill != null && !string.IsNullOrWhiteSpace(entry.skill.skillID))
            {
                entry.id = entry.skill.skillID.Trim();
            }
            else
            {
                entry.id = Guid.NewGuid().ToString("N");
            }
        }
        else
        {
            entry.id = entry.id.Trim();
        }

        entry.comboCode = string.IsNullOrWhiteSpace(entry.comboCode) ? "m" : entry.comboCode.Trim().ToLowerInvariant();
        entry.boltCost = Mathf.Max(1, entry.boltCost);
    }

    private static string BuildComboKey(string mainCode, string subCode)
    {
        string m = string.IsNullOrWhiteSpace(mainCode) ? "m" : mainCode.Trim().ToLowerInvariant();
        string s = string.IsNullOrWhiteSpace(subCode) ? m : subCode.Trim().ToLowerInvariant();
        return m + "|" + s;
    }
}
