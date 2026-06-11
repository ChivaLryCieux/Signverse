using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 虚拟摇杆。挂载在外圈 Image 上，内圈手柄作为子物体。
/// 通过 EventTrigger 实现拖拽，输出 Vector2 到 MobileInputManager。
/// </summary>
[RequireComponent(typeof(Image))]
public class MobileJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [Header("摇杆设置")]
    [SerializeField] private RectTransform handle;
    [SerializeField, Range(0f, 1f)] private float deadZone = 0.1f;

    private RectTransform rectTransform;
    private Canvas parentCanvas;
    private Camera eventCamera;
    private Vector2 center;
    private float radius;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        parentCanvas = GetComponentInParent<Canvas>();
        eventCamera = parentCanvas != null && parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? parentCanvas.worldCamera : null;

        // 初始隐藏手柄在中心
        if (handle != null)
        {
            handle.anchoredPosition = Vector2.zero;
        }
    }

    private void Start()
    {
        // 计算摇杆半径（外圈宽度的一半）
        radius = rectTransform.rect.width * 0.5f;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        // 计算外圈中心在屏幕空间的位置
        Vector2 screenCenter = RectTransformUtility.WorldToScreenPoint(eventCamera, rectTransform.position);
        center = screenCenter;

        OnDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (handle == null)
        {
            return;
        }

        Vector2 direction = eventData.position - center;
        float magnitude = direction.magnitude;

        // clamp 到半径
        if (magnitude > radius)
        {
            direction = direction.normalized * radius;
            magnitude = radius;
        }

        // 更新手柄位置（局部坐标）
        handle.anchoredPosition = direction;

        // 归一化输出（考虑死区）
        float normalizedMagnitude = magnitude / radius;
        Vector2 output = normalizedMagnitude > deadZone
            ? direction.normalized * ((normalizedMagnitude - deadZone) / (1f - deadZone))
            : Vector2.zero;

        MobileInputManager.SetMoveInput(output);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (handle != null)
        {
            handle.anchoredPosition = Vector2.zero;
        }

        MobileInputManager.SetMoveInput(Vector2.zero);
    }
}
