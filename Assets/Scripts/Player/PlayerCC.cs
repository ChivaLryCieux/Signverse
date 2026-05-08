using UnityEngine;
using System.Collections.Generic;
using Skills; 
using System;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerDeath))]
public class PlayerCC : MonoBehaviour
{
    private const float MinClimbExitUpInputLockDuration = 3f;
    public AudioSource audioSource;
    public AudioClip deathSFX;

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
    private PlayerDeath playerDeath;
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

    [Header("防挤出平台")]
    [SerializeField] private bool preventSideCollisionPushOffGround = true;
    [SerializeField] private LayerMask groundSafetyMask = ~0;
    [SerializeField] private float groundSafetyCheckDistance = 0.35f;
    [SerializeField] private float groundSafetyRadiusPadding = 0.03f;
    [SerializeField] private bool drawGroundSafetyCheck;
    [SerializeField] private float ceilingHitFallVelocity = -0.5f;

    [Header("前方 Trigger 移动阻挡")]
    [SerializeField] private LayerMask directionalMoveBlockMask = ~0;
    [SerializeField] private bool drawDirectionalMoveBlock;

    private float verticalVelocity;
    private Vector3 facingDirection = Vector3.right;
    private float moveXDisableTimer;
    private int gravitySuppressedFrame = -1;
    private bool climbExitMoveActive;
    private float climbExitMoveTimer;
    private Vector3 climbExitMoveVelocity;
    private LayerMask climbExitGroundMask;
    private float climbExitGroundSnapDistance;
    private readonly List<DirectionalMoveBlockContact> directionalMoveBlockContacts = new List<DirectionalMoveBlockContact>();

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

    private struct DirectionalMoveBlockContact
    {
        public Collider other;
        public Transform trigger;
    }

    [Header("技能系统 (Slot-Based)")]
    [Tooltip("可选：调试或特殊关卡开局自带技能。正式流程可留空，移动/跳跃/冲刺由拾取和 UI 解锁。")]
    public List<SkillBase> startingSkills = new List<SkillBase>();

    public List<SkillBase> unlockedSkills = new List<SkillBase>();

    // Lry的修改：当前装备槽最终生效的技能列表。unlockedSkills 表示“已拥有/可用能力集合”，equippedSkills 表示“当前装配 loadout”，动画脚本应优先读取这里。
    public List<SkillBase> equippedSkills = new List<SkillBase>();

    public SkillDatabase masterDatabase; 
    private readonly List<SkillBase> skillUpdateBuffer = new List<SkillBase>();
    private bool hasExplicitEquippedSkills;

    [Header("地面检测调试")]
    [SerializeField] private bool drawGroundedGizmo = true;
    [SerializeField] private float groundedGizmoOffsetY;

    [Header("技能装配限制")]
    [SerializeField] private string skillLoadoutSurfaceTag = "Nature";
    [SerializeField] private LayerMask skillLoadoutSurfaceMask = ~0;
    [SerializeField] private float skillLoadoutSurfaceCheckDistance = 0.25f;

    [Header("攀爬翻越")]
    [SerializeField] private float climbExitUpInputLockDuration = 3f;

    // --- 给技能脚本提供的“遥控器”接口 ---
    public CharacterController GetCharacterController() => controlProxy != null ? controlProxy : cc;
    public Transform GetControlTransform() => controlProxyTransform != null ? controlProxyTransform : transform;
    public Vector2 GetRawMoveInput()
    {
        return controls.Player.Move.ReadValue<Vector2>();
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
    
    // 供蓄力跳检测：空格是否正被按住
    public bool IsJumpPressed() => CurrentPosture != Posture.Climbing && controls.Player.Jump.IsPressed();
    public bool WasJumpPressed() => CurrentPosture != Posture.Climbing && controls.Player.Jump.WasPressedThisFrame();
    
    // 供技能检测：空格是否在这一帧松开
    public bool WasJumpReleased() => CurrentPosture != Posture.Climbing && controls.Player.Jump.WasReleasedThisFrame();

    public bool IsDashPressed() => CurrentPosture != Posture.Climbing && controls.Player.Dash.IsPressed();
    public bool WasDashPressed() => CurrentPosture != Posture.Climbing && controls.Player.Dash.WasPressedThisFrame();

    public bool IsHidePressed() => controls.Player.Hide.IsPressed();
    public bool WasHidePressed() => controls.Player.Hide.WasPressedThisFrame();

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
        directionalMoveBlockContacts.Clear();
    }

