using UnityEngine;
using Cinemachine;

public class ButtonMoveCamera : MonoBehaviour
{
    [SerializeField] private CinemachineVirtualCamera virtualCamera1;
    [SerializeField] private CinemachineVirtualCamera virtualCamera2;

    public GameObject upButton;
    public GameObject downButton;
    public GameObject newGameButton;
    public GameObject continueButton;

    void Start()
    {
        // 初始化时，设置虚拟摄像机的优先级
        virtualCamera1.Priority = 10;
        virtualCamera2.Priority = 5;
        // 初始化按钮状态，后期需要改成调整Interactable
        upButton.SetActive(true);
        downButton.SetActive(false);
        newGameButton.SetActive(false);
        continueButton.SetActive(false);
    }
    public void Update()
    {

    }

    public void SwitchToCamera2()
    {
        virtualCamera2.Priority = 15;
        upButton.SetActive(false);
        downButton.SetActive(true);
        newGameButton.SetActive(true);
        continueButton.SetActive(true);
    }
    public void SwitchToCamera1()
    {
        virtualCamera2.Priority = 5;
        downButton.SetActive(false);
        upButton.SetActive(true);
        newGameButton.SetActive(false);
        continueButton.SetActive(false);
    }
}