using UnityEngine;
using Cinemachine;
using System.Collections;
using UnityEngine.UI;

public class ButtonMoveCamera : MonoBehaviour
{
    [SerializeField] private CinemachineVirtualCamera virtualCamera1;
    [SerializeField] private CinemachineVirtualCamera virtualCamera2;

    public GameObject upButton;
    public GameObject downButton;
    public GameObject newGameText;
    public GameObject continueText;
    public GameObject exitText;

    [SerializeField] private TMPTypewriterFade newGameTextFade;
    [SerializeField] private TMPTypewriterFade continueTextFade;
    [SerializeField] private TMPTypewriterFade exitTextFade;

    void Start()
    {
        // 初始化时，设置虚拟摄像机的优先级
        virtualCamera1.Priority = 10;
        virtualCamera2.Priority = 5;

        // 初始化按钮与Text状态，后期需要改成调整Interactable
        upButton.SetActive(true);
        downButton.SetActive(false);
        newGameText.SetActive(false);
        continueText.SetActive(false);
        exitText.SetActive(false);
    }

    public void SwitchToCamera2()
    {
        virtualCamera2.Priority = 15;

        downButton.SetActive(true);
        newGameText.SetActive(true);
        continueText.SetActive(true);
        exitText.SetActive(true);

        upButton.SetActive(false);
    }

    public void SwitchToCamera1()
    {
        virtualCamera2.Priority = 5;

        downButton.SetActive(false);
        upButton.SetActive(true);

        newGameTextFade.FadeOutAndDisable();
        continueTextFade.FadeOutAndDisable();
        exitTextFade.FadeOutAndDisable();
    }
}