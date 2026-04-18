using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class UImanager : MonoBehaviour
{
    public GameObject UI_Camera;
    public bool enableUI = false;
    private SkillPauseUIController skillPauseUIController;
    
    void Awake()
    {
        skillPauseUIController = GetComponent<SkillPauseUIController>();
        if (skillPauseUIController == null && UI_Camera != null)
        {
            UI_Camera.SetActive(enableUI);
        }
    }
    
    void Update()
    {
        if (skillPauseUIController != null)
        {
            return;
        }

        if(Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            enableUI =!enableUI;
            if (UI_Camera != null)
            {
                UI_Camera.SetActive(enableUI);
            }
        }
    }
}
