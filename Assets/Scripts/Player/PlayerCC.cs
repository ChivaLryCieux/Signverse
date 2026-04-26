using UnityEngine;
using System.Collections.Generic;
using Skills; 
using System;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerDeath))]
public class PlayerCC : MonoBehaviour
{
    public enum Posture
    {
        Grounded,
        Airborne,
        Climbing
    }

    public event Action<SkillBase> SkillUnlocked;

    [Header("核心引用")]
    private CharacterController cc;
    private PlayerControls controls; 
    private PlayerDeath playerDeath;

    [Header("物理参数")]
    public float gravity = -25f;
    [Tooltip("下落时的额外重力倍率，用于让 VerticalVelocity 更快进入下落段。")]
    public float fallMultiplier = 1.6f;
    [SerializeField] private float turnInputThreshold = 0.1f;
    private float verticalVelocity;
    private Vector3 facingDirection = Vector3.right;
    private float moveXDisableTimer;

    [Header("状态监控")]
    public bool isGrounded;
    public bool isClimbing; 
    public Posture CurrentPosture { get; private set; }
    public float VerticalVelocity => verticalVelocity;
    public int JumpType { get; private set; }
    public float DashPosture { get; private set; }
    public float ClimbInput { get; private set; }
    public bool IsInClimbTransitionTrigger => climbTransitionTriggerCount > 0;
    private int climbTransitionTriggerCount;
    private bool climbExitUpRequested;
    private bool climbExitDownRequested;

    [Header("技能系统 (Slot-Based)")]
    [Tooltip("可选：调试或特殊关卡开局自带技能。正式流程可留空，移动/跳跃/冲刺由拾取和 UI 解锁。")]
    public List<SkillBase> startingSkills = new List<SkillBase>();

    public List<SkillBase> unlockedSkills = new List<SkillBase>();
    public SkillDatabase masterDatabase; 

    [Header("地面检测调试")]
    [SerializeField] private bool drawGroundedGizmo = true;
    [SerializeField] private float groundedGizmoOffsetY;

    // --- 给技能脚本提供的“遥控器”接口 ---
    public CharacterController GetCharacterController() => cc;
    public Vector2 GetRawMoveInput()
    {
        return controls.Player.Move.ReadValue<Vector2>();
    }

    public Vector2 GetMoveInput()
    {
        Vector2 input = GetRawMoveInput();
        if (moveXDisableTimer > 0f)
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
    public void SetJumpType(int type) => JumpType = Mathf.Max(0, type);
    public void SetDashPosture(float posture) => DashPosture = Mathf.Clamp01(posture);
    public void DisableMoveXFor(float duration)
    {
        moveXDisableTimer = Mathf.Max(moveXDisableTimer, duration);
    }

    public void ClearMovementLocks()
    {
        moveXDisableTimer = 0f;
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

    public void SetClimbState(bool climbing, float input)
    {
        isClimbing = climbing;
        ClimbInput = climbing ? Mathf.Clamp(input, -1f, 1f) : 0f;
        RefreshPosture();
    }

    public void EnterClimbTransitionTrigger()
    {
        climbTransitionTriggerCount++;
    }

    public void ExitClimbTransitionTrigger()
    {
        climbTransitionTriggerCount = Mathf.Max(0, climbTransitionTriggerCount - 1);
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

    public void RequestClimbExitUpAnimation()
    {
        climbExitUpRequested = true;
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
        transform.forward = dir.normalized;
    }

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        playerDeath = GetComponent<PlayerDeath>();
        if (playerDeath == null)
        {
            playerDeath = gameObject.AddComponent<PlayerDeath>();
        }

        controls = new PlayerControls();
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

        RefreshPosture();

        HandleIntrinsicFacing();

        for (int i = 0; i < unlockedSkills.Count; i++)
        {
            SkillBase skill = unlockedSkills[i];
            if (skill == null)
            {
                continue;
            }

            skill.OnUpdate(gameObject, this, CurrentPosture);
        }

        HandleGravity();

        cc.Move(new Vector3(0, verticalVelocity, 0) * Time.deltaTime);
    }
    private void HandleGravity()
    {
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
        isGrounded = cc != null && cc.isGrounded;

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
        transform.position = new Vector3(transform.position.x, transform.position.y, 0);
    }

    public void UnlockNewSkill(string id)
    {
        if (masterDatabase == null) return;
        SkillBase newSkill = masterDatabase.GetSkillByID(id);
        UnlockSkill(newSkill);
    }

    private void HandleIntrinsicFacing()
    {
        if (CurrentPosture == Posture.Climbing)
        {
            return;
        }

        float horizontal = GetRawMoveInput().x;

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
