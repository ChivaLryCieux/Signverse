using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class UImanager : MonoBehaviour
{
    public GameObject UI_Camera;
    public bool enableUI = false;

    // Start is called before the first frame update
    void Awake()
    {
        UI_Camera.SetActive(enableUI);
    }

    // Update is called once per frame
    void Update()
    {
        if(Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            enableUI =!enableUI;
            UI_Camera.SetActive(enableUI);
        }
    }
}
