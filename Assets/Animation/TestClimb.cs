using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class TestClimb : MonoBehaviour
{
    public PlayerCC playerCC;
    public Animator animator;



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
            playerCC.SetInputEnabled(false);
            
            animator.SetBool("Climb_Exit_Up" , true);

       
            
        }
    }
    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerCC.SetInputEnabled(true);
            animator.SetBool("Climb_Exit_Up" , false);
            animator.SetBool("Climb_Exit_Down" , false);
        }
    }

    public void MatchTarget()
    {

        animator.MatchTarget(handsTarget.position , handsTarget.rotation , AvatarTarget.LeftHand , new MatchTargetWeightMask(Vector3.one , 0f), 0f , 0.3f);
    }

}
