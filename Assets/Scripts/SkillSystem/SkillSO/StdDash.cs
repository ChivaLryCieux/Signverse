using UnityEngine;

namespace Skills
{
    [CreateAssetMenu(fileName = "StdDash", menuName = "Game/Skills/Standard Dash")]
    public class StdDash : SkillBase
    {
        [Header("冲刺设置")]
        public KeyCode dashKey = KeyCode.L;
        public float dashDistance = 4f;
        public float cooldown = 0.4f;

        private float cooldownTimer;

        public override void OnActivate(GameObject user, PlayerCC controller) { }

        public override void OnUpdate(GameObject user, PlayerCC controller)
        {
            if (cooldownTimer > 0f)
            {
                cooldownTimer -= Time.deltaTime;
            }

            if (!Input.GetKeyDown(dashKey) || cooldownTimer > 0f)
            {
                return;
            }

            Vector2 input = controller.GetMoveInput();
            float horizontal = 0f;

            if (input.x > 0.1f)
            {
                horizontal = 1f;
            }
            else if (input.x < -0.1f)
            {
                horizontal = -1f;
            }

            if (Mathf.Approximately(horizontal, 0f))
            {
                return;
            }

            Vector3 dashDir = horizontal > 0f ? Vector3.right : Vector3.left;
            controller.SetFacing(dashDir);
            controller.GetCharacterController().Move(dashDir * dashDistance);
            cooldownTimer = cooldown;
        }
    }
}
