using UnityEngine;

/// <summary>
/// 移动端虚拟输入管理器。单例，仅在移动端激活。
/// 虚拟摇杆和按钮通过此类桥接到现有输入系统。
/// </summary>
public class MobileInputManager : MonoBehaviour
{
    public static MobileInputManager Instance { get; private set; }

    // ── 摇杆输出 ──
    public static Vector2 MoveInput { get; private set; }

    // ── 跳跃状态机（支持 tap / hold / release）──
    public static bool JumpPressed { get; private set; }
    public static bool JumpHeld { get; private set; }
    public static bool JumpReleased { get; private set; }
    private static bool prevJumpHeld;

    // ── 单帧触发 ──
    public static bool DashPressed { get; private set; }
    public static bool HidePressed { get; private set; }
    public static bool HideHeld { get; private set; }
    public static bool InteractPressed { get; private set; }
    public static bool PausePressed { get; private set; }

    // ── 交互按钮显隐（引用计数）──
    public static bool InteractButtonVisible => interactRefCount > 0;
    private static int interactRefCount;

    // ── 是否为移动端 ──
    public static bool IsMobilePlatform =>
#if UNITY_ANDROID || UNITY_IOS
        true;
#else
        false;
#endif

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // 非移动端自动禁用
        if (!IsMobilePlatform)
        {
            gameObject.SetActive(false);
        }
    }

    private void LateUpdate()
    {
        // 重置单帧触发标志
        JumpPressed = false;
        JumpReleased = false;
        DashPressed = false;
        HidePressed = false;
        InteractPressed = false;
        PausePressed = false;

        // 跳跃边缘检测
        JumpPressed = JumpHeld && !prevJumpHeld;
        JumpReleased = !JumpHeld && prevJumpHeld;
        prevJumpHeld = JumpHeld;
    }

    // ── 摇杆调用 ──

    public static void SetMoveInput(Vector2 input)
    {
        MoveInput = Vector2.ClampMagnitude(input, 1f);
    }

    // ── 跳跃按钮调用 ──

    public static void SetJumpHeld(bool held)
    {
        JumpHeld = held;
    }

    // ── 单帧触发按钮调用 ──

    public static void NotifyDashPressed()
    {
        DashPressed = true;
    }

    public static void NotifyHidePressed()
    {
        HidePressed = true;
    }

    public static void NotifyInteractPressed()
    {
        InteractPressed = true;
    }

    public static void NotifyPausePressed()
    {
        PausePressed = true;
    }

    // ── 交互按钮显隐管理 ──

    public static void PushInteractVisible()
    {
        interactRefCount++;
    }

    public static void PopInteractVisible()
    {
        interactRefCount = Mathf.Max(0, interactRefCount - 1);
    }

    /// <summary>
    /// 消费交互按压（只读取一次即重置）。
    /// </summary>
    public static bool ConsumeInteractPressed()
    {
        if (InteractPressed)
        {
            InteractPressed = false;
            return true;
        }

        return false;
    }

    /// <summary>
    /// 消费暂停按压（只读取一次即重置）。
    /// </summary>
    public static bool ConsumePausePressed()
    {
        if (PausePressed)
        {
            PausePressed = false;
            return true;
        }

        return false;
    }
}
