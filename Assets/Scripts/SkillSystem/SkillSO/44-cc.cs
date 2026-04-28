using UnityEngine;

namespace Skills
{
    [CreateAssetMenu(fileName = "44-cc", menuName = "Game/Skills/44 CC Placeholder")]
    public class Skill44CCPlaceholder : SkillBase
    {
        // ===== 元数据 =====
        [Header("隐身")]
        [Tooltip("勾选后按住隐藏键隐身，松开退出。")]
        public bool cloakWhileHideHeld = true;

        // ===== 物理控制 =====
        // 组合技能占位，当前激活时不执行效果。
        public override void OnActivate(GameObject user, PlayerCC controller, PlayerCC.Posture posture) { }

        public override void OnUpdate(GameObject user, PlayerCC controller, PlayerCC.Posture posture)
        {
            CloakEffectController cloakEffect = user.GetComponent<CloakEffectController>();
            if (cloakEffect == null)
            {
                cloakEffect = user.AddComponent<CloakEffectController>();
            }

            cloakEffect.RequestCloak(this, cloakWhileHideHeld && controller.IsHidePressed());
        }

        // ===== 动画控制 =====
    }
}
