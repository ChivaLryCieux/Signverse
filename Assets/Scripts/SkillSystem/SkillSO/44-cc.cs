using UnityEngine;

namespace Skills
{
    [CreateAssetMenu(fileName = "44-cc", menuName = "Game/Skills/44 CC Placeholder")]
    public class Skill44CCPlaceholder : Skill4CloakBase
    {
        // ===== 元数据 =====
        [Header("隐身")]
        [Tooltip("勾选后按下隐藏键切换隐身开关。")]
        public bool toggleOnHidePressed = true;

        private bool isCloaking;

        // ===== 物理控制 =====
        // 组合技能占位，当前激活时不执行效果。
        public override void OnActivate(GameObject user, PlayerCC controller, PlayerCC.Posture posture) { }

        public override void OnUpdate(GameObject user, PlayerCC controller, PlayerCC.Posture posture)
        {
            if (toggleOnHidePressed && controller.WasHidePressed())
            {
                isCloaking = !isCloaking;
            }

            RequestCloakEffect(user, isCloaking);

            if (isCloaking)
            {
                PlayerDeath death = user.GetComponent<PlayerDeath>();
                if (death != null)
                {
                    death.RequestDeathBlock();
                }
            }
        }

        // ===== 动画控制 =====
    }
}
