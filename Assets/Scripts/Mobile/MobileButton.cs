using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 虚拟按钮。挂载在按钮 Image 上，通过 EventTrigger 实现按下/抬起。
/// 支持单次触发（Dash/Hide/Interact/Pause）和持续按住（Jump）。
/// </summary>
[RequireComponent(typeof(Image))]
public class MobileButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public enum ButtonType
    {
        Jump,
        Dash,
        Hide,
        Interact,
        Pause
    }

    [Header("按钮类型")]
    [SerializeField] private ButtonType buttonType;

    [Header("按下效果")]
    [SerializeField] private float pressedAlpha = 0.6f;
    [SerializeField] private float normalAlpha = 1f;

    private Image buttonImage;

    private void Awake()
    {
        buttonImage = GetComponent<Image>();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        SetPressedVisual(true);

        switch (buttonType)
        {
            case ButtonType.Jump:
                MobileInputManager.SetJumpHeld(true);
                break;
            case ButtonType.Dash:
                MobileInputManager.NotifyDashPressed();
                break;
            case ButtonType.Hide:
                MobileInputManager.NotifyHidePressed();
                break;
            case ButtonType.Interact:
                MobileInputManager.NotifyInteractPressed();
                break;
            case ButtonType.Pause:
                MobileInputManager.NotifyPausePressed();
                break;
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        SetPressedVisual(false);

        if (buttonType == ButtonType.Jump)
        {
            MobileInputManager.SetJumpHeld(false);
        }
    }

    private void SetPressedVisual(bool pressed)
    {
        if (buttonImage != null)
        {
            Color c = buttonImage.color;
            c.a = pressed ? pressedAlpha : normalAlpha;
            buttonImage.color = c;
        }
    }
}
