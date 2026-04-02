using UnityEngine;

namespace Skills
{
    [CreateAssetMenu(fileName = "33-dd", menuName = "Game/Skills/33 DD Ultra Dash")]
    public class Skill33DDUltraDash : SkillBase
    {
        [Header("超长冲刺")]
        public KeyCode dashKey = KeyCode.LeftShift;
        public float dashDistance = 9f;
        public float cooldown = 1.25f;

        private float cooldownTimer;

        public override void OnActivate(GameObject user, PlayerCC controller) { }

        public override void OnUpdate(GameObject user, PlayerCC controller)
        {
            cooldownTimer -= Time.deltaTime;
            if (cooldownTimer > 0f || !Input.GetKeyDown(dashKey)) return;

            Vector3 dir = controller.GetFacing().sqrMagnitude > 0.01f ? controller.GetFacing() : Vector3.right;
            controller.GetCharacterController().Move(dir.normalized * dashDistance);
            cooldownTimer = cooldown;
        }
    }
}
