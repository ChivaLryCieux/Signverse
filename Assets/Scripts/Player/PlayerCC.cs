using UnityEngine;
using Skills; // 技能命名空间

[RequireComponent(typeof(CharacterController))]
public class PlayerCC : MonoBehaviour
{
    private CharacterController cc;
    private PlayerControls controls; // 与生成的 Input Action 类名一致
    
    [Header("核心物理")]
    public float gravity = -25f;
    private float verticalVelocity;
    private Vector3 facingDirection = Vector3.right;
    
    [Header("状态监控")]
    public bool isGrounded;
    public bool isClimbing; // 由 MoveSkill 实时控制

    [Header("技能槽位 (拖入 .asset 文件)")]
    public SkillBase moveSkill;
    public SkillBase jumpSkill;

    [Header("摔死检测")]
    public float deathDistance = 5.0f;
    private float airStartY;
    private bool wasGrounded;

    // --- 供技能脚本调用的 Public 接口 ---
    public CharacterController GetCharacterController() => cc;
    
    // 获取 WASD 的 Vector2 输入
    public Vector2 GetMoveInput() => controls.Player.Move.ReadValue<Vector2>();
    
    // 获取空格键是否被按住 (用于持续爬墙)
    public bool IsJumpPressed() => controls.Player.Jump.IsPressed();
    
    // 获取当前面向 (Vector3.right 或 Vector3.left)
    public Vector3 GetFacing() => facingDirection;

    // 设置纵向速度 (跳跃技能调用)
    public void SetVerticalVelocity(float val) => verticalVelocity = val;

    // 设置朝向
    public void SetFacing(Vector3 dir)
    {
        facingDirection = dir;
        transform.forward = dir;
    }

    // --- 生命周期 ---
    void Awake()
    {
        cc = GetComponent<CharacterController>();
        controls = new PlayerControls(); // 如果你的名字改了，这里也要改
    }

    void OnEnable() => controls.Player.Enable();
    void OnDisable() => controls.Player.Disable();

    void Update()
    {
        // 1. 环境检测 (使用 CC 自带检测)
        isGrounded = cc.isGrounded;

        // 2. 执行移动逻辑 (包含爬墙检测)
        if (moveSkill != null)
        {
            moveSkill.OnUpdate(gameObject, this);
        }

        // 3. 处理重力与跳跃触发
        if (isGrounded && verticalVelocity < 0)
        {
            verticalVelocity = -2f; // 贴地力
        }

        if (!isClimbing)
        {
            // 只有不在爬墙时才受重力影响
            verticalVelocity += gravity * Time.deltaTime;

            // 地面跳跃触发
            if (controls.Player.Jump.WasPressedThisFrame() && isGrounded)
            {
                if (jumpSkill != null) jumpSkill.OnActivate(gameObject, this);
            }
        }
        else
        {
            // 爬墙时重力置零，垂直位移由 MoveSkill 完全接管
            verticalVelocity = 0;
        }

        // 4. 执行垂直位移
        cc.Move(new Vector3(0, verticalVelocity, 0) * Time.deltaTime);

        // 5. 摔死检测
        HandleFallDeath();
    }

    void LateUpdate()
    {
        // 强力锁定 Z 轴，防止 2.5D 游戏中角色前后偏移
        transform.position = new Vector3(transform.position.x, transform.position.y, 0);
    }

    private void HandleFallDeath()
    {
        // 离开地面瞬间
        if (wasGrounded && !isGrounded)
        {
            airStartY = transform.position.y;
        }

        // 落地瞬间
        if (!wasGrounded && isGrounded)
        {
            float fallHeight = airStartY - transform.position.y;
            if (fallHeight > deathDistance)
            {
                Die();
            }
        }
        wasGrounded = isGrounded;
    }

    private void Die()
    {
        Debug.Log("<color=red><b>角色摔死了！</b></color>");
        // 重生逻辑：比如重载场景或回到存盘点
    }

    private void OnDrawGizmos()
    {
        // 画出脚底位置，方便调试射线起始点
        Gizmos.color = isGrounded ? Color.green : Color.red;
        Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.1f, 0.2f);
    }
}