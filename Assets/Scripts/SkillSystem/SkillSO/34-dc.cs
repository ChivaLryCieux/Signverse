using UnityEngine;

namespace Skills
{
    [CreateAssetMenu(fileName = "34-dc", menuName = "Game/Skills/34 DC Placeholder")]
    public class Skill34DCPlaceholder : Skill33DDUltraDash
    {
        // ===== 元数据 =====

        // ===== 物理控制 =====
        // 和基础冲刺一样按 Dash 触发，但冲刺期间持续请求死亡拦截。
        public override void OnUpdate(GameObject user, PlayerCC controller, PlayerCC.Posture posture)
        {
            RequestInvincibilityWhileDashing(user);

            base.OnUpdate(user, controller, posture);

            RequestInvincibilityWhileDashing(user);
        }

        // ===== 动画控制 =====

        private void RequestInvincibilityWhileDashing(GameObject user)
        {
            if (!isDashing)
            {
                return;
            }

            PlayerDeath death = user.GetComponent<PlayerDeath>();
            if (death != null)
            {
                death.RequestDeathBlock();
            }
        }
    }
}
