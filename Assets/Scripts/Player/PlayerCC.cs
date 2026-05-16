using UnityEngine;
using System.Collections.Generic;
using Skills; 
using System;

// ===== PlayerCC 12大功能分区总览 =====
// 01. 基础配置、引用与运行时缓存
// 02. 技能脚本访问接口
// 03. 技能拥有、装备与装配限制
// 04. 技能解锁、朝向、检查点、死亡与调试绘制
// 05. 技能装配地面检测
// 06. 朝向、移动与控制代理
// 07. 攀爬状态、翻越请求与过渡触发器
// 08. 姿态、隐身与重力状态刷新
// 09. 死亡表现控制
// 10. 死亡标志、渲染器与碰撞体显隐
// 11. Unity 生命周期与主循环
// 12. 每帧移动、技能更新与物理辅助

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerDeath))]
public class PlayerCC : MonoBehaviour
{
    // ===== 01. 基础配置、引用与运行时缓存 =====

    private const int SurfaceHitBufferSize = 16;
    public bool isClimbInvincible = false;
    private const float MinClimbExitUpInputLockDuration = 3f;
    public AudioSource audioSource;
    public AudioClip deathSFX;
    
    
    [Header("临时任务道具")]
    public bool hasDoorPickup;
    public enum Posture
    {
        Grounded,
        Airborne,
        Climbing
    }

    public event Action<SkillBase> SkillUnlocked;

    [Header("核心引用")]
    public CharacterController cc;
    private PlayerControls controls; 
    private PlayerControls.PlayerActions playerActions;
    private PlayerDeath playerDeath;
    private CloakEffectController cloakEffect;
    private CharacterController controlProxy;
    private Transform controlProxyTransform;
    private readonly List<Collider> disabledOwnerCollidersForProxy = new List<Collider>();
    private bool ownerControllerEnabledBeforeProxy;
    private readonly Dictionary<Renderer, bool> rendererVisibilityBeforeDeath = new Dictionary<Renderer, bool>();
    private readonly List<Collider> disabledCollidersForDeath = new List<Collider>();
    private bool deathPresentationActive;

    [Header("动画引用")]
    [SerializeField] private Animator animator;

    [Header("死亡表现")]
    [SerializeField] private Transform deathSign;
    [SerializeField] private string deathSignName = "DeathSign";
    [SerializeField] private bool hideDeathSignOnAwake = true;

    [Header("物理参数")]
    public float gravity = -25f;
    [Tooltip("下落时的额外重力倍率，用于让 VerticalVelocity 更快进入下落段。")]
    public float fallMultiplier = 1.6f;
    [SerializeField] private float turnInputThreshold = 0.1f;

    [SerializeField] private float ceilingHitFallVelocity = -0.5f;

    private float verticalVelocity;
    private Vector3 facingDirection = Vector3.right;
    private float moveXDisableTimer;
    private int gravitySuppressedFrame = -1;
    private bool climbExitMoveActive;
    private float climbExitMoveTimer;
    private Vector3 climbExitMoveVelocity;
    private LayerMask climbExitGroundMask;
    private float climbExitGroundSnapDistance;

    [Header("状态监控")]
    public bool isGrounded;
    public bool isClimbing; 
    [SerializeField] private bool isCloaked;
    [SerializeField]
    private Posture currentPosture;
    public Posture CurrentPosture 
    { 
        get => currentPosture; 
        private set => currentPosture = value; 
    }
    public float VerticalVelocity => verticalVelocity;
    public int JumpType { get; private set; }
    public float DashPosture { get; private set; }
    public bool UltraDashActive { get; private set; }
    public float ClimbInput { get; private set; }
    public bool IsCloaked => isCloaked;
    public bool IsClimbExitMoveActive => climbExitMoveActive;
    public bool UsesEquippedSkillLoadout => hasExplicitEquippedSkills || (equippedSkills != null && equippedSkills.Count > 0);
    public bool IsInClimbTransitionTrigger => climbTransitionTriggerCount > 0;
    public ClimbTransitionTrigger ActiveClimbTransitionTrigger { get; private set; }
    public bool IsInClimbVolume => ActiveClimbTransitionTrigger != null && ActiveClimbTransitionTrigger.ActsAsClimbVolume;
    private int climbTransitionTriggerCount;
    private bool climbExitUpRequested;
    private bool climbExitDownRequested;
    private bool climbExitUpAnimationLock;
    private float climbExitUpAnimationLockTimer;
    private bool hasQueuedClimbExitUpForcedMove;
    private Vector2 queuedClimbExitUpForcedOffset;
    private float queuedClimbExitUpForcedMoveDuration;

