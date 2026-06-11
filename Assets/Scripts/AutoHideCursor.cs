using UnityEngine;
using UnityEngine.InputSystem;

public class AutoHideCursor : MonoBehaviour
{
    [Header("光标设置")]
    public Texture2D cursorTexture;   // Build 设置里自定义的鼠标图片
    public Vector2 hotspot = Vector2.zero;

    [Header("隐藏设置")]
    public float idleTime = 2f;       // 鼠标闲置时间，秒
    private float idleTimer = 0f;

    private Vector3 lastMousePosition;

    void Start()
    {
        // 设置自定义光标
        if (cursorTexture != null)
        {
            Cursor.SetCursor(cursorTexture, hotspot, CursorMode.Auto);
        }

        Cursor.visible = true;
        lastMousePosition = GetMousePosition();
    }

    void Update()
    {
        Vector3 currentPos = GetMousePosition();

        // 鼠标移动检测
        if (currentPos != lastMousePosition)
        {
            idleTimer = 0f;
            if (!Cursor.visible)
                Cursor.visible = true; // 重新显示光标
            lastMousePosition = currentPos;
        }
        else
        {
            idleTimer += Time.deltaTime;
            if (idleTimer >= idleTime)
            {
                Cursor.visible = false; // 闲置超过指定时间隐藏光标
            }
        }
    }

    private static Vector3 GetMousePosition()
    {
        if (Mouse.current != null)
        {
            Vector2 pos = Mouse.current.position.ReadValue();
            return new Vector3(pos.x, pos.y, 0f);
        }

        if (Touchscreen.current != null)
        {
            Vector2 pos = Touchscreen.current.primaryTouch.position.ReadValue();
            return new Vector3(pos.x, pos.y, 0f);
        }

        return Vector3.zero;
    }
}