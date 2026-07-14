using UnityEngine;

public class PlayAnimOnKey : MonoBehaviour
{
    [Header("Animator 控制设置")]
    public Animator animator;          // 用来接收物体的 Animator 组件
    public string triggerName = "Play"; // 必须和 Animator 的 Parameters 名字**完全一致**
    public KeyCode triggerKey = KeyCode.X; // 你想按哪个键触发

    private bool isAnimPlaying = false; // 控制锁：防止在动画播放时按多次

    void Update()
    {
        // 检测：按键被按下，且当前没有正在播放动画（避免连按导致冲突）
        if (Input.GetKeyDown(triggerKey) && !isAnimPlaying)
        {
            // 向 Animator 发送名为 triggerName 的 Trigger 指令
            animator.SetTrigger(triggerName);
            
            // 锁住，不允许再次触发
            isAnimPlaying = true;
        }
    }

    // 【核心关键】这个函数会被动画文件(.anim)在播放结束的那一瞬间自动调用
    // 这样就能精准把锁解开，允许你下一次按键
    public void OnAnimationFinished()
    {
        isAnimPlaying = false;
    }
}