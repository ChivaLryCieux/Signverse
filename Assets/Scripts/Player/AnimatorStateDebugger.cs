using UnityEngine;
using UnityEngine.InputSystem;

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

    [Header("用于快速切换攀爬状态")]
    public bool enterClimbing = false;

    
    [Header("Jump Settings")]
    public float jumpChangeRate = 5f;

    float jumpHold = 0f;
    bool jumpRising = false;
    bool jumpFalling = false;

    // [Header("动画补丁引用部分")]
    // public CharacterController cc;
    // public Transform player;
    // public Transform targetPos;

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
        //简单的攀爬Posture控制
        if (Keyboard.current.cKey.wasPressedThisFrame)
        {
            enterClimbing = !enterClimbing;
        }


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
    if (enterClimbing)
    {
        locomotion = Locomotion.isClimbing;
        return;
    }
    if (!enterClimbing)
    {
         locomotion = Locomotion.isGrounded;
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
        float climbVelTarget = 0f;

        if (Mathf.Abs(move.y) > 0.01f)
        {
            animator.SetBool("Climb", true);

            if (move.y > 0.01f)
            {
                climbVelTarget = 1f;   // 向上
            }
            else if (move.y < -0.01f)
            {
                climbVelTarget = -1f;   // 向下/停止
            }
        }
        else
        {
            climbVelTarget = 0f;
        }

        // 平滑实现+阈值吸附
        
        animator.SetFloat("ClimbInput", climbVelTarget , 0.2f , Time.deltaTime * 6f);

        float climbVelCurrent = animator.GetFloat("ClimbInput");
        

        // ⭐关键：硬归零

        if (Mathf.Abs(climbVelCurrent - climbVelTarget) < 0.1f)
        {
            animator.SetFloat("ClimbInput", climbVelTarget);
           
        }
        
    }

    else
    {
        animator.SetBool("Climb", false);

        // 不在攀爬状态时归零
        animator.SetFloat("ClimbInput", 0f, 0.2f, Time.deltaTime);
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

    //用来暂时实现悬崖边上翻越的功能event
    public void LockRootMotion()
    {
        animator.applyRootMotion = false;
        Debug.Log("RM Locked!");
    }
    public void ApplyRootMotion()
    {
        animator.applyRootMotion = true;
        Debug.Log("RM Applaied!");
    }



    // public void OnVaultEndSnap()
    // {
        
        
    //     // 2. 临时关闭 CharacterController 防止碰撞干扰
    //     cc.enabled = false;

    //     // 3. 直接移动角色根节点
    //     player.transform.position = targetPos.position;

    //     // 4. 重新启用 CharacterController
    //     cc.enabled = true;

    //     Debug.Log("Vault End Snap Executed");
    // }
}