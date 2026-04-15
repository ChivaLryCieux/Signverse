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

    [Header("Locomotion Select (Inspector Debug)")]
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
    public float jumpHold = 0f;

    public float firstJump = 0.5f;

    public float jumpMaxTime = 0.5f;

    public float jumpReleaseSpeed = 5f;

    float jumpTimer = 0f;

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
        EasyLocomotionSet();
        //HandleLocomotionSelection();

        HandleInput();

        HandleAnimator();
    }

    //========================
    // Locomotion选择
    //========================
    void EasyLocomotionSet()
    {
        if(move.x > 0) locomotion = Locomotion.isGrounded;
        if(move.y > 0) locomotion = Locomotion.isClimbing;
        
    }
    void HandleLocomotionSelection()
    {
        int trueCount = 0;

        if (isGrounded) trueCount++;
        if (isMidAir) trueCount++;
        if (isClimbing) trueCount++;

        // 保证始终只有一个为true
        if (trueCount > 1)
        {
            isGrounded = true;
            isMidAir = false;
            isClimbing = true;
        }

        if (isGrounded)
            locomotion = Locomotion.isGrounded;

        if (isMidAir)
            locomotion = Locomotion.isMidAir;

        if (isClimbing)
            locomotion = Locomotion.isClimbing;
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
        if (locomotion != Locomotion.isGrounded)
        {
            jumpHold = Mathf.Lerp(
                jumpHold,
                0f,
                jumpReleaseSpeed * Time.deltaTime
            );

            animator.SetFloat("Jump", jumpHold);
            return;
        }

        bool jumpHeld = jumpAxis > 0.01f;

        // 按下跳跃
        if (jumpHeld)
        {
            if (jumpTimer == 0f)
            {
                jumpHold = firstJump;
            }

            jumpTimer += Time.deltaTime;

            float t = jumpTimer / jumpMaxTime;

            jumpHold = Mathf.Lerp(
                firstJump,
                1f,
                t
            );

            if (jumpHold >= 1f)
            {
                jumpHold = Mathf.Lerp(
                    jumpHold,
                    0f,
                    jumpReleaseSpeed * Time.deltaTime
                );
            }
        }
        else
        {
            jumpTimer = 0f;

            jumpHold = Mathf.Lerp(
                jumpHold,
                0f,
                jumpReleaseSpeed * Time.deltaTime
            );
        }

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