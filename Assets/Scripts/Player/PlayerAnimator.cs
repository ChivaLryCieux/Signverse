using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAnimatorController : MonoBehaviour
{
    private Animator animator;

    private Vector2 moveInput;

    private PlayerControls controls;

    private bool isRunning;
    private bool isJumping;

    public float jumpTime = 0.3f;

    void Awake()
    {
        controls = new PlayerControls();

        animator = GetComponent<Animator>();
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

        // 设置 Run Bool
        animator.SetBool("Run", isRunning);

        if (controls.Player.Jump.WasPressedThisFrame())
        {
            Jump();
        }
    }

    void Jump()
    {
        // 设置 Jump Bool
        animator.SetBool("Jump", true);

        // 可选：延迟恢复（适合简单跳跃动画）
        StartCoroutine(ResetJump());
    }

    IEnumerator ResetJump()
    {
        yield return new WaitForSeconds(jumpTime);

        animator.SetBool("Jump", false);
    }
}