using UnityEngine;
using UnityEngine.InputSystem; // 必须引用

[RequireComponent(typeof(CharacterController))]
public class PlayerController_InputSystem : MonoBehaviour
{
    private CharacterController controller;
    private PlayerControls controls; // 自动生成的C#类
    private Vector2 moveInput;
    private Vector3 playerVelocity;
    private bool isGrounded;
    private bool isClimbing;
    private bool jumpHeld; // 是否正按住跳跃键

    [Header("移动设置")]
    public float moveSpeed = 6f;
    public float jumpHeight = 2.2f;
    public float gravity = -25f;
    public float climbSpeed = 4f;

    [Header("检测设置")]
    public LayerMask groundMask;
    public LayerMask climbableMask;
    public float groundRayLength = 0.3f;
    public float climbRayLength = 0.8f;

    [Header("摔死检测")]
    public float deathDistance = 5.0f;
    private float airStartY;
    private bool wasGrounded;

    private Vector3 facingDirection = Vector3.right;

    // --- 新系统生命周期 ---
    void Awake() => controls = new PlayerControls();
    void OnEnable() => controls.Player.Enable();
    void OnDisable() => controls.Player.Disable();

    void Start() => controller = GetComponent<CharacterController>();

    void Update()
    {
        // 1. 读取输入数值
        moveInput = controls.Player.Move.ReadValue<Vector2>();
        jumpHeld = controls.Player.Jump.IsPressed();

        // 2. 环境检测
        Vector3 footPos = transform.position + Vector3.up * 0.1f;
        isGrounded = Physics.Raycast(footPos, Vector3.down, groundRayLength, groundMask);

        Vector3 climbOrigin = transform.position + Vector3.up * -0.8f; 
        bool canClimb = Physics.Raycast(climbOrigin, facingDirection, climbRayLength, climbableMask);

        // 3. 状态切换：靠近墙且 (按W/S 或 按住空格)
        if (canClimb && (Mathf.Abs(moveInput.y) > 0.1f || jumpHeld)) 
        {
            isClimbing = true;
        }

        if (isClimbing)
            HandleClimbing(canClimb);
        else
            HandleRegularMovement();

        CheckFallDeath();
        wasGrounded = isGrounded;
    }

    void HandleRegularMovement()
    {
        // 水平移动
        Vector3 move = new Vector3(moveInput.x, 0, 0);
        controller.Move(move * moveSpeed * Time.deltaTime);

        // 转向逻辑
        if (moveInput.x > 0.01f) { facingDirection = Vector3.right; transform.forward = Vector3.right; }
        else if (moveInput.x < -0.01f) { facingDirection = Vector3.left; transform.forward = Vector3.left; }

        if (isGrounded && playerVelocity.y < 0) playerVelocity.y = -2f;

        // 跳跃逻辑 (由 ActionTrigger 触发)
        if (controls.Player.Jump.WasPressedThisFrame() && isGrounded)
        {
            playerVelocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        // --- 变长跳跃 (新系统版) ---
        // 如果玩家松开了跳跃键，且角色正在上升
        if (controls.Player.Jump.WasReleasedThisFrame() && playerVelocity.y > 0)
        {
            playerVelocity.y *= 0.5f;
        }

        playerVelocity.y += gravity * Time.deltaTime;
        controller.Move(playerVelocity * Time.deltaTime);
    }

    void HandleClimbing(bool canClimb)
    {
        // 合并 W 和 空格的向上动力
        float verticalForce = moveInput.y;
        if (jumpHeld) verticalForce = 1f;

        playerVelocity.y = verticalForce * climbSpeed;
        controller.Move(new Vector3(0, playerVelocity.y, 0) * Time.deltaTime);

        // 跳离判定：在墙上按【左右】+【空格】
        bool jumpOff = controls.Player.Jump.WasPressedThisFrame() && Mathf.Abs(moveInput.x) > 0.1f;

        if (!canClimb || jumpOff || (isGrounded && moveInput.y < -0.1f))
        {
            isClimbing = false;
            if (jumpOff) playerVelocity.y = Mathf.Sqrt(jumpHeight * -1.2f * gravity);
        }
    }

    void CheckFallDeath()
    {
        if (wasGrounded && !isGrounded) airStartY = transform.position.y;
        if (!wasGrounded && isGrounded)
        {
            float fallHeight = airStartY - transform.position.y;
            if (fallHeight > deathDistance) Die();
        }
    }

    void Die() => Debug.Log("<color=red>摔死了！</color>");

    private void OnDrawGizmos()
    {
        Gizmos.color = isGrounded ? Color.green : Color.red;
        Gizmos.DrawRay(transform.position + Vector3.up * 0.1f, Vector3.down * groundRayLength);
        Gizmos.color = Color.blue;
        Vector3 climbOrigin = transform.position + Vector3.up * -0.8f;
        Gizmos.DrawRay(climbOrigin, (Application.isPlaying ? facingDirection : transform.right) * climbRayLength);
    }
}