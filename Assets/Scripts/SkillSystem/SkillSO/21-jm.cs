using UnityEngine;

namespace Skills
{
    [CreateAssetMenu(fileName = "21-jm", menuName = "Game/Skills/21 JM Standing Long Jump")]
    public class Skill21JMStandingLongJump : SkillBase
    {
        // ===== 元数据 =====

        // ===== 物理控制 =====
        [Header("立定跳远")]
        public float jumpHeight = 2.5f;
        public float forwardDistance = 6f;

        private float airForwardSpeed;
        private Vector3 moveDirection = Vector3.right;

        public override void OnActivate(GameObject user, PlayerCC controller, PlayerCC.Posture posture)
        {
            float verticalVel = Mathf.Sqrt(jumpHeight * -2f * controller.gravity);
            controller.SetVerticalVelocity(verticalVel);
            moveDirection = controller.GetFacing().sqrMagnitude > 0.01f ? controller.GetFacing() : Vector3.right;
            airForwardSpeed = forwardDistance;
        }

        public override void OnUpdate(GameObject user, PlayerCC controller, PlayerCC.Posture posture)
        {
            if (posture == PlayerCC.Posture.Grounded)
            {
                airForwardSpeed = 0f;
                return;
            }

            if (airForwardSpeed <= 0.01f) return;

            controller.GetCharacterController().Move(moveDirection * airForwardSpeed * Time.deltaTime);
            airForwardSpeed = Mathf.MoveTowards(airForwardSpeed, 0f, forwardDistance * Time.deltaTime);
        }

        // ===== 动画控制 =====
    }
}
