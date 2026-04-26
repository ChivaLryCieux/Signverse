using UnityEngine;

namespace Skills
{
    [CreateAssetMenu(fileName = "22-jj", menuName = "Game/Skills/22 JJ Jump")]
    public class Skill22JJZAxisJump : SkillBase
    {
        // ===== 元数据 =====

        // ===== 物理控制 =====
        [Header("基础跳跃")]
        public float jumpHeight = 3f;
        [Tooltip("起跳瞬间横向输入超过该值时，JumpType 记为跑动跳。0=静立跳，1=跑动跳。")]
        public float runJumpInputThreshold = 0.1f;

        // 在接地时执行普通跳跃，并记录跳跃类型给动画使用。
        public override void OnActivate(GameObject user, PlayerCC controller, PlayerCC.Posture posture)
        {
            if (posture != PlayerCC.Posture.Grounded)
            {
                return;
            }

            float horizontalInput = Mathf.Abs(controller.GetMoveInput().x);
            controller.SetJumpType(horizontalInput > runJumpInputThreshold ? 1 : 0);

            float verticalVel = Mathf.Sqrt(jumpHeight * -2f * controller.gravity);
            controller.SetVerticalVelocity(verticalVel);
        }

        // 每帧监听跳跃按下事件，满足条件时触发 OnActivate。
        public override void OnUpdate(GameObject user, PlayerCC controller, PlayerCC.Posture posture)
        {
            if (controller.WasJumpPressed())
            {
                OnActivate(user, controller, posture);
            }
        }

        // ===== 动画控制 =====
    }
}