    public void SetInputEnabled(bool enabled)
    {
        if (controls == null)
        {
            return;
        }

        if (enabled)
        {
            controls.Player.Enable();
        }
        else
        {
            controls.Player.Disable();
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

    // Lry的修改：按 skillID 查询当前装备技能。给 AnimatorStateDebugger 等表现层使用，避免表现层直接理解 UI 装备槽内部结构。
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

    // Lry的修改：按具体 SkillBase 类型查询当前装备技能，方便动画或关卡逻辑写强类型判断。
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
        return IsStandingOnTaggedSurface(skillLoadoutSurfaceTag);
    }

    // Lry的修改：由 UI 装备系统统一提交当前装备技能快照。使用快照同步可以避免动画脚本读取 UI 私有数组，降低模块耦合。
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

    public bool ConsumeClimbExitDownAnimationRequest()
    {
        if (!climbExitDownRequested)
        {
            return false;
        }

        climbExitDownRequested = false;
        return true;
    }

    public void SetFacing(Vector3 dir)
    {
        if (dir.sqrMagnitude <= 0.01f)
        {
            return;
        }

        facingDirection = dir;
        GetControlTransform().forward = dir.normalized;
    }

    public void EnterDirectionalMoveBlockTrigger(Collider other, Transform trigger)
    {
        if (!CanUseDirectionalMoveBlockContact(other, trigger))
        {
            return;
        }

        int index = FindDirectionalMoveBlockContact(other, trigger);
        if (index >= 0)
        {
            return;
        }

        directionalMoveBlockContacts.Add(new DirectionalMoveBlockContact
        {
            other = other,
            trigger = trigger
        });
    }

    public void ExitDirectionalMoveBlockTrigger(Collider other, Transform trigger)
    {
        int index = FindDirectionalMoveBlockContact(other, trigger);
        if (index >= 0)
        {
            directionalMoveBlockContacts.RemoveAt(index);
        }
    }

    public bool CanUseDirectionalMoveBlockContact(Collider other, Transform trigger)
    {
        if (other == null || trigger == null || other.transform.IsChildOf(transform))
        {
            return false;
        }

        return (directionalMoveBlockMask.value & (1 << other.gameObject.layer)) != 0;
    }

    public CollisionFlags MoveWithGroundProtection(Vector3 delta)
    {
        CharacterController activeController = GetCharacterController();
        if (activeController == null)
        {
            return CollisionFlags.None;
        }

        delta = ApplyDirectionalMoveBlock(delta);

        if (!ShouldProtectGroundedSideMove(delta, activeController))
        {
            CollisionFlags moveFlags = activeController.Move(delta);
            HandleMoveCollisionFlags(moveFlags, delta);
            return moveFlags;
        }

        Transform activeTransform = GetControlTransform();
        Vector3 positionBeforeMove = activeTransform.position;
        bool hadGroundSupport = HasGroundSupport(activeController);
        CollisionFlags flags = activeController.Move(delta);
        HandleMoveCollisionFlags(flags, delta);

        if (!hadGroundSupport || (flags & CollisionFlags.Sides) == 0 || HasGroundSupport(activeController))
        {
            return flags;
        }

        bool wasEnabled = activeController.enabled;
        activeController.enabled = false;
        activeTransform.position = positionBeforeMove;
        activeController.enabled = wasEnabled;
        verticalVelocity = Mathf.Min(verticalVelocity, 0f);

        return flags;
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

    public bool CanAutoExitUpFromActiveClimbTrigger()
    {
        return ActiveClimbTransitionTrigger != null && ActiveClimbTransitionTrigger.CanAutoExitUp(this);
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
            audioSource.PlayOneShot(deathSFX);
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
            controls.Player.Enable();
        }
    }

    void OnDisable()
    {
        if (controls != null)
        {
            controls.Player.Disable();
        }
    }

    void Update()
    {
        if (IsDead)
        {
            return;
        }

        if (moveXDisableTimer > 0f)
        {
            moveXDisableTimer -= Time.deltaTime;
        }

        if (climbExitUpAnimationLock)
        {
            climbExitUpAnimationLockTimer -= Time.deltaTime;
            if (climbExitUpAnimationLockTimer <= 0f)
            {
                FinishClimbExitUpAnimationLock();
            }
        }

        RefreshPosture();
        RefreshCloakState();

        HandleIntrinsicFacing();

        IList<SkillBase> activeSkills = GetActiveSkillsForUpdate();
        skillUpdateBuffer.Clear();

        if (activeSkills != null)
        {
            for (int i = 0; i < activeSkills.Count; i++)
            {
                SkillBase skill = activeSkills[i];
                if (skill != null && !skillUpdateBuffer.Contains(skill))
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

        UpdateClimbExitMove();

        RefreshCloakState();

        HandleGravity();

        Vector3 gravityDelta = new Vector3(0, verticalVelocity, 0) * Time.deltaTime;
        CollisionFlags gravityFlags = GetCharacterController().Move(gravityDelta);
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
        MoveWithGroundProtection(climbExitMoveVelocity * step);

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

    private bool ShouldProtectGroundedSideMove(Vector3 delta, CharacterController activeController)
    {
        if (!preventSideCollisionPushOffGround || activeController == null)
        {
            return false;
        }

        if (CurrentPosture != Posture.Grounded)
        {
            return false;
        }

        return Mathf.Abs(delta.x) > 0.0001f && Mathf.Abs(delta.y) <= 0.0001f;
    }

    private Vector2 ApplyDirectionalMoveBlock(Vector2 input)
    {
        if (IsMoveDirectionBlocked(input.x))
        {
            input.x = 0f;
        }

        return input;
    }

    private Vector3 ApplyDirectionalMoveBlock(Vector3 delta)
    {
        if (IsMoveDirectionBlocked(delta.x))
        {
            delta.x = 0f;
        }

        return delta;
    }

    private bool IsMoveDirectionBlocked(float x)
    {
        if (Mathf.Abs(x) <= 0.0001f)
        {
            return false;
        }

        PruneDirectionalMoveBlockContacts();
        float moveDirection = Mathf.Sign(x);
        for (int i = 0; i < directionalMoveBlockContacts.Count; i++)
        {
            if (Mathf.Sign(GetDirectionalMoveBlockSign(directionalMoveBlockContacts[i].other)) == moveDirection)
            {
                return true;
            }
        }

        return false;
    }

    private void PruneDirectionalMoveBlockContacts()
    {
        for (int i = directionalMoveBlockContacts.Count - 1; i >= 0; i--)
        {
            DirectionalMoveBlockContact contact = directionalMoveBlockContacts[i];
            if (contact.other == null || contact.trigger == null || !contact.other.enabled || !CanUseDirectionalMoveBlockContact(contact.other, contact.trigger))
            {
                directionalMoveBlockContacts.RemoveAt(i);
            }
        }
    }

    private int FindDirectionalMoveBlockContact(Collider other, Transform trigger)
    {
        for (int i = 0; i < directionalMoveBlockContacts.Count; i++)
        {
            DirectionalMoveBlockContact contact = directionalMoveBlockContacts[i];
            if (contact.other == other && contact.trigger == trigger)
            {
                return i;
            }
        }

        return -1;
    }

    private float GetDirectionalMoveBlockSign(Collider other)
    {
        Transform activeTransform = GetControlTransform();
        float direction = 0f;
        if (other != null && activeTransform != null)
        {
            Vector3 playerPosition = activeTransform.position;
            Vector3 closestPoint = other.ClosestPoint(playerPosition);
            direction = closestPoint.x - playerPosition.x;

            if (Mathf.Abs(direction) <= 0.0001f)
            {
                direction = other.bounds.center.x - playerPosition.x;
            }
        }

        if (Mathf.Abs(direction) > 0.0001f)
        {
            return Mathf.Sign(direction);
        }

        return facingDirection.x >= 0f ? 1f : -1f;
    }

    private bool HasGroundSupport(CharacterController activeController)
    {
        if (activeController == null || groundSafetyMask.value == 0)
        {
            return false;
        }

        Vector3 worldCenter = activeController.transform.TransformPoint(activeController.center);
        float bottomOffset = Mathf.Max(0f, activeController.height * 0.5f - activeController.radius);
        Vector3 origin = worldCenter + Vector3.down * bottomOffset + Vector3.up * 0.05f;
        float radius = Mathf.Max(0.01f, activeController.radius - Mathf.Max(0f, groundSafetyRadiusPadding));
        float distance = Mathf.Max(0.01f, groundSafetyCheckDistance);
        bool hitGround = Physics.SphereCast(
            origin,
            radius,
            Vector3.down,
            out _,
            distance,
            groundSafetyMask,
            QueryTriggerInteraction.Ignore
        );

        if (drawGroundSafetyCheck)
        {
            Color color = hitGround ? Color.green : Color.red;
            Debug.DrawRay(origin, Vector3.down * distance, color);
            Debug.DrawRay(origin + Vector3.right * radius, Vector3.down * distance, color);
            Debug.DrawRay(origin + Vector3.left * radius, Vector3.down * distance, color);
            Debug.DrawRay(origin + Vector3.forward * radius, Vector3.down * distance, color);
            Debug.DrawRay(origin + Vector3.back * radius, Vector3.down * distance, color);
        }

        return hitGround;
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

    private bool IsStandingOnTaggedSurface(string requiredTag)
    {
        if (string.IsNullOrWhiteSpace(requiredTag))
        {
            return true;
        }

        CharacterController activeController = GetCharacterController();
        if (activeController == null || skillLoadoutSurfaceMask.value == 0)
        {
            return false;
        }

        Vector3 worldCenter = activeController.transform.TransformPoint(activeController.center);
        float bottomOffset = Mathf.Max(0f, activeController.height * 0.5f - activeController.radius);
        Vector3 sphereOrigin = worldCenter + Vector3.down * bottomOffset + Vector3.up * 0.05f;
        float radius = Mathf.Max(0.01f, activeController.radius * 0.9f);
        float distance = Mathf.Max(0.01f, skillLoadoutSurfaceCheckDistance + 0.05f);

        if (!Physics.SphereCast(
                sphereOrigin,
                radius,
                Vector3.down,
                out RaycastHit hit,
                distance,
                skillLoadoutSurfaceMask,
                QueryTriggerInteraction.Ignore))
        {
            return false;
        }

        return hit.collider != null && hit.collider.CompareTag(requiredTag);
    }

    private void RefreshCloakState()
    {
        CloakEffectController cloakEffect = GetComponent<CloakEffectController>();
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
        // 2.5D 锁定 Z 轴
        Transform activeTransform = GetControlTransform();
        activeTransform.position = new Vector3(activeTransform.position.x, activeTransform.position.y, 0);

        if (drawDirectionalMoveBlock)
        {
            DrawDirectionalMoveBlockDebug();
        }
    }

    private void DrawDirectionalMoveBlockDebug()
    {
        PruneDirectionalMoveBlockContacts();
        Vector3 origin = GetControlTransform().position + Vector3.up * 0.5f;
        for (int i = 0; i < directionalMoveBlockContacts.Count; i++)
        {
            float direction = Mathf.Sign(GetDirectionalMoveBlockSign(directionalMoveBlockContacts[i].other));
            Debug.DrawRay(origin, Vector3.right * direction * 0.75f, Color.yellow);
        }
    }

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

        // Lry的修改：保证 equippedSkills 在运行时不为空，方便动画侧直接读取。
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
}
