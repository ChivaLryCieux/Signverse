using UnityEngine;

namespace Skills
{
    [CreateAssetMenu(fileName = "13-md", menuName = "Game/Skills/13 MD Ice Slide")]
    public class Skill13MDIceSlide : SkillBase
    {
        // ===== 元数据 =====

        // ===== 物理控制 =====
        [Header("溜冰参数")]
        public float slideSpeed = 8f;
        public float acceleration = 12f;
        public float deceleration = 2f;

        private float currentSpeed;

        public override void OnActivate(GameObject user, PlayerCC controller, PlayerCC.Posture posture) { }

        public override void OnUpdate(GameObject user, PlayerCC controller, PlayerCC.Posture posture)
        {
            Vector2 input = controller.GetMoveInput();
            float targetSpeed = Mathf.Abs(input.x) > 0.1f ? input.x * slideSpeed : 0f;
            float rate = Mathf.Abs(targetSpeed) > 0.01f ? acceleration : deceleration;
            currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, rate * Time.deltaTime);

            if (Mathf.Abs(currentSpeed) < 0.01f) return;

            controller.GetCharacterController().Move(new Vector3(currentSpeed, 0f, 0f) * Time.deltaTime);
        }

        // ===== 动画控制 =====
    }
}
