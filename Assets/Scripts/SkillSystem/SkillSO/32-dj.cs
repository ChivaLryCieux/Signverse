using UnityEngine;

namespace Skills
{
    [CreateAssetMenu(fileName = "32-dj", menuName = "Game/Skills/32 DJ Eight Direction Dash")]
    public class Skill32DJEightDirectionDash : SkillBase
    {
        [Header("八向冲刺")]
        public KeyCode dashKey = KeyCode.L;
        public float dashDistance = 4f;
        public float cooldown = 0.8f;

        private float cooldownTimer;

        public override void OnActivate(GameObject user, PlayerCC controller) { }

        public override void OnUpdate(GameObject user, PlayerCC controller)
        {
            if (cooldownTimer > 0f)
            {
                cooldownTimer -= Time.deltaTime;
            }

            if (cooldownTimer > 0f || !Input.GetKeyDown(dashKey))
            {
                return;
            }

            Vector2 input = controller.GetMoveInput();
            Vector3 dashDir = new Vector3(input.x, input.y, 0f);

            if (dashDir.sqrMagnitude < 0.01f)
            {
                return;
            }

            controller.GetCharacterController().Move(dashDir.normalized * dashDistance);

            if (Mathf.Abs(dashDir.x) > 0.01f)
            {
                controller.SetFacing(dashDir.x > 0f ? Vector3.right : Vector3.left);
            }

            cooldownTimer = cooldown;
        }
    }
}
