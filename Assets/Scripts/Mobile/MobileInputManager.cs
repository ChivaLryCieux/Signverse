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
    public static bool JumpPressed { get; internal set; }
    public static bool JumpHeld { get; private set; }
    public static bool JumpReleased { get; internal set; }
    private static bool prevJumpHeld;
    private static bool jumpPressedLatch;
    private static bool jumpReleasedLatch;

    // ── 单帧触发 ──
    public static bool DashPressed { get; internal set; }
    public static bool HidePressed { get; internal set; }
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
        // 同帧 down+up 的快速点击保底：如果 Update 期间重置了但同帧事件又触发了 latch
        if (jumpPressedLatch)
        {
            JumpPressed = true;
            jumpPressedLatch = false;
        }

        if (jumpReleasedLatch)
        {
            JumpReleased = true;
            jumpReleasedLatch = false;
        }
    }

    // ── 摇杆调用 ──

    public static void SetMoveInput(Vector2 input)
    {
        MoveInput = Vector2.ClampMagnitude(input, 1f);
    }

    // ── 跳跃按钮调用 ──

    public static void SetJumpHeld(bool held)
    {
        if (held && !JumpHeld)
        {
            // 上升沿：立即设置 pressed 标志
            JumpPressed = true;
            jumpPressedLatch = true; // 同帧 down+up 的保底
        }
        else if (!held && JumpHeld)
        {
            // 下降沿：立即设置 released 标志
            JumpReleased = true;
            jumpReleasedLatch = true;
        }

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
