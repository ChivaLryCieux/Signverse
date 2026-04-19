using UnityEngine;

namespace Skills
{
    [CreateAssetMenu(fileName = "22-jj", menuName = "Game/Skills/22 JJ Jump")]
    public class Skill22JJZAxisJump : SkillBase
    {
        [Header("基础跳跃")]
        public float jumpHeight = 3f;

        public override void OnActivate(GameObject user, PlayerCC controller)
        {
            if (!controller.isGrounded || controller.isClimbing)
            {
                return;
            }

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
