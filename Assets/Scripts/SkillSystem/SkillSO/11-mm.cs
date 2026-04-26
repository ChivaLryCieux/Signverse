using UnityEngine;

namespace Skills
{
    [CreateAssetMenu(fileName = "11-mm", menuName = "Game/Skills/11 MM Move")]
    public class Skill11MMCustomPortal : SkillBase
    {
        // ===== 元数据 =====

        // ===== 物理控制 =====
        [Header("基础移动")]
        public float moveSpeed = 6f;

        public override void OnActivate(GameObject user, PlayerCC controller, PlayerCC.Posture posture) { }

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
