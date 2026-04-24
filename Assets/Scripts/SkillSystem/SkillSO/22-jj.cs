using UnityEngine;

namespace Skills
{
    [CreateAssetMenu(fileName = "22-jj", menuName = "Game/Skills/22 JJ Jump")]
    public class Skill22JJZAxisJump : SkillBase
    {
        [Header("基础跳跃")]
        public float jumpHeight = 3f;
        [Tooltip("起跳瞬间横向输入超过该值时，JumpType 记为跑动跳。0=静立跳，1=跑动跳。")]
        public float runJumpInputThreshold = 0.1f;

        public override void OnActivate(GameObject user, PlayerCC controller)
        {
            if (!controller.isGrounded || controller.isClimbing)
            {
                return;
            }

            float horizontalInput = Mathf.Abs(controller.GetMoveInput().x);
            controller.SetJumpType(horizontalInput > runJumpInputThreshold ? 1 : 0);

            float verticalVel = Mathf.Sqrt(jumpHeight * -2f * controller.gravity);
            controller.SetVerticalVelocity(verticalVel);
        }

        public override void OnUpdate(GameObject user, PlayerCC controller)
        {
            if (controller.WasJumpPressed())
            {
                OnActivate(user, controller);
            }
        }
    }
}
