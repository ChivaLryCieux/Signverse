using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class Cheating : MonoBehaviour
{
    public GameObject cheat;
    // Start is called before the first frame update
    void Start()
    {
        gameObject.SetActive(true);
    }

    // Update is called once per frame
    void Update()
    {
        if(Keyboard.current.yKey.wasPressedThisFrame)     cheat.gameObject.SetActive(true);
        if(Keyboard.current.tKey.wasPressedThisFrame)     SceneManager.LoadScene(0);
        if(Keyboard.current.gKey.wasPressedThisFrame)     SceneManager.LoadScene(1);
        if(Keyboard.current.vKey.wasPressedThisFrame)     SceneManager.LoadScene(2);
    }
}
