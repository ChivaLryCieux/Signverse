using System;
using System.Collections.Generic;
using Skills;
using UnityEngine;

[DisallowMultipleComponent]
public class SkillController : MonoBehaviour
{
    private const int SurfaceHitBufferSize = 16;

    [Header("技能系统 (Slot-Based)")]
    [Tooltip("可选：调试或特殊关卡开局自带技能。正式流程可留空，移动/跳跃/冲刺由拾取和 UI 解锁。")]
    [SerializeField] private List<SkillBase> startingSkills = new List<SkillBase>();
    [SerializeField] private List<SkillBase> unlockedSkills = new List<SkillBase>();
    [SerializeField] private List<SkillBase> equippedSkills = new List<SkillBase>();
    [SerializeField] private SkillDatabase masterDatabase;

    [Header("技能装配限制")]
    [SerializeField] private List<string> skillLoadoutSurfaceTags = new List<string>()
    {
        "Nature",
        "Water"
    };
    [SerializeField] private LayerMask skillLoadoutSurfaceMask = ~0;
    [SerializeField] private float skillLoadoutSurfaceCheckDistance = 0.25f;

    private readonly List<SkillBase> skillUpdateBuffer = new List<SkillBase>();
    private readonly HashSet<SkillBase> skillUpdateSet = new HashSet<SkillBase>();
    private readonly RaycastHit[] skillLoadoutSurfaceHitBuffer = new RaycastHit[SurfaceHitBufferSize];
    private bool hasExplicitEquippedSkills;
    private bool startingSkillsInitialized;
    [SerializeField, HideInInspector]
    private bool legacyDataInitialized;

    public event Action<SkillBase> SkillUnlocked;

    public List<SkillBase> StartingSkills
    {
        get => startingSkills;
        set => startingSkills = value ?? new List<SkillBase>();
    }

    public List<SkillBase> UnlockedSkills
    {
        get => unlockedSkills;
        set => unlockedSkills = value ?? new List<SkillBase>();
    }

    public List<SkillBase> EquippedSkills
    {
        get => equippedSkills;
        set => equippedSkills = value ?? new List<SkillBase>();
    }

    public SkillDatabase MasterDatabase
    {
        get => masterDatabase;
        set => masterDatabase = value;
    }

    public bool UsesEquippedSkillLoadout => hasExplicitEquippedSkills || (equippedSkills != null && equippedSkills.Count > 0);

    public void InitializeStartingSkills()
    {
        EnsureLists();

        if (startingSkillsInitialized)
        {
            return;
        }

        startingSkillsInitialized = true;
        for (int i = 0; i < startingSkills.Count; i++)
        {
            UnlockSkill(startingSkills[i]);
        }
    }

    public void InitializeFromLegacy(
        List<SkillBase> legacyStartingSkills,
        List<SkillBase> legacyUnlockedSkills,
        List<SkillBase> legacyEquippedSkills,
        SkillDatabase legacyMasterDatabase,
        List<string> legacySurfaceTags,
        LayerMask legacySurfaceMask,
        float legacySurfaceCheckDistance)
    {
        if (legacyDataInitialized)
        {
            return;
        }

        legacyDataInitialized = true;

        if (legacyStartingSkills != null && legacyStartingSkills.Count > 0 && IsNullOrEmpty(startingSkills))
        {
            startingSkills = new List<SkillBase>(legacyStartingSkills);
        }

        if (legacyUnlockedSkills != null && legacyUnlockedSkills.Count > 0 && IsNullOrEmpty(unlockedSkills))
        {
            unlockedSkills = new List<SkillBase>(legacyUnlockedSkills);
        }

        if (legacyEquippedSkills != null && legacyEquippedSkills.Count > 0 && IsNullOrEmpty(equippedSkills))
        {
            equippedSkills = new List<SkillBase>(legacyEquippedSkills);
        }

        if (masterDatabase == null)
        {
            masterDatabase = legacyMasterDatabase;
        }

        if (legacySurfaceTags != null && legacySurfaceTags.Count > 0 && IsDefaultSurfaceTagList(skillLoadoutSurfaceTags))
        {
            skillLoadoutSurfaceTags = new List<string>(legacySurfaceTags);
        }

        if (legacySurfaceMask.value != 0 && skillLoadoutSurfaceMask.value == ~0)
        {
            skillLoadoutSurfaceMask = legacySurfaceMask;
        }

        if (legacySurfaceCheckDistance > 0f && Mathf.Approximately(skillLoadoutSurfaceCheckDistance, 0.25f))
        {
            skillLoadoutSurfaceCheckDistance = legacySurfaceCheckDistance;
        }

        hasExplicitEquippedSkills = equippedSkills != null && equippedSkills.Count > 0;
        EnsureLists();
    }

    public bool HasUnlockedSkill(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return false;
        }

        EnsureLists();
        for (int i = 0; i < unlockedSkills.Count; i++)
        {
            SkillBase skill = unlockedSkills[i];
            if (skill != null && skill.skillID == id)
            {
                return true;
            }
        }

