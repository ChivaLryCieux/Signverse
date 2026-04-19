using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using Skills; 
using System;

[RequireComponent(typeof(CharacterController))]
public class PlayerCC : MonoBehaviour
{
    public event Action<SkillBase> SkillUnlocked;

    [Header("核心引用")]
    private CharacterController cc;
    private PlayerControls controls; 

    [Header("物理参数")]
    public float gravity = -25f;
    private float verticalVelocity;
    private Vector3 facingDirection = Vector3.right;

    [Header("状态监控")]
    public bool isGrounded;
    public bool isClimbing; 

    [Header("技能系统 (Slot-Based)")]
    [Tooltip("可选：调试或特殊关卡开局自带技能。正式流程可留空，移动/跳跃/冲刺由拾取和 UI 解锁。")]
    public List<SkillBase> startingSkills = new List<SkillBase>();

    public List<SkillBase> unlockedSkills = new List<SkillBase>();
    public SkillDatabase masterDatabase; 

    [Header("摔死检测")]
    public float deathDistance = 8.0f;
    public float respawnDelay = 3.0f;
    private float airStartY;
    private bool wasGrounded;

    [Header("重生状态")]
    [SerializeField] private Vector3 currentCheckpoint;
    [SerializeField] private bool isDead;

    // --- 给技能脚本提供的“遥控器”接口 ---
    public CharacterController GetCharacterController() => cc;
    public Vector2 GetMoveInput() => controls.Player.Move.ReadValue<Vector2>();
    
    // 供蓄力跳检测：空格是否正被按住
    public bool IsJumpPressed() => controls.Player.Jump.IsPressed();
    public bool WasJumpPressed() => controls.Player.Jump.WasPressedThisFrame();
    
    // 供技能检测：空格是否在这一帧松开
    public bool WasJumpReleased() => controls.Player.Jump.WasReleasedThisFrame();

    public bool IsDashPressed() => controls.Player.Dash.IsPressed();
    public bool WasDashPressed() => controls.Player.Dash.WasPressedThisFrame();

    public bool IsHidePressed() => controls.Player.Hide.IsPressed();
    public bool WasHidePressed() => controls.Player.Hide.WasPressedThisFrame();

    public Vector3 GetFacing() => facingDirection;
    public bool IsDead => isDead;
    public void SetVerticalVelocity(float val) => verticalVelocity = val;
    public void SetFacing(Vector3 dir)
    {
        facingDirection = dir;
        transform.forward = dir;
    }

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        controls = new PlayerControls();
        isGrounded = false;
        isClimbing = false;
        isDead = false;
        currentCheckpoint = transform.position;
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
        if (isDead)
        {
            return;
        }

        isGrounded = cc.isGrounded;

        for (int i = 0; i < unlockedSkills.Count; i++)
        {
            SkillBase skill = unlockedSkills[i];
            if (skill == null)
            {
                continue;
            }

            skill.OnUpdate(gameObject, this);
        }

        HandleGravity();

        cc.Move(new Vector3(0, verticalVelocity, 0) * Time.deltaTime);

        HandleFallDeath();
    }

    private void HandleGravity()
    {
        if (isGrounded && verticalVelocity < 0)
        {
            verticalVelocity = -2f; 
        }

        if (!isClimbing)
        {
            verticalVelocity += gravity * Time.deltaTime;
        }
        else
        {
            verticalVelocity = 0;
        }
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
        currentCheckpoint = checkpointPosition;
        Debug.Log($"<color=green>已更新存档点：</color>{currentCheckpoint}");
    }

    private void HandleFallDeath()
    {
        if (wasGrounded && !isGrounded) airStartY = transform.position.y;
        if (!wasGrounded && isGrounded)
        {
            float fallHeight = airStartY - transform.position.y;
            if (fallHeight > deathDistance) Die();
        }
        wasGrounded = isGrounded;
    }

    private void Die()
    {
        if (isDead)
        {
            return;
        }

        isDead = true;
        verticalVelocity = 0f;
        isClimbing = false;
        isGrounded = false;
        controls.Player.Disable();
        cc.enabled = false;

        Debug.Log("<color=red>角色死亡！3 秒后将在存档点复活。</color>");
        StartCoroutine(RespawnAfterDelay());
    }

    private IEnumerator RespawnAfterDelay()
    {
        yield return new WaitForSeconds(respawnDelay);
        RespawnAtCheckpoint();
    }

    private void RespawnAtCheckpoint()
    {
        Vector3 respawnPosition = new Vector3(currentCheckpoint.x, currentCheckpoint.y, 0f);

        transform.position = respawnPosition;
        facingDirection = Vector3.right;
        transform.forward = facingDirection;

        verticalVelocity = -2f;
        airStartY = respawnPosition.y;
        wasGrounded = false;
        isClimbing = false;
        isDead = false;

        cc.enabled = true;
        controls.Player.Enable();

        Debug.Log("<color=cyan>角色已在存档点复活。</color>");
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = isGrounded ? Color.green : Color.red;
        Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.1f, 0.2f);
    }
}