    [Header("技能系统 (Slot-Based)")]
    [Tooltip("可选：调试或特殊关卡开局自带技能。正式流程可留空，移动/跳跃/冲刺由拾取和 UI 解锁。")]
    public List<SkillBase> startingSkills = new List<SkillBase>();

    public List<SkillBase> unlockedSkills = new List<SkillBase>();

    public List<SkillBase> equippedSkills = new List<SkillBase>();

    public SkillDatabase masterDatabase; 
    private readonly List<SkillBase> skillUpdateBuffer = new List<SkillBase>();
    private readonly HashSet<SkillBase> skillUpdateSet = new HashSet<SkillBase>();
    private readonly RaycastHit[] skillLoadoutSurfaceHitBuffer = new RaycastHit[SurfaceHitBufferSize];
    private bool hasExplicitEquippedSkills;

    [Header("地面检测调试")]
    [SerializeField] private bool drawGroundedGizmo = true;
    [SerializeField] private float groundedGizmoOffsetY;

    [Header("技能装配限制")]
    [SerializeField] private List<string> skillLoadoutSurfaceTags = new List<string>()
    {
        "Nature",
        "Water"
    };
    [SerializeField] private LayerMask skillLoadoutSurfaceMask = ~0;
    [SerializeField] private float skillLoadoutSurfaceCheckDistance = 0.25f;

    [Header("攀爬翻越")]
    [SerializeField] private float climbExitUpInputLockDuration = 3f;

    // ===== 02. 技能脚本访问接口 =====

    public CharacterController GetCharacterController() => controlProxy != null ? controlProxy : cc;
    public Transform GetControlTransform() => controlProxyTransform != null ? controlProxyTransform : transform;
    public Vector2 GetRawMoveInput()
    {
        return playerActions.Move.ReadValue<Vector2>();
    }

    public Vector2 GetMoveInput()
    {
        Vector2 input = GetRawMoveInput();
        if (moveXDisableTimer > 0f || climbExitUpAnimationLock)
        {
            input.x = 0f;
        }

        return input;
    }
    
    public bool IsJumpPressed() => CurrentPosture != Posture.Climbing && playerActions.Jump.IsPressed();
    public bool WasJumpPressed() => CurrentPosture != Posture.Climbing && playerActions.Jump.WasPressedThisFrame();
    
    public bool WasJumpReleased() => CurrentPosture != Posture.Climbing && playerActions.Jump.WasReleasedThisFrame();

    public bool IsDashPressed() => CurrentPosture != Posture.Climbing && playerActions.Dash.IsPressed();
    public bool WasDashPressed() => CurrentPosture != Posture.Climbing && playerActions.Dash.WasPressedThisFrame();

    public bool IsHidePressed() => playerActions.Hide.IsPressed();
    public bool WasHidePressed() => playerActions.Hide.WasPressedThisFrame();

    public Vector3 GetFacing() => facingDirection;
    public bool IsDead => playerDeath != null && playerDeath.IsDead;
    public bool IsPosture(Posture posture) => CurrentPosture == posture;
    public void SetVerticalVelocity(float val) => verticalVelocity = val;
    public void RequestGravitySuppressed()
    {
        gravitySuppressedFrame = Time.frameCount;
        verticalVelocity = 0f;
    }
    public void SetJumpType(int type) => JumpType = Mathf.Max(0, type);
    public void SetDashPosture(float posture)
    {
        DashPosture = Mathf.Clamp01(posture);
        if (DashPosture <= 0.01f)
        {
            UltraDashActive = false;
        }
    }
    public void SetUltraDashActive(bool active) => UltraDashActive = active;
    public void DisableMoveXFor(float duration)
    {
        moveXDisableTimer = Mathf.Max(moveXDisableTimer, duration);
    }

    public void ClearMovementLocks()
    {
        moveXDisableTimer = 0f;
        climbExitUpAnimationLock = false;
        climbExitUpAnimationLockTimer = 0f;
        hasQueuedClimbExitUpForcedMove = false;
    }

