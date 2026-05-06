using System.Collections;
using System.Collections.Generic;
using Skills;
using UnityEngine;
using UnityEngine.InputSystem;

public class TestClimb : MonoBehaviour
{
    public float enableInputWaitingTime = 1f;
    public PlayerCC playerCC;
    public Animator animator;
    public Vector2 exitUpOffset = new Vector2(0.6f, 1f);
    public float exitUpDuration = 3.1f;
    private bool placingPlayerAfterExitUp;
    private float exitUpTimer;
    private Vector3 exitUpTopPosition;



    PlayerControls inputActions;
    [Header("手部匹配目标点（在Trigger内手动摆放）")]
    public Transform handsTarget;   // 👈 核心

    [Header("调试可视化")]
    public bool showGizmo = true;
    public float gizmoSize = 0.1f;

    private void OnDrawGizmos()
    {
        if (!showGizmo || handsTarget == null) return;

        // 画一个球
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(handsTarget.position, gizmoSize);

        // 画方向（可选）
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(handsTarget.position,
                        handsTarget.position + handsTarget.forward * 0.3f);
    }



    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") )
        {
            if (playerCC == null)
            {
                playerCC = other.GetComponentInParent<PlayerCC>();
            }

            if (animator == null && playerCC != null)
            {
                animator = playerCC.GetComponentInChildren<Animator>();
            }

            if (playerCC != null)
            {
                playerCC.SetInputEnabled(false);
                Vector2 moveOffset = exitUpOffset;
                float moveDuration = exitUpDuration;
                if (TryGetClimbSkill(playerCC, out Skill12MJClimb climbSkill))
                {
                    moveOffset = climbSkill.exitUpOffset;
                    moveDuration = climbSkill.exitUpDuration;
                }

                ClimbTransitionTrigger climbTrigger = GetComponent<ClimbTransitionTrigger>();
                exitUpTopPosition = climbTrigger != null
                    ? climbTrigger.GetExitTopPosition(playerCC, moveOffset.x)
                    : playerCC.transform.position + new Vector3(playerCC.GetFacing().x * moveOffset.x, moveOffset.y, 0f);
                exitUpTimer = Mathf.Max(0.01f, moveDuration);
                placingPlayerAfterExitUp = true;
            }
            
            if (animator != null)
            {
                animator.SetBool("Climb_Exit_Up" , true);
            }
            
            StartCoroutine(InputControlRoutine());
       
            
        }

        IEnumerator InputControlRoutine()
        {
            playerCC.DisableInput();
            yield return new WaitForSeconds(enableInputWaitingTime);
            playerCC.EnableInput();
        }
    }
    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (playerCC != null)
            {
                playerCC.SetInputEnabled(true);
            }

            if (animator != null)
            {
                animator.SetBool("Climb_Exit_Up" , false);
                animator.SetBool("Climb_Exit_Down" , false);
            }
        }
    }

    public void MatchTarget()
    {

        if (animator != null && handsTarget != null)
        {
            animator.MatchTarget(handsTarget.position , handsTarget.rotation , AvatarTarget.LeftHand , new MatchTargetWeightMask(Vector3.one , 0f), 0f , 0.3f);
        }
    }

    private void Update()
    {
        if (!placingPlayerAfterExitUp || playerCC == null)
        {
            return;
        }

        playerCC.SetVerticalVelocity(0f);
        playerCC.SetClimbState(true, 0f);
        exitUpTimer -= Time.deltaTime;

        if (exitUpTimer > 0f)
        {
            return;
        }

        playerCC.PlaceCapsuleBottomAt(exitUpTopPosition);
        playerCC.RequestGravitySuppressed();
        playerCC.SetClimbState(false, 0f);
        playerCC.SetInputEnabled(true);
        placingPlayerAfterExitUp = false;
    }

    private bool TryGetClimbSkill(PlayerCC controller, out Skill12MJClimb climbSkill)
    {
        climbSkill = null;

        if (controller == null)
        {
            return false;
        }

        if (controller.equippedSkills != null)
        {
            for (int i = 0; i < controller.equippedSkills.Count; i++)
            {
                if (controller.equippedSkills[i] is Skill12MJClimb equippedClimbSkill)
                {
                    climbSkill = equippedClimbSkill;
                    return true;
                }
            }
        }

        if (controller.unlockedSkills != null)
        {
            for (int i = 0; i < controller.unlockedSkills.Count; i++)
            {
                if (controller.unlockedSkills[i] is Skill12MJClimb unlockedClimbSkill)
                {
                    climbSkill = unlockedClimbSkill;
                    return true;
                }
            }
        }

        return false;
    }

}
