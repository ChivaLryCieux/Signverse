using System;
using System.Collections.Generic;
using Skills;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class SkillPauseUIController : MonoBehaviour
{
    public enum SelectionState
    {
        None,
        LibrarySkillSelected,
        EquippedSkillSelected
    }

    [Serializable]
    public class SkillUiEntry
    {
        public SkillBase skill;
        public Sprite frontSprite;
        public Sprite backSprite;
        public int boltCost;
    }

    [Header("Core References")]
    [SerializeField] private GameObject uiCameraRoot;
    [SerializeField] private PlayerCC player;
    [SerializeField] private Skill2DSlot[] librarySlotsBottomToTop;
    [SerializeField] private Skill3DSlot[] equipSlots;

    [Header("Skill Data")]
    [SerializeField] private SkillUiEntry[] skillEntriesBottomToTop;
    [SerializeField] private int startingUnlockedCount = 1;
    [SerializeField] private int boltLimit = 6;

    [Header("Bolt UI")]
    [SerializeField] private GameObject boltPanel;
    [SerializeField] private Text boltText;
    [SerializeField] private Color boltNormalColor = Color.white;
    [SerializeField] private Color boltOverflowColor = Color.red;
    [SerializeField] private float boltBlinkSpeed = 5f;

    [Header("Detail UI")]
    [SerializeField] private GameObject detailPage;
    [SerializeField] private Text detailTitleText;
    [SerializeField] private Text detailCostText;

    [Header("Audio")]
    [SerializeField] private AudioClip hoverClip;
    [SerializeField] private AudioClip selectClip;
    [SerializeField] private AudioClip equipSuccessClip;
    [SerializeField] private AudioClip detachClip;
    [SerializeField, Range(0f, 1f)] private float sfxVolume = 1f;

    [Header("Pause")]
    [SerializeField] private bool pauseOnOpen = true;
    [SerializeField] private bool unlockCursorOnOpen = true;

    public bool IsOpen { get; private set; }
    public SelectionState CurrentSelectionState { get; private set; }
    public event Action<int, SkillBase> SkillEquipped;
    public event Action<int, SkillBase> SkillDetached;

    private readonly Dictionary<SkillBase, SkillUiEntry> entryBySkill = new Dictionary<SkillBase, SkillUiEntry>();
    private readonly Dictionary<SkillBase, Skill2DSlot> librarySlotBySkill = new Dictionary<SkillBase, Skill2DSlot>();
    private Skill2DSlot selectedLibrarySlot;
    private Skill3DSlot selectedEquipSlot;
    private float previousTimeScale = 1f;
    private CursorLockMode previousLockMode;
    private bool previousCursorVisible;

    private void Awake()
    {
        BuildEntryLookup();
        RegisterSlots();
        SetOpen(false, true);
    }

    private void OnEnable()
    {
        if (player != null)
        {
            player.SkillUnlocked += HandlePlayerSkillUnlocked;
        }
    }

    private void Start()
    {
        SyncUnlockedSlots();
    }

    private void OnDisable()
    {
        if (player != null)
        {
            player.SkillUnlocked -= HandlePlayerSkillUnlocked;
        }

        if (IsOpen)
        {
            RestorePauseState();
        }
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            SetOpen(!IsOpen);
        }

        UpdateBoltBlink();
    }

    public void SetOpen(bool open)
    {
        SetOpen(open, false);
    }

    public void OnLibrarySlotHover(Skill2DSlot slot)
    {
        if (!CanInteractWithLibrarySlot(slot))
        {
            return;
        }

        slot.SetHighlighted(true);
        PlaySfx(hoverClip);
        ShowBoltCost(slot.AssignedSkill);
    }

    public void OnLibrarySlotExit(Skill2DSlot slot)
    {
        if (slot != selectedLibrarySlot)
        {
            slot.SetHighlighted(false);
        }

        if (CurrentSelectionState != SelectionState.LibrarySkillSelected)
        {
            HideBoltCost();
        }
    }

    public void OnLibrarySlotClicked(Skill2DSlot slot)
    {
        if (!CanInteractWithLibrarySlot(slot))
        {
            return;
        }

        ClearEquipSelection();
        SelectLibrarySlot(slot);
        PlaySfx(selectClip);
    }

    public void OnEquipSlotHover(Skill3DSlot slot)
    {
        if (!IsOpen)
        {
            return;
        }

        if (CurrentSelectionState == SelectionState.LibrarySkillSelected || slot.HasSkill)
        {
            slot.SetHighlighted(true);
            PlaySfx(hoverClip);
        }
    }

    public void OnEquipSlotExit(Skill3DSlot slot)
    {
        if (slot != selectedEquipSlot)
        {
            slot.SetHighlighted(false);
        }
    }

    public void OnEquipSlotLeftClicked(Skill3DSlot slot)
    {
        if (!IsOpen)
        {
            return;
        }

        if (CurrentSelectionState == SelectionState.LibrarySkillSelected)
        {
            TryEquipSelectedLibrarySkill(slot);
            return;
        }

        if (!slot.HasSkill)
        {
            ClearSelection();
            return;
        }

        if (selectedEquipSlot == slot)
        {
            OpenDetailPage(slot.EquippedSkill);
            return;
        }

        ClearLibrarySelection(false);
        SelectEquipSlot(slot);
        PlaySfx(selectClip);
    }

    public void OnEquipSlotRightClicked(Skill3DSlot slot)
    {
        if (!IsOpen || !slot.HasSkill)
        {
            return;
        }

        SkillBase detachedSkill = slot.EquippedSkill;
        slot.ClearSkill();
        RestoreLibraryCard(detachedSkill);
        SkillDetached?.Invoke(slot.Index, detachedSkill);

        if (selectedEquipSlot == slot)
        {
            ClearEquipSelection();
        }

        CloseDetailPage();
        PlaySfx(detachClip);
    }

    public void UnlockSkill(SkillBase skill)
    {
        if (skill == null)
        {
            return;
        }

        if (librarySlotsBottomToTop == null)
        {
            librarySlotsBottomToTop = new Skill2DSlot[0];
        }

        for (int i = 0; i < librarySlotsBottomToTop.Length; i++)
        {
            Skill2DSlot slot = librarySlotsBottomToTop[i];
            if (slot == null)
            {
                continue;
            }

            if (slot.AssignedSkill == skill)
            {
                slot.SetUnlocked(true);
                return;
            }

            if (slot.AssignedSkill == null)
            {
                AssignSlot(slot, skill);
                slot.SetUnlocked(true);
                return;
            }
        }
    }

    private void SetOpen(bool open, bool immediate)
    {
        if (IsOpen == open && !immediate)
        {
            return;
        }

        IsOpen = open;

        if (uiCameraRoot != null)
        {
            uiCameraRoot.SetActive(open);
        }

        if (open)
        {
            previousTimeScale = Time.timeScale;
            previousLockMode = Cursor.lockState;
            previousCursorVisible = Cursor.visible;

            if (pauseOnOpen)
            {
                Time.timeScale = 0f;
            }

            if (unlockCursorOnOpen)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            SyncUnlockedSlots();
        }
        else
        {
            ClearSelection();
            CloseDetailPage();
            HideBoltCost();
            RestorePauseState();
        }
    }

    private void RestorePauseState()
    {
        if (pauseOnOpen)
        {
            Time.timeScale = previousTimeScale <= 0f ? 1f : previousTimeScale;
        }

        if (unlockCursorOnOpen)
        {
            Cursor.lockState = previousLockMode;
            Cursor.visible = previousCursorVisible;
        }
    }

    private void BuildEntryLookup()
    {
        entryBySkill.Clear();

        if (skillEntriesBottomToTop == null)
        {
            return;
        }

        foreach (SkillUiEntry entry in skillEntriesBottomToTop)
        {
            if (entry != null && entry.skill != null && !entryBySkill.ContainsKey(entry.skill))
            {
                entryBySkill.Add(entry.skill, entry);
            }
        }
    }

    private void RegisterSlots()
    {
        librarySlotBySkill.Clear();

        if (librarySlotsBottomToTop == null)
        {
            librarySlotsBottomToTop = new Skill2DSlot[0];
        }

        if (equipSlots == null)
        {
            equipSlots = new Skill3DSlot[0];
        }

        if (skillEntriesBottomToTop == null)
        {
            skillEntriesBottomToTop = new SkillUiEntry[0];
        }

        for (int i = 0; i < librarySlotsBottomToTop.Length; i++)
        {
            Skill2DSlot slot = librarySlotsBottomToTop[i];
            if (slot == null)
            {
                continue;
            }

            slot.Initialize(this, i);

            SkillUiEntry entry = i < skillEntriesBottomToTop.Length ? skillEntriesBottomToTop[i] : null;
            if (entry != null && entry.skill != null)
            {
                AssignSlot(slot, entry.skill);
            }
            else
            {
                slot.SetUnlocked(false);
            }
        }

        for (int i = 0; i < equipSlots.Length; i++)
        {
            if (equipSlots[i] != null)
            {
                equipSlots[i].Initialize(this, i);
                equipSlots[i].ClearSkill();
            }
        }
    }

    private void AssignSlot(Skill2DSlot slot, SkillBase skill)
    {
        SkillUiEntry entry = GetEntry(skill);
        slot.Assign(skill, entry != null ? entry.frontSprite : null, entry != null ? entry.backSprite : null);

        if (skill != null && !librarySlotBySkill.ContainsKey(skill))
        {
            librarySlotBySkill.Add(skill, slot);
        }
    }

    private void SyncUnlockedSlots()
    {
        if (librarySlotsBottomToTop == null)
        {
            librarySlotsBottomToTop = new Skill2DSlot[0];
        }

        int initialCount = Mathf.Clamp(startingUnlockedCount, 0, librarySlotsBottomToTop.Length);
        HashSet<SkillBase> unlockedSkills = BuildUnlockedSkillSet();

        for (int i = 0; i < librarySlotsBottomToTop.Length; i++)
        {
            Skill2DSlot slot = librarySlotsBottomToTop[i];
            if (slot == null)
            {
                continue;
            }

            bool isInitiallyUnlocked = i < initialCount;
            bool isPlayerUnlocked = slot.AssignedSkill != null && unlockedSkills.Contains(slot.AssignedSkill);
            slot.SetUnlocked((isInitiallyUnlocked || isPlayerUnlocked) && slot.AssignedSkill != null);
        }
    }

    private HashSet<SkillBase> BuildUnlockedSkillSet()
    {
        HashSet<SkillBase> unlockedSkills = new HashSet<SkillBase>();

        if (player == null)
        {
            return unlockedSkills;
        }

        foreach (SkillBase skill in player.unlockedSkills)
        {
            if (skill != null)
            {
                unlockedSkills.Add(skill);
            }
        }

        return unlockedSkills;
    }

    private bool CanInteractWithLibrarySlot(Skill2DSlot slot)
    {
        return IsOpen && slot != null && slot.IsUnlocked && slot.IsFaceUp && slot.AssignedSkill != null;
    }

    private void SelectLibrarySlot(Skill2DSlot slot)
    {
        ClearLibrarySelection(false);
        selectedLibrarySlot = slot;
        CurrentSelectionState = SelectionState.LibrarySkillSelected;
        slot.SetSelected(true);
        ShowBoltCost(slot.AssignedSkill);
    }

    private void SelectEquipSlot(Skill3DSlot slot)
    {
        ClearEquipSelection();
        selectedEquipSlot = slot;
        CurrentSelectionState = SelectionState.EquippedSkillSelected;
        slot.SetSelected(true);
        ShowBoltCost(slot.EquippedSkill);
    }

    private void TryEquipSelectedLibrarySkill(Skill3DSlot targetSlot)
    {
        if (selectedLibrarySlot == null || targetSlot == null)
        {
            ClearSelection();
            return;
        }

        if (targetSlot.HasSkill)
        {
            PlaySfx(selectClip);
            ClearLibrarySelection(true);
            return;
        }

        SkillBase skill = selectedLibrarySlot.AssignedSkill;
        targetSlot.SetSkill(skill);
        SkillEquipped?.Invoke(targetSlot.Index, skill);
        selectedLibrarySlot.SetFaceUp(false);
        selectedLibrarySlot.SetSelected(false);
        selectedLibrarySlot = null;
        CurrentSelectionState = SelectionState.None;
        HideBoltCost();
        PlaySfx(equipSuccessClip);
    }

    private void RestoreLibraryCard(SkillBase skill)
    {
        if (skill == null)
        {
            return;
        }

        if (librarySlotBySkill.TryGetValue(skill, out Skill2DSlot slot) && slot != null)
        {
            slot.SetUnlocked(true);
            slot.SetFaceUp(true);
            slot.SetSelected(false);
        }
    }

    private void ClearSelection()
    {
        ClearLibrarySelection(false);
        ClearEquipSelection();
        CurrentSelectionState = SelectionState.None;
    }

    private void ClearLibrarySelection(bool shrinkOnly)
    {
        if (selectedLibrarySlot != null)
        {
            selectedLibrarySlot.SetSelected(false);
            selectedLibrarySlot.SetHighlighted(false);
            selectedLibrarySlot = null;
        }

        if (!shrinkOnly && CurrentSelectionState == SelectionState.LibrarySkillSelected)
        {
            CurrentSelectionState = SelectionState.None;
        }
    }

    private void ClearEquipSelection()
    {
        if (selectedEquipSlot != null)
        {
            selectedEquipSlot.SetSelected(false);
            selectedEquipSlot.SetHighlighted(false);
            selectedEquipSlot = null;
        }

        if (CurrentSelectionState == SelectionState.EquippedSkillSelected)
        {
            CurrentSelectionState = SelectionState.None;
        }
    }

    private void ShowBoltCost(SkillBase skill)
    {
        SkillUiEntry entry = GetEntry(skill);

        if (boltPanel != null)
        {
            boltPanel.SetActive(entry != null);
        }

        if (boltText != null && entry != null)
        {
            boltText.text = entry.boltCost.ToString();
            boltText.color = entry.boltCost > boltLimit ? boltOverflowColor : boltNormalColor;
        }
    }

    private void HideBoltCost()
    {
        if (boltPanel != null)
        {
            boltPanel.SetActive(false);
        }
    }

    private void UpdateBoltBlink()
    {
        if (!IsOpen || boltText == null || boltPanel == null || !boltPanel.activeSelf)
        {
            return;
        }

        Color baseColor = boltText.color;
        float alpha = 0.45f + Mathf.Abs(Mathf.Sin(Time.unscaledTime * boltBlinkSpeed)) * 0.55f;
        boltText.color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
    }

    private void OpenDetailPage(SkillBase skill)
    {
        SkillUiEntry entry = GetEntry(skill);

        if (detailPage != null)
        {
            detailPage.SetActive(true);
        }

        if (detailTitleText != null)
        {
            detailTitleText.text = skill != null ? skill.skillName : string.Empty;
        }

        if (detailCostText != null)
        {
            detailCostText.text = entry != null ? entry.boltCost.ToString() : string.Empty;
        }
    }

    private void CloseDetailPage()
    {
        if (detailPage != null)
        {
            detailPage.SetActive(false);
        }
    }

    private SkillUiEntry GetEntry(SkillBase skill)
    {
        if (skill != null && entryBySkill.TryGetValue(skill, out SkillUiEntry entry))
        {
            return entry;
        }

        return null;
    }

    private void HandlePlayerSkillUnlocked(SkillBase skill)
    {
        UnlockSkill(skill);
    }

    private void PlaySfx(AudioClip clip)
    {
        if (clip == null || AudioManager.Instance == null)
        {
            return;
        }

        AudioManager.Instance.PlaySFX(clip, sfxVolume);
    }
}
