using UnityEngine;

public class CursorChanger : MonoBehaviour
{
    public Texture2D cursorTexture;

    void Start()
    {
        Cursor.SetCursor(cursorTexture, Vector2.zero, CursorMode.Auto);
    }
}