using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class Cheating : MonoBehaviour
{
    public GameObject cheat;

    void Start()
    {
        gameObject.SetActive(true);
    }

    void Update()
    {
        if (Keyboard.current == null)
        {
            return;
        }

        // W + O + N + B
        if (Keyboard.current.wKey.isPressed &&
            Keyboard.current.oKey.isPressed &&
            Keyboard.current.nKey.isPressed &&
            Keyboard.current.bKey.wasPressedThisFrame)
        {
            cheat.gameObject.SetActive(true);
        }

        // M + E + N + U
        if (Keyboard.current.mKey.isPressed &&
            Keyboard.current.eKey.isPressed &&
            Keyboard.current.nKey.isPressed &&
            Keyboard.current.uKey.wasPressedThisFrame)
        {
            SceneManager.LoadScene(0);
        }

        // D + I + Y
        if (Keyboard.current.dKey.isPressed &&
            Keyboard.current.iKey.isPressed &&
            Keyboard.current.yKey.wasPressedThisFrame)
        {
            SceneManager.LoadScene(1);
        }

        // D + I + E + R
        if (Keyboard.current.dKey.isPressed &&
            Keyboard.current.iKey.isPressed &&
            Keyboard.current.eKey.isPressed &&
            Keyboard.current.rKey.wasPressedThisFrame)
        {
            SceneManager.LoadScene(2);
        }
    }
}