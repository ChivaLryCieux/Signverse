using UnityEngine;

namespace Skills
{
    [CreateAssetMenu(fileName = "31-dm", menuName = "Game/Skills/31 DM Dash Distance Up")]
    public class Skill31DMDashDistanceUp : SkillBase
    {
        [Header("冲刺设置")]
        public KeyCode dashKey = KeyCode.LeftShift;
        public float dashDistance = 5f;
        public float cooldown = 0.75f;

        private float cooldownTimer;

        public override void OnActivate(GameObject user, PlayerCC controller) { }

        public override void OnUpdate(GameObject user, PlayerCC controller)
        {
            cooldownTimer -= Time.deltaTime;
            if (cooldownTimer > 0f || !Input.GetKeyDown(dashKey)) return;

            Vector3 dir = controller.GetFacing().sqrMagnitude > 0.01f ? controller.GetFacing() : Vector3.right;
            controller.GetCharacterController().Move(dir * dashDistance);
            cooldownTimer = cooldown;
        }
    }
}
