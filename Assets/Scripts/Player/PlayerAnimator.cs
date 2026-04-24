using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAnimatorController : MonoBehaviour
{
    private Animator animator;
    private PlayerCC controller;

    private Vector2 moveInput;

    private PlayerControls controls;

    private bool isRunning;
    private bool isJumping;
    private bool hasRun;
    private bool hasJump;
    private bool hasClimb;
    private bool hasClimbVel;
    private bool hasClimbInput;
    private bool hasClimbExitUp;
    private bool hasClimbExitDown;

    public float jumpTime = 0.3f;

    void Awake()
    {
        controls = new PlayerControls();

        animator = GetComponent<Animator>();
        controller = GetComponentInParent<PlayerCC>();
        CacheAnimatorParameters();
    }

    void OnEnable()
    {
        controls.Enable();


    }

    void OnDisable()
    {
        controls.Disable();

  
    }

    void Update()
    {
        // 获取移动输入
        moveInput = controls.Player.Move.ReadValue<Vector2>();

        // 判断是否移动
        isRunning =
            Mathf.Abs(moveInput.x) > 0.1f ||
            Mathf.Abs(moveInput.y) > 0.1f;

        if (controller != null && controller.isClimbing)
        {
            isRunning = false;
        }

        // 设置 Run Bool
        SetBoolIfExists(hasRun, "Run", isRunning);
        UpdateClimbAnimator();

        if ((controller == null || !controller.isClimbing) && controls.Player.Jump.WasPressedThisFrame())
        {
            Jump();
        }
    }

    void Jump()
    {
        // 设置 Jump Bool
        SetBoolIfExists(hasJump, "Jump", true);

        // 可选：延迟恢复（适合简单跳跃动画）
        StartCoroutine(ResetJump());
    }

    IEnumerator ResetJump()
    {
        yield return new WaitForSeconds(jumpTime);

        SetBoolIfExists(hasJump, "Jump", false);
    }

    private void UpdateClimbAnimator()
    {
        if (controller == null)
        {
            return;
        }

        float climbInput = controller.ClimbInput;
        bool climbing = controller.isClimbing;

        SetBoolIfExists(hasClimb, "Climb", climbing);
        SetFloatIfExists(hasClimbVel, "ClimbVel", climbInput);
        SetFloatIfExists(hasClimbInput, "ClimbInput", climbInput);

        if (controller.ConsumeClimbExitUpAnimationRequest())
        {
            SetTriggerIfExists(hasClimbExitUp, "Climb_Exit_Up");
        }

        if (controller.ConsumeClimbExitDownAnimationRequest())
        {
            SetTriggerIfExists(hasClimbExitDown, "Climb_Exit_Down");
        }
    }

    private void CacheAnimatorParameters()
    {
        if (animator == null)
        {
            return;
        }

        AnimatorControllerParameter[] parameters = animator.parameters;
        for (int i = 0; i < parameters.Length; i++)
        {
            string parameterName = parameters[i].name;

            if (parameterName == "Run") hasRun = true;
            else if (parameterName == "Jump") hasJump = true;
            else if (parameterName == "Climb") hasClimb = true;
            else if (parameterName == "ClimbVel") hasClimbVel = true;
            else if (parameterName == "ClimbInput") hasClimbInput = true;
            else if (parameterName == "Climb_Exit_Up") hasClimbExitUp = true;
            else if (parameterName == "Climb_Exit_Down") hasClimbExitDown = true;
        }
    }

    private void SetBoolIfExists(bool exists, string parameterName, bool value)
    {
        if (exists)
        {
            animator.SetBool(parameterName, value);
        }
    }

    private void SetFloatIfExists(bool exists, string parameterName, float value)
    {
        if (exists)
        {
            animator.SetFloat(parameterName, value, 0.1f, Time.deltaTime);
        }
    }

    private void SetTriggerIfExists(bool exists, string parameterName)
    {
        if (exists)
        {
            animator.SetTrigger(parameterName);
        }
    }
}
