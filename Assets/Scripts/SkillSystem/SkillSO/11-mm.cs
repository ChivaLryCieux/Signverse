using UnityEngine;

namespace Skills
{
    [CreateAssetMenu(fileName = "11-mm", menuName = "Game/Skills/11 MM Move")]
    public class Skill11MMCustomPortal : SkillBase
    {
        // ===== 元数据 =====

        // ===== 物理控制 =====
        [Header("超移动")]
        [Tooltip("装备 11 后的整体移动速度，横向和竖向都会使用这个速度。")]
        public float moveSpeed = 3f;

        // 基础移动是持续型技能，激活时不需要额外处理。
        public override void OnActivate(GameObject user, PlayerCC controller, PlayerCC.Posture posture) { }

        // 每帧取消重力，读取横向/竖向输入，并通过 CharacterController 执行慢速自由移动。
        public override void OnUpdate(GameObject user, PlayerCC controller, PlayerCC.Posture posture)
        {
            controller.RequestGravitySuppressed();

            Vector2 input = controller.GetMoveInput();

            if (input.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            Vector2 clampedInput = Vector2.ClampMagnitude(input, 1f);
            Vector3 moveDelta = new Vector3(clampedInput.x, clampedInput.y, 0f) * moveSpeed * Time.deltaTime;
            controller.MoveWithGroundProtection(moveDelta);
        }

        // ===== 动画控制 =====
    }
}
