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
        [Min(1)] public int maxJumpCount = 3;
        [Tooltip("起跳瞬间横向输入超过该值时，JumpType 记为跑动跳。0=静立跳，1=跑动跳。")]
        public float runJumpInputThreshold = 0.1f;

        private int jumpsUsed;
        private bool wasGroundedLastFrame;

        // 第一段跳必须在地面，之后可在空中继续跳到 maxJumpCount。
        public override void OnActivate(GameObject user, PlayerCC controller, PlayerCC.Posture posture)
        {
            bool isGrounded = posture == PlayerCC.Posture.Grounded;
            if (!isGrounded && (jumpsUsed <= 0 || jumpsUsed >= maxJumpCount))
            {
                return;
            }

            if (isGrounded)
            {
                jumpsUsed = 0;
            }

            jumpsUsed++;

            float horizontalInput = Mathf.Abs(controller.GetMoveInput().x);
            controller.SetJumpType(horizontalInput > runJumpInputThreshold ? 1 : 0);

            float verticalVel = Mathf.Sqrt(jumpHeight * -2f * controller.gravity);
            controller.SetVerticalVelocity(verticalVel);
        }

        // 每帧重置落地状态，并监听跳跃按下事件。
        public override void OnUpdate(GameObject user, PlayerCC controller, PlayerCC.Posture posture)
        {
            bool isGrounded = posture == PlayerCC.Posture.Grounded;
            if (isGrounded && !wasGroundedLastFrame)
            {
                jumpsUsed = 0;
            }

            wasGroundedLastFrame = isGrounded;

            if (controller.WasJumpPressed())
            {
                OnActivate(user, controller, posture);
            }
        }

        // ===== 动画控制 =====
    }
}
