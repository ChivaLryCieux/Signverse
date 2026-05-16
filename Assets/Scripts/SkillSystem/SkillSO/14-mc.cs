using UnityEngine;

namespace Skills
{
    [CreateAssetMenu(fileName = "14-mc", menuName = "Game/Skills/14 MC Placeholder")]
    public class Skill14MCPlaceholder : SkillBase
    {
        // ===== 元数据 =====
        [Header("移动隐身")]
        public float moveSpeed = 6f;
        [Tooltip("移动输入低于该值时视为站定并进入隐身。")]
        [Range(0f, 1f)] public float stationaryInputThreshold = 0.05f;

        // ===== 物理控制 =====
        // 被动技能，激活入口不需要处理。
        public override void OnActivate(GameObject user, PlayerCC controller, PlayerCC.Posture posture) { }

        public override void OnUpdate(GameObject user, PlayerCC controller, PlayerCC.Posture posture)
        {
            Vector2 input = controller.GetMoveInput();
            bool isStationary = input.sqrMagnitude <= stationaryInputThreshold * stationaryInputThreshold;

            RequestCloakEffect(user, isStationary);

            float horizontal = input.x;
            if (Mathf.Abs(horizontal) <= 0.01f)
            {
                return;
            }

            Vector3 moveDelta = new Vector3(horizontal * moveSpeed, 0f, 0f) * Time.deltaTime;
            controller.MoveCharacter(moveDelta);
        }

        // ===== 动画控制 =====

        private void RequestCloakEffect(GameObject user, bool active)
        {
            CloakEffectController cloakEffect = user.GetComponent<CloakEffectController>();
            if (cloakEffect == null)
            {
                cloakEffect = user.AddComponent<CloakEffectController>();
            }

            cloakEffect.RequestCloak(this, active);
        }
    }
}
