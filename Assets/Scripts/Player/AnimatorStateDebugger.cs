using UnityEngine;

public class AnimatorStateDebugger : MonoBehaviour
{
    [Header("Animator")]
    public Animator animator;

    [Header("Input System Class")]
    public PlayerControls inputActions; // 你的输入系统生成类

    Vector2 move;
    float jumpAxis;
    bool dashPressed;
    bool hidePressed;
    [Header("环境检测")]
    public float groundDetect = 0.5f;

    [Header("状态检测判断")]
    public bool isGrounded = true;
    public bool isMidAir = false;
    public bool isClimbing = false;

    public enum Locomotion
    {
        isGrounded,
        isMidAir,
        isClimbing
    }

    public Locomotion locomotion;

    [Header("Jump Settings")]
    public float jumpChangeRate = 5f;

    float jumpHold = 0f;
    bool jumpRising = false;
    bool jumpFalling = false;

    void Awake()
    {
        inputActions = new PlayerControls();
    }

    void OnEnable()
    {
        inputActions.Enable();
    }

    void OnDisable()
    {
        inputActions.Disable();
    }

    void Update()
    {
        SwitchLocomotion();
  

        HandleInput();

        HandleAnimator();
    }

    //========================
    // Locomotion选择
    //========================
    void SwitchLocomotion()
{
    // 1️⃣ 默认状态：Ground
    locomotion = Locomotion.isGrounded;

    // 2️⃣ 如果有垂直输入（爬梯等）
    if (Mathf.Abs(move.y) > 0.01f)
    {
        locomotion = Locomotion.isClimbing;
        return;
    }

    // 3️⃣ 向下发射射线检测地面
    RaycastHit hit;

    if (Physics.Raycast(transform.position , Vector3.down, out hit, groundDetect))
    {
        // 检测到地面，且没有垂直输入
        if (Mathf.Abs(move.y) <= 0.01f)
        {
            locomotion = Locomotion.isGrounded;
        }
    }
    else
    {
        // 没检测到地面 → 在空中
        locomotion = Locomotion.isMidAir;
    }
}
  

    //========================
    // 输入获取
    //========================

    void HandleInput()
    {
        move = inputActions.Player.Move.ReadValue<Vector2>();

        jumpAxis = inputActions.Player.Jump.ReadValue<float>();

        dashPressed = inputActions.Player.Dash.triggered;

        hidePressed = inputActions.Player.Hide.triggered;
    }

    //========================
    // Animator处理
    //========================

    void HandleAnimator()
    {
        HandleRun();

        HandleClimb();

        HandleJump();

        HandleDash();

        HandleHide();
    }

    //========================
    // Run
    //========================

    void HandleRun()
    {
        if (locomotion == Locomotion.isGrounded)
        {
            if (Mathf.Abs(move.x) > 0.01f)
            {
                animator.SetBool("Run", true);
            }
            else
            {
                animator.SetBool("Run", false);
            }
        }
        else
        {
            animator.SetBool("Run", false);
        }
    }

    //========================
    // Climb
    //========================

  void HandleClimb()
{
    if (locomotion == Locomotion.isClimbing)
    {
        if (Mathf.Abs(move.y) > 0.01f)
        {
            animator.SetBool("Climb", true);

            if (move.y > 0.01f)
            {
                // 向上爬：ClimbVel → 1
                animator.SetFloat("ClimbVel", 1f, 0.2f, Time.deltaTime);
            }
            else if (move.y < -0.01f)
            {
                // 向下或停止：ClimbVel → 0
                animator.SetFloat("ClimbVel", 0f, 0.2f, Time.deltaTime);
            }
        }
        else
        {
            animator.SetBool("Climb", false);

            // 没输入时缓慢归零
            animator.SetFloat("ClimbVel", 0f);
        }
    }
    else
    {
        animator.SetBool("Climb", false);

        // 不在攀爬状态时归零
        animator.SetFloat("ClimbVel", 0f, 0.2f, Time.deltaTime);
    }
}

    //========================
    // Jump（核心部分）
    //========================

    void HandleJump()
{
    // 按下Jump，开始上升
    if (jumpAxis > 0f  && !jumpRising && !jumpFalling)
    {
        jumpRising = true;
    }

    // 上升阶段：0 → 1
    if (jumpRising)
    {
        jumpHold += jumpChangeRate * Time.deltaTime;

        if (jumpHold >= 1f)
        {
            jumpHold = 1f;

            jumpRising = false;
            jumpFalling = true;
        }
    }

    // 下降阶段：1 → 0
    if (jumpFalling)
    {
        jumpHold -= jumpChangeRate * Time.deltaTime;

        if (jumpHold <= 0f)
        {
            jumpHold = 0f;

            jumpFalling = false;
        }
    }

    // 持续更新 Animator
    animator.SetFloat("Jump", jumpHold);
}

    //========================
    // Dash
    //========================

    void HandleDash()
    {
        if (dashPressed)
        {
            animator.SetBool("Dash", true);
        }
        else
        {
            animator.SetBool("Dash", false);
        }
    }

    //========================
    // Hide
    //========================

    void HandleHide()
    {
        if (hidePressed)
        {
            animator.SetBool("Hide", true);
        }
        else
        {
            animator.SetBool("Hide", false);
        }
    }
}