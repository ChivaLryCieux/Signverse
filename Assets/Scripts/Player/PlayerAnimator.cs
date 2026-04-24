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
    private bool hasClimb;
    private bool hasClimbVel;
    private bool hasClimbInput;
    private bool hasClimbExitUp;
    private bool hasClimbExitDown;
    private bool hasVerticalVelocity;
    private bool hasLegacyJumpVelocity;
    private bool hasJumpType;
    private bool hasIsGrounded;
    private bool hasDashPosture;

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
        UpdateJumpAnimator();
        UpdateDashAnimator();
        UpdateClimbAnimator();
    }

    private void UpdateJumpAnimator()
    {
        if (controller == null)
        {
            return;
        }

        SetFloatImmediateIfExists(hasVerticalVelocity, "VerticalVelocity", controller.VerticalVelocity);
        SetFloatImmediateIfExists(hasLegacyJumpVelocity, "Jump", controller.VerticalVelocity);
        SetIntIfExists(hasJumpType, "JumpType", controller.JumpType);
        SetBoolIfExists(hasIsGrounded, "IsGrounded", controller.isGrounded);
    }

    private void UpdateDashAnimator()
    {
        if (controller == null)
        {
            return;
        }

        SetFloatImmediateIfExists(hasDashPosture, "DashPosture", controller.DashPosture);
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

            AnimatorControllerParameterType parameterType = parameters[i].type;

            if (parameterName == "Run" && parameterType == AnimatorControllerParameterType.Bool) hasRun = true;
            else if (parameterName == "Climb" && parameterType == AnimatorControllerParameterType.Bool) hasClimb = true;
            else if (parameterName == "ClimbVel" && parameterType == AnimatorControllerParameterType.Float) hasClimbVel = true;
            else if (parameterName == "ClimbInput" && parameterType == AnimatorControllerParameterType.Float) hasClimbInput = true;
            else if (parameterName == "Climb_Exit_Up" && parameterType == AnimatorControllerParameterType.Trigger) hasClimbExitUp = true;
            else if (parameterName == "Climb_Exit_Down" && parameterType == AnimatorControllerParameterType.Trigger) hasClimbExitDown = true;
            else if (parameterName == "VerticalVelocity" && parameterType == AnimatorControllerParameterType.Float) hasVerticalVelocity = true;
            else if (parameterName == "Jump" && parameterType == AnimatorControllerParameterType.Float) hasLegacyJumpVelocity = true;
            else if (parameterName == "JumpType" && parameterType == AnimatorControllerParameterType.Int) hasJumpType = true;
            else if (parameterName == "IsGrounded" && parameterType == AnimatorControllerParameterType.Bool) hasIsGrounded = true;
            else if (parameterName == "DashPosture" && parameterType == AnimatorControllerParameterType.Float) hasDashPosture = true;
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

    private void SetFloatImmediateIfExists(bool exists, string parameterName, float value)
    {
        if (exists)
        {
            animator.SetFloat(parameterName, value);
        }
    }

    private void SetIntIfExists(bool exists, string parameterName, int value)
    {
        if (exists)
        {
            animator.SetInteger(parameterName, value);
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
