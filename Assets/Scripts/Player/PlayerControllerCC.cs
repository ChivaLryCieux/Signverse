using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerControllerCC : MonoBehaviour
{
    private CharacterController controller;
    private Vector3 playerVelocity;
    
    [Header("Movement")]
    public float moveSpeed = 7f;
    public float gravity = -25f; // 自定义重力常数
    public float jumpHeight = 2.5f;

    [Header("Detection")]
    public float climbSpeed = 5f;
    public LayerMask climbableMask;
    public float climbCheckDistance = 0.6f;

    [Header("Fall Damage")]
    public float deathThreshold = 8f; // 摔死高度
    private float highestPointDuringFall;
    private bool wasGroundedLastFrame;

    private bool isClimbing;

    void Start() {
        controller = GetComponent<CharacterController>();
    }

    void Update() {
        HandleMovement();
        HandleClimbing();
        CheckFallDeath();
    }

    void HandleMovement() {
        // 1. 基础水平移动 (锁定Z轴)
        float move = Input.GetAxis("Horizontal");
        Vector3 moveDirection = new Vector3(move, 0, 0);
        
        if (!isClimbing) {
            controller.Move(moveDirection * moveSpeed * Time.deltaTime);

            // 2. 地面检测与重力
            if (controller.isGrounded && playerVelocity.y < 0) {
                playerVelocity.y = -2f; // 保持微小的向下力以贴合地面
            }

            // 3. 大小跳实现 (Variable Jump Height)
            // 按下瞬间跳跃
            if (Input.GetButtonDown("Jump") && controller.isGrounded) {
                playerVelocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }
            // 核心：如果玩家松开跳跃键，且还在上升，则快速减小上升速度（实现小跳）
            if (Input.GetButtonUp("Jump") && playerVelocity.y > 0) {
                playerVelocity.y *= 0.5f; 
            }

            playerVelocity.y += gravity * Time.deltaTime;
            controller.Move(playerVelocity * Time.deltaTime);
        }
    }

    void HandleClimbing() {
        // 向前发射射线检测爬梯子
        RaycastHit hit;
        bool canClimb = Physics.Raycast(transform.position, transform.forward, out hit, climbCheckDistance, climbableMask);

        float vInput = Input.GetAxis("Vertical");

        if (canClimb && Mathf.Abs(vInput) > 0.1f) {
            isClimbing = true;
        }

        if (isClimbing) {
            playerVelocity.y = 0; // 攀爬时无视重力
            Vector3 climbDir = new Vector3(0, vInput * climbSpeed, 0);
            controller.Move(climbDir * Time.deltaTime);

            // 离开爬梯逻辑：按跳跃键跳开，或离开射线范围
            if (Input.GetButtonDown("Jump") || !canClimb) {
                isClimbing = false;
            }
        }
    }

    void CheckFallDeath() {
        // 记录离开地面瞬间的高度
        if (wasGroundedLastFrame && !controller.isGrounded) {
            highestPointDuringFall = transform.position.y;
        }

        // 落地瞬间计算高度差
        if (!wasGroundedLastFrame && controller.isGrounded) {
            float fallDistance = highestPointDuringFall - transform.position.y;
            if (fallDistance > deathThreshold) {
                Die();
            }
        }
        wasGroundedLastFrame = controller.isGrounded;
    }

    void Die() {
        Debug.Log("摔死了！");
        // 这里执行重生逻辑
    }
}