using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class TestClimb : MonoBehaviour
{
    public Animator animator;
    PlayerControls inputActions;
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") )
        {
            // animator.applyRootMotion = true;
            animator.SetTrigger("Climb_Exit_Up11");
        }
    }
}
