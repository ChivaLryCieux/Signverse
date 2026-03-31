using UnityEngine;
using System.Collections.Generic;
using Skills; 

[RequireComponent(typeof(CharacterController))]
public class PlayerCC : MonoBehaviour
{
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
    public SkillBase moveSkill; 
    public SkillBase jumpSkill; 
    
    public List<SkillBase> unlockedSkills = new List<SkillBase>();
    public SkillDatabase masterDatabase; 

    [Header("摔死检测")]
    public float deathDistance = 5.0f;
    private float airStartY;
    private bool wasGrounded;

    // --- 给技能脚本提供的“遥控器”接口 ---
    public CharacterController GetCharacterController() => cc;
    public Vector2 GetMoveInput() => controls.Player.Move.ReadValue<Vector2>();
    
    // 供蓄力跳检测：空格是否正被按住
    public bool IsJumpPressed() => controls.Player.Jump.IsPressed();
    
    // 供技能检测：空格是否在这一帧松开
    public bool WasJumpReleased() => controls.Player.Jump.WasReleasedThisFrame();

    public Vector3 GetFacing() => facingDirection;
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
    }

    void OnEnable() => controls.Player.Enable();
    void OnDisable() => controls.Player.Disable();

    void Update()
    {
        isGrounded = cc.isGrounded;

        // 1. 基础移动逻辑
        if (moveSkill != null) moveSkill.OnUpdate(gameObject, this);

        // 2. 只有每帧执行 OnUpdate，LongJumpSkill 才能计算蓄力时间和处理空中惯性
        if (jumpSkill != null) jumpSkill.OnUpdate(gameObject, this);

        // 3. 遍历执行其他已解锁技能
        foreach (var skill in unlockedSkills)
        {
            skill.OnUpdate(gameObject, this);
        }

        // 4. 处理物理重力与触发信号
        HandleGravityAndJump();

        // 5. 执行垂直位移
        cc.Move(new Vector3(0, verticalVelocity, 0) * Time.deltaTime);

        HandleFallDeath();
    }

    private void HandleGravityAndJump()
    {
        if (isGrounded && verticalVelocity < 0)
        {
            verticalVelocity = -2f; 
        }

        if (!isClimbing)
        {
            verticalVelocity += gravity * Time.deltaTime;

            // 发送起跳信号：如果是蓄力跳，OnActivate 用于标记“开始蓄力”
            if (controls.Player.Jump.WasPressedThisFrame() && isGrounded)
            {
                if (jumpSkill != null) jumpSkill.OnActivate(gameObject, this);
            }
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
        if (newSkill != null && !unlockedSkills.Contains(newSkill))
        {
            unlockedSkills.Add(newSkill);
        }
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

    private void Die() => Debug.Log("<color=red>角色摔死了！</color>");

    private void OnDrawGizmos()
    {
        Gizmos.color = isGrounded ? Color.green : Color.red;
        Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.1f, 0.2f);
    }
}