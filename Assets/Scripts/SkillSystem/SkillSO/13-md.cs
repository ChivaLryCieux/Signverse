using UnityEngine;

namespace Skills
{
    [CreateAssetMenu(fileName = "13-md", menuName = "Game/Skills/13 MD Ice Slide")]
    public class Skill13MDIceSlide : SkillBase
    {
        // ===== 元数据 =====

        // ===== 物理控制 =====
        [Header("高速移动")]
        public float moveSpeed = 9f;

        // 高速移动是持续型技能，激活时不需要额外处理。
        public override void OnActivate(GameObject user, PlayerCC controller, PlayerCC.Posture posture) { }

        // 每帧读取横向输入，并以更高速度移动。
        public override void OnUpdate(GameObject user, PlayerCC controller, PlayerCC.Posture posture)
        {
            Vector2 input = controller.GetMoveInput();
            float horizontal = input.x;

            if (Mathf.Abs(horizontal) <= 0.01f)
            {
                return;
            }

            Vector3 moveDelta = new Vector3(horizontal * moveSpeed, 0f, 0f) * Time.deltaTime;
            controller.GetCharacterController().Move(moveDelta);
        }

        // ===== 动画控制 =====
    }
}