    public void SetInputEnabled(bool enabled)
    {
        if (controls == null)
        {
            return;
        }

        if (enabled)
        {
            playerActions.Enable();
        }
        else
        {
            playerActions.Disable();
        }
    }
    public void EnableInput()
    {
        SetInputEnabled(true);
    }
    public void DisableInput()
    {
        SetInputEnabled(false);
    }

    // ===== 03. 技能拥有、装备与装配限制 =====

    public bool HasUnlockedSkill(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return false;
        }

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
        for (int i = 0; i < equippedSkills.Count; i++)
        {
            if (equippedSkills[i] is T)
            {
                return true;
            }
        }

        return false;
    }

    public bool CanModifySkillLoadout()
    {
        return IsStandingOnAnyTaggedSurface(skillLoadoutSurfaceTags);
    }

    public void SetEquippedSkills(IList<SkillBase> skills)
    {
        hasExplicitEquippedSkills = true;

        if (equippedSkills == null)
        {
            equippedSkills = new List<SkillBase>();
        }

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

    // ===== 04. 技能解锁、朝向、检查点、死亡与调试绘制 =====

    public void UnlockNewSkill(string id)
    {
        if (masterDatabase == null) return;
        SkillBase newSkill = masterDatabase.GetSkillByID(id);
        UnlockSkill(newSkill);
    }

    private void HandleIntrinsicFacing()
    {
        if (PausePanelController.IsPaused)
        {
            return;
        }

        if (CurrentPosture == Posture.Climbing)
        {
            return;
        }

        float horizontal = GetMoveInput().x;

        if (Mathf.Abs(horizontal) <= turnInputThreshold)
        {
            return;
        }

        SetFacing(horizontal > 0f ? Vector3.right : Vector3.left);
    }

    public void UnlockSkill(SkillBase skill)
    {
        if (skill != null && !unlockedSkills.Contains(skill))
        {
            unlockedSkills.Add(skill);
            SkillUnlocked?.Invoke(skill);
        }
    }

    private void InitializeStartingSkills()
    {
        if (unlockedSkills == null)
        {
            unlockedSkills = new List<SkillBase>();
        }

        if (equippedSkills == null)
        {
            equippedSkills = new List<SkillBase>();
        }

        if (startingSkills != null)
        {
            for (int i = 0; i < startingSkills.Count; i++)
            {
                UnlockSkill(startingSkills[i]);
            }
        }
    }

    public void SetCheckpoint(Vector3 checkpointPosition)
    {
        if (playerDeath != null)
        {
            playerDeath.SetCheckpoint(checkpointPosition);
        }
    }

    public void Die()
    {
        if (playerDeath != null)
        {
            if (isClimbInvincible)
            {
                Debug.Log("[PlayerCC] 无敌状态，死亡被拦截");
                return;
            }
            playerDeath.Die();
        }
    }

    private void OnDrawGizmos()
    {
        if (!drawGroundedGizmo)
        {
            return;
        }

        CharacterController debugCc = cc != null ? cc : GetComponent<CharacterController>();
        if (debugCc == null)
        {
            return;
        }

        Gizmos.color = isGrounded ? Color.green : Color.red;

        Vector3 worldCenter = transform.TransformPoint(debugCc.center);
        float bottomOffset = Mathf.Max(0f, debugCc.height * 0.5f - debugCc.radius);
        Vector3 bottomSphereCenter = worldCenter + Vector3.down * bottomOffset + Vector3.up * groundedGizmoOffsetY;

        Gizmos.DrawWireSphere(bottomSphereCenter, debugCc.radius);
        Gizmos.DrawLine(worldCenter, bottomSphereCenter);
    }

    // ===== 05. 技能装配地面检测 =====

    private bool IsStandingOnAnyTaggedSurface(List<string> requiredTags)
    {
        if (requiredTags == null || requiredTags.Count == 0)
        {
            return true;
        }

        int hitCount = GetSkillLoadoutSurfaceHits();
        return HasAnyTaggedSurfaceHit(skillLoadoutSurfaceHitBuffer, hitCount, requiredTags);
    }

    private int GetSkillLoadoutSurfaceHits()
    {
        CharacterController activeController = GetCharacterController();
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

    // ===== 06. 朝向、移动与控制代理 =====

    public void SetFacing(Vector3 dir)
    {
        if (dir.sqrMagnitude <= 0.01f)
        {
            return;
        }

        facingDirection = dir;
        GetControlTransform().forward = dir.normalized;
    }

    public CollisionFlags MoveCharacter(Vector3 delta)
    {
        CharacterController activeController = GetCharacterController();
        if (activeController == null)
        {
            return CollisionFlags.None;
        }

        CollisionFlags moveFlags = activeController.Move(delta);
        HandleMoveCollisionFlags(moveFlags, delta);
        return moveFlags;
    }


    public void PlaceCapsuleBottomAt(Vector3 bottomPosition)
    {
        if (cc == null)
        {
            transform.position = new Vector3(bottomPosition.x, bottomPosition.y, 0f);
            return;
        }

        Vector3 targetPosition = bottomPosition;
        targetPosition.y = bottomPosition.y - cc.center.y + cc.height * 0.5f;

        bool wasEnabled = cc.enabled;
        cc.enabled = false;
        transform.position = new Vector3(targetPosition.x, targetPosition.y, 0f);
        cc.enabled = wasEnabled;
    }


    public void BeginControlProxy(CharacterController proxy)
    {
        if (proxy == null)
        {
            return;
        }

        ownerControllerEnabledBeforeProxy = cc != null && cc.enabled;
        if (cc != null)
        {
            cc.enabled = false;
        }

        DisableOwnerCollidersForProxy(proxy.transform);
        controlProxy = proxy;
        controlProxyTransform = proxy.transform;
        controlProxy.enabled = true;
        controlProxyTransform.forward = facingDirection;
    }

    public void EndControlProxy(bool teleportPlayerToProxy)
    {
        if (controlProxy == null)
        {
            return;
        }

        Vector3 proxyPosition = controlProxy.transform.position;
        Vector3 proxyForward = controlProxy.transform.forward;

        controlProxy = null;
        controlProxyTransform = null;

        if (teleportPlayerToProxy)
        {
            if (cc != null)
            {
                cc.enabled = false;
            }

            transform.position = new Vector3(proxyPosition.x, proxyPosition.y, 0f);
            transform.forward = proxyForward.sqrMagnitude > 0.01f ? proxyForward.normalized : facingDirection;
        }

        if (cc != null)
        {
            cc.enabled = ownerControllerEnabledBeforeProxy;
        }

        RestoreOwnerCollidersForProxy();

        if (proxyForward.sqrMagnitude > 0.01f)
        {
            facingDirection = proxyForward.normalized;
        }
    }

    private void DisableOwnerCollidersForProxy(Transform proxyRoot)
    {
        RestoreOwnerCollidersForProxy();

        Collider[] colliders = GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            Collider targetCollider = colliders[i];
            if (targetCollider == null || !targetCollider.enabled)
            {
                continue;
            }

            if (proxyRoot != null && targetCollider.transform.IsChildOf(proxyRoot))
            {
                continue;
            }

            targetCollider.enabled = false;
            disabledOwnerCollidersForProxy.Add(targetCollider);
        }
    }

    private void RestoreOwnerCollidersForProxy()
    {
        for (int i = 0; i < disabledOwnerCollidersForProxy.Count; i++)
        {
            Collider targetCollider = disabledOwnerCollidersForProxy[i];
            if (targetCollider != null)
            {
                targetCollider.enabled = true;
            }
        }

        disabledOwnerCollidersForProxy.Clear();
    }

    // ===== 07. 攀爬状态、翻越请求与过渡触发器 =====

    public void SetClimbState(bool climbing, float input)
    {
        isClimbing = climbing;
        ClimbInput = climbing ? Mathf.Clamp(input, -1f, 1f) : 0f;


        RefreshPosture();
    }

    public void EnterClimbTransitionTrigger()
    {
        EnterClimbTransitionTrigger(null);
    }

    public void EnterClimbTransitionTrigger(ClimbTransitionTrigger trigger)
    {
        climbTransitionTriggerCount++;
        if (trigger != null)
        {
            ActiveClimbTransitionTrigger = trigger;
        }

        if (trigger != null && trigger.DebugLogs)
        {
            Debug.Log($"[PlayerCC] EnterClimbTransitionTrigger count={climbTransitionTriggerCount}, active={trigger.name}", this);
        }
    }

    public void ExitClimbTransitionTrigger()
    {
        ExitClimbTransitionTrigger(null);
    }

    public void ExitClimbTransitionTrigger(ClimbTransitionTrigger trigger)
    {
        climbTransitionTriggerCount = Mathf.Max(0, climbTransitionTriggerCount - 1);
        if (trigger == null || ActiveClimbTransitionTrigger == trigger)
        {
            ActiveClimbTransitionTrigger = climbTransitionTriggerCount > 0 ? ActiveClimbTransitionTrigger : null;
        }

        if (trigger != null && trigger.DebugLogs)
        {
            Debug.Log($"[PlayerCC] ExitClimbTransitionTrigger count={climbTransitionTriggerCount}, active={(ActiveClimbTransitionTrigger != null ? ActiveClimbTransitionTrigger.name : "null")}", this);
        }
    }


    public void RequestClimbExitUpAnimation()
    {
        climbExitUpRequested = true;
        BeginClimbExitUpAnimationLock();
    }

    public void RequestClimbExitDownAnimation()
    {
        climbExitDownRequested = true;
    }

    public bool ConsumeClimbExitUpAnimationRequest()
    {
        if (!climbExitUpRequested)
        {
            return false;
        }

        climbExitUpRequested = false;
        return true;
    }

    public void BeginClimbExitUpAnimationLock()
    {
        climbExitUpAnimationLock = true;
        float lockDuration = Mathf.Max(MinClimbExitUpInputLockDuration, climbExitUpInputLockDuration);
        climbExitUpAnimationLockTimer = Mathf.Max(climbExitUpAnimationLockTimer, lockDuration);
    }

    public void FinishClimbExitUpAnimationLock()
    {
        climbExitUpAnimationLock = false;
        climbExitUpAnimationLockTimer = 0f;
    }

    public void QueueClimbExitUpForcedMove(Vector2 offset, float duration)
    {
        hasQueuedClimbExitUpForcedMove = true;
        queuedClimbExitUpForcedOffset = offset;
        queuedClimbExitUpForcedMoveDuration = Mathf.Max(0.01f, duration);
    }

    public void CompleteClimbExitUpAnimation()
    {
        FinishClimbExitUpAnimationLock();

        if (hasQueuedClimbExitUpForcedMove)
        {
            hasQueuedClimbExitUpForcedMove = false;
            BeginClimbExitMove(queuedClimbExitUpForcedOffset, queuedClimbExitUpForcedMoveDuration);
            return;
        }

        RequestGravitySuppressed();
        SetClimbState(false, 0f);
    }

    public bool ConsumeClimbExitDownAnimationRequest()
    {
        if (!climbExitDownRequested)
        {
            return false;
        }

        climbExitDownRequested = false;
        return true;
    }


    public void BeginClimbExitMove(Vector2 offset, float duration)
    {
        BeginClimbExitMove(offset, duration, default, 0f);
    }

    public void BeginClimbExitMove(Vector2 offset, float duration, LayerMask groundMask, float groundSnapDistance)
    {
        if (climbExitMoveActive)
        {
            return;
        }

        Vector3 facing = GetFacing().sqrMagnitude > 0.01f ? GetFacing().normalized : Vector3.right;
        Vector3 totalDelta = new Vector3(facing.x * offset.x, offset.y, 0f);
        float safeDuration = Mathf.Max(0.01f, duration);

        climbExitMoveActive = true;
        climbExitMoveTimer = safeDuration;
        climbExitMoveVelocity = totalDelta / safeDuration;
        climbExitGroundMask = groundMask;
        climbExitGroundSnapDistance = Mathf.Max(0f, groundSnapDistance);
        SetVerticalVelocity(0f);
        SetClimbState(true, 0f);
    }


    public bool CanAutoExitUpFromActiveClimbTrigger()
    {
        return ActiveClimbTransitionTrigger != null && ActiveClimbTransitionTrigger.CanAutoExitUp(this);
    }

    // ===== 08. 姿态、隐身与重力状态刷新 =====

    private void RefreshCloakState(bool resolveIfMissing = true)
    {
        if (cloakEffect == null && resolveIfMissing)
        {
            cloakEffect = GetComponent<CloakEffectController>();
        }

        isCloaked = cloakEffect != null && cloakEffect.IsCloaked;
    }

    private IList<SkillBase> GetActiveSkillsForUpdate()
    {
        if (UsesEquippedSkillLoadout)
        {
            return equippedSkills;
        }

        return unlockedSkills;
    }

    private void HandleGravity()
    {
        if (gravitySuppressedFrame == Time.frameCount)
        {
            verticalVelocity = 0f;
            return;
        }

        if (isGrounded && verticalVelocity < 0)
        {
            verticalVelocity = -2f;
        }

        if (CurrentPosture != Posture.Climbing)
        {
            float gravityMultiplier = verticalVelocity < 0f ? fallMultiplier : 1f;
            verticalVelocity += gravity * gravityMultiplier * Time.deltaTime;
        }
        else
        {
            verticalVelocity = 0;
        }
    }

    private void RefreshPosture()
    {
        CharacterController activeController = GetCharacterController();
        isGrounded = activeController != null && activeController.isGrounded;

        if (isClimbing)
        {
            CurrentPosture = Posture.Climbing;
            return;
        }


        CurrentPosture = isGrounded ? Posture.Grounded : Posture.Airborne;
    }

    void LateUpdate()
    {
        Transform activeTransform = GetControlTransform();
        activeTransform.position = new Vector3(activeTransform.position.x, activeTransform.position.y, 0);
    }

    // ===== 09. 死亡表现控制 =====

    public void SetDeathPresentationActive(bool active)
    {
        if (deathPresentationActive == active)
        {
            return;
        }

        deathPresentationActive = active;

        if (active)
        {
            ShowDeathSign(true);
            HidePlayerRenderersForDeath();
            DisablePlayerCollidersForDeath();
        }
        else
        {
            RestorePlayerCollidersAfterDeath();
            RestorePlayerRenderersAfterDeath();
            ShowDeathSign(false);
        }
    }

    // ===== 10. 死亡标志、渲染器与碰撞体显隐 =====

    private void ResolveDeathSign()
    {
        if (deathSign != null)
        {
            return;
        }

        Transform[] children = GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            Transform child = children[i];
            if (child != null && string.Equals(child.name, deathSignName, StringComparison.OrdinalIgnoreCase))
            {
                deathSign = child;
                return;
            }
        }
    }

    private void ShowDeathSign(bool visible)
    {
        ResolveDeathSign();
        if (deathSign != null)
        {
            if (visible && audioSource != null && deathSFX != null)
            {
                audioSource.PlayOneShot(deathSFX);
            }

            deathSign.gameObject.SetActive(visible);
        }
    }

    private void HidePlayerRenderersForDeath()
    {
        rendererVisibilityBeforeDeath.Clear();

        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer targetRenderer = renderers[i];
            if (targetRenderer == null || IsPartOfDeathSign(targetRenderer.transform))
            {
                continue;
            }

            rendererVisibilityBeforeDeath[targetRenderer] = targetRenderer.enabled;
            targetRenderer.enabled = false;
        }
    }

    private void RestorePlayerRenderersAfterDeath()
    {
        foreach (KeyValuePair<Renderer, bool> entry in rendererVisibilityBeforeDeath)
        {
            if (entry.Key != null)
            {
                entry.Key.enabled = entry.Value;
            }
        }

        rendererVisibilityBeforeDeath.Clear();
    }

    private void DisablePlayerCollidersForDeath()
    {
        disabledCollidersForDeath.Clear();

        Collider[] colliders = GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            Collider targetCollider = colliders[i];
            if (targetCollider == null || !targetCollider.enabled || IsPartOfDeathSign(targetCollider.transform))
            {
                continue;
            }

            targetCollider.enabled = false;
            disabledCollidersForDeath.Add(targetCollider);
        }
    }

    private void RestorePlayerCollidersAfterDeath()
    {
        for (int i = 0; i < disabledCollidersForDeath.Count; i++)
        {
            Collider targetCollider = disabledCollidersForDeath[i];
            if (targetCollider != null)
            {
                targetCollider.enabled = true;
            }
        }

        disabledCollidersForDeath.Clear();
    }

    private bool IsPartOfDeathSign(Transform target)
    {
        ResolveDeathSign();
        return deathSign != null && target != null && target.IsChildOf(deathSign);
    }

    // ===== 11. Unity 生命周期与主循环 =====

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();
        playerDeath = GetComponent<PlayerDeath>();
        if (playerDeath == null)
        {
            playerDeath = gameObject.AddComponent<PlayerDeath>();
        }

        controls = new PlayerControls();
        playerActions = controls.Player;
        cloakEffect = GetComponent<CloakEffectController>();
        ResolveDeathSign();
        if (hideDeathSignOnAwake)
        {
            ShowDeathSign(false);
        }

        isGrounded = false;
        CurrentPosture = Posture.Airborne;
        SetClimbState(false, 0f);
        InitializeStartingSkills();
    }

    void OnEnable()
    {
        if (controls != null)
        {
            playerActions.Enable();
        }
    }

    void OnDisable()
    {
        if (controls != null)
        {
            playerActions.Disable();
        }
    }

    void OnDestroy()
    {
        if (controls != null)
        {
            controls.Dispose();
            controls = null;
        }
    }

    void Update()
    {
        if (IsDead)
        {
            return;
        }

        TickMovementLocks(Time.deltaTime);
        RefreshPosture();
        RefreshCloakState(false);
        HandleIntrinsicFacing();
        UpdateActiveSkills();
        UpdateClimbExitMove();
        RefreshCloakState();
        HandleGravityMove();
    }

    // ===== 12. 每帧移动、技能更新与物理辅助 =====

    private void TickMovementLocks(float deltaTime)
    {
        if (moveXDisableTimer > 0f)
        {
            moveXDisableTimer -= deltaTime;
        }

        if (!climbExitUpAnimationLock)
        {
            return;
        }

        climbExitUpAnimationLockTimer -= deltaTime;
        if (climbExitUpAnimationLockTimer <= 0f)
        {
            FinishClimbExitUpAnimationLock();
        }
    }

    private void UpdateActiveSkills()
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

            skill.OnUpdate(gameObject, this, CurrentPosture);
        }
    }

    private void HandleGravityMove()
    {
        HandleGravity();

        Vector3 gravityDelta = new Vector3(0, verticalVelocity, 0) * Time.deltaTime;
        CharacterController activeController = GetCharacterController();
        if (activeController == null)
        {
            return;
        }

        CollisionFlags gravityFlags = activeController.Move(gravityDelta);
        HandleMoveCollisionFlags(gravityFlags, gravityDelta);
    }

    private void UpdateClimbExitMove()
    {
        if (!climbExitMoveActive)
        {
            return;
        }

        float step = Mathf.Min(Time.deltaTime, climbExitMoveTimer);
        SetVerticalVelocity(0f);
        SetClimbState(true, 0f);
        MoveCharacter(climbExitMoveVelocity * step);

        climbExitMoveTimer -= step;
        if (climbExitMoveTimer > 0f)
        {
            return;
        }

        climbExitMoveActive = false;
        climbExitMoveTimer = 0f;
        climbExitMoveVelocity = Vector3.zero;
        SnapClimbExitMoveToGround();
        RequestGravitySuppressed();
        SetClimbState(false, 0f);
    }

    private void SnapClimbExitMoveToGround()
    {
        if (cc == null || climbExitGroundMask.value == 0 || climbExitGroundSnapDistance <= 0f)
        {
            return;
        }

        Vector3 worldCenter = transform.TransformPoint(cc.center);
        float bottomOffset = Mathf.Max(0f, cc.height * 0.5f - cc.radius);
        float centerToBottom = bottomOffset + cc.radius;
        Vector3 rayOrigin = worldCenter + Vector3.up * 0.25f;

        if (!Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, climbExitGroundSnapDistance, climbExitGroundMask))
        {
            return;
        }

        Vector3 snappedPosition = transform.position;
        snappedPosition.y = hit.point.y - cc.center.y + centerToBottom;

        bool wasEnabled = cc.enabled;
        cc.enabled = false;
        transform.position = new Vector3(snappedPosition.x, snappedPosition.y, 0f);
        cc.enabled = wasEnabled;
    }

    private void HandleMoveCollisionFlags(CollisionFlags flags, Vector3 attemptedMove)
    {
        if ((flags & CollisionFlags.Above) == 0)
        {
            return;
        }

        if (attemptedMove.y <= 0f && verticalVelocity <= 0f)
        {
            return;
        }

        verticalVelocity = ceilingHitFallVelocity < 0f ? ceilingHitFallVelocity : -0.5f;
    }

}