        return false;
    }

    public bool HasUnlockedSkill<T>() where T : SkillBase
    {
        EnsureLists();
        for (int i = 0; i < unlockedSkills.Count; i++)
        {
            if (unlockedSkills[i] is T)
            {
                return true;
            }
        }

        return false;
    }

    public bool HasEquippedSkill(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return false;
        }

        EnsureLists();
        for (int i = 0; i < equippedSkills.Count; i++)
        {
            SkillBase skill = equippedSkills[i];
            if (skill != null && skill.skillID == id)
            {
                return true;
            }
        }

        return false;
    }

    public bool HasEquippedSkill<T>() where T : SkillBase
    {
        EnsureLists();
        for (int i = 0; i < equippedSkills.Count; i++)
        {
            if (equippedSkills[i] is T)
            {
                return true;
            }
        }

        return false;
    }

    public void SetEquippedSkills(IList<SkillBase> skills)
    {
        EnsureLists();
        hasExplicitEquippedSkills = true;
        equippedSkills.Clear();

        if (skills == null)
        {
            return;
        }

        for (int i = 0; i < skills.Count; i++)
        {
            SkillBase skill = skills[i];
            if (skill != null && !equippedSkills.Contains(skill))
            {
                equippedSkills.Add(skill);
            }
        }
    }

    public void UnlockNewSkill(string id)
    {
        if (masterDatabase == null)
        {
            return;
        }

        SkillBase newSkill = masterDatabase.GetSkillByID(id);
        UnlockSkill(newSkill);
    }

    public void UnlockSkill(SkillBase skill)
    {
        EnsureLists();
        if (skill != null && !unlockedSkills.Contains(skill))
        {
            unlockedSkills.Add(skill);
            SkillUnlocked?.Invoke(skill);
        }
    }

    public bool CanModifySkillLoadout(PlayerCC owner)
    {
        return IsStandingOnAnyTaggedSurface(owner, skillLoadoutSurfaceTags);
    }

    public void UpdateSkills(GameObject user, PlayerCC owner, PlayerCC.Posture posture)
    {
        IList<SkillBase> activeSkills = GetActiveSkillsForUpdate();
        skillUpdateBuffer.Clear();
        skillUpdateSet.Clear();

        if (activeSkills != null)
        {
            for (int i = 0; i < activeSkills.Count; i++)
            {
                SkillBase skill = activeSkills[i];
                if (skill != null && skillUpdateSet.Add(skill))
                {
                    skillUpdateBuffer.Add(skill);
                }
            }
        }

        for (int i = 0; i < skillUpdateBuffer.Count; i++)
        {
            SkillBase skill = skillUpdateBuffer[i];
            if (skill == null)
            {
                continue;
            }

            skill.OnUpdate(user, owner, posture);
        }
    }

    private IList<SkillBase> GetActiveSkillsForUpdate()
    {
        EnsureLists();
        return UsesEquippedSkillLoadout ? equippedSkills : unlockedSkills;
    }

    private bool IsStandingOnAnyTaggedSurface(PlayerCC owner, List<string> requiredTags)
    {
        if (requiredTags == null || requiredTags.Count == 0)
        {
            return true;
        }

        int hitCount = GetSkillLoadoutSurfaceHits(owner);
        return HasAnyTaggedSurfaceHit(skillLoadoutSurfaceHitBuffer, hitCount, requiredTags);
    }

    private int GetSkillLoadoutSurfaceHits(PlayerCC owner)
    {
        CharacterController activeController = owner != null ? owner.GetCharacterController() : null;
        if (activeController == null || skillLoadoutSurfaceMask.value == 0)
        {
            return 0;
        }

        Vector3 worldCenter = activeController.transform.TransformPoint(activeController.center);
        float bottomOffset = Mathf.Max(0f, activeController.height * 0.5f - activeController.radius);
        Vector3 sphereOrigin = worldCenter + Vector3.down * bottomOffset + Vector3.up * 0.05f;
        float radius = Mathf.Max(0.01f, activeController.radius * 0.9f);
        float distance = Mathf.Max(0.01f, skillLoadoutSurfaceCheckDistance + 0.05f);

        return Physics.SphereCastNonAlloc(
            sphereOrigin,
            radius,
            Vector3.down,
            skillLoadoutSurfaceHitBuffer,
            distance,
            skillLoadoutSurfaceMask,
            QueryTriggerInteraction.Ignore);
    }

    private bool HasAnyTaggedSurfaceHit(RaycastHit[] hits, int hitCount, List<string> requiredTags)
    {
        if (hits == null || requiredTags == null)
        {
            return false;
        }

        int safeHitCount = Mathf.Min(hitCount, hits.Length);
        for (int i = 0; i < safeHitCount; i++)
        {
            Collider hitCollider = hits[i].collider;
            if (hitCollider == null)
            {
                continue;
            }

            for (int j = 0; j < requiredTags.Count; j++)
            {
                if (HasTagInParents(hitCollider.transform, requiredTags[j]))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private bool HasTagInParents(Transform target, string requiredTag)
    {
        if (target == null || string.IsNullOrWhiteSpace(requiredTag))
        {
            return false;
        }

        Transform current = target;
        while (current != null)
        {
            if (string.Equals(current.gameObject.tag, requiredTag, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            current = current.parent;
        }

        return false;
    }

    private void EnsureLists()
    {
        startingSkills ??= new List<SkillBase>();
        unlockedSkills ??= new List<SkillBase>();
        equippedSkills ??= new List<SkillBase>();
    }

    private static bool IsNullOrEmpty<T>(ICollection<T> collection)
    {
        return collection == null || collection.Count == 0;
    }

    private static bool IsDefaultSurfaceTagList(List<string> tags)
    {
        return tags == null ||
               tags.Count == 0 ||
               (tags.Count == 2 && tags[0] == "Nature" && tags[1] == "Water");
    }
}
