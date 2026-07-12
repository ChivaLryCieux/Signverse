using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class UI_2d_Test : MonoBehaviour
{
    public Animator animator;
    private bool animationTriggered = false;
    // Start is called before the first frame update
    void Start()
    {
        animator.SetBool("start", animationTriggered);
        animator.SetBool("ready", animationTriggered);
    }

    // Update is called once per frame
    void Update()
    {
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            animationTriggered = !animationTriggered;
            animator.SetBool("start", animationTriggered);
            animator.SetBool("ready", !animationTriggered);
        }
        if(Keyboard.current.sKey.wasPressedThisFrame)
        {
            animator.SetTrigger("startFly");
        }
        if(Keyboard.current.dKey.wasPressedThisFrame)
        {
            animator.SetTrigger("startExplode");
        }
    }
}
