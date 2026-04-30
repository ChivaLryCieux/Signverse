using UnityEngine;

namespace Skills
{
    [CreateAssetMenu(fileName = "43-cd", menuName = "Game/Skills/43 CD Cloak Kill")]
    public class Skill43CDCloakKill : SkillBase
    {
        // ===== 元数据 =====

        // ===== 物理控制 =====
        [Header("死亡规避")]
        [Min(0.01f)] public float invisibleDuration = 1f;
        [Min(0f)] public float invincibleDuration = 2f;
        [Min(0f)] public float cooldown = 3.5f;

        private float invisibleTimer;
        private float cooldownTimer;
        private bool isInvisible;

        // 死亡规避由隐藏键触发，激活入口暂不处理。
        public override void OnActivate(GameObject user, PlayerCC controller, PlayerCC.Posture posture) { }

        // 按下隐藏键后进入 1 秒判定窗：窗口内挡下一次死亡并获得无敌，否则窗口结束时死亡。
        public override void OnUpdate(GameObject user, PlayerCC controller, PlayerCC.Posture posture)
        {
            if (cooldownTimer > 0f)
            {
                cooldownTimer -= Time.deltaTime;
            }

            if (!isInvisible && cooldownTimer <= 0f && controller.WasHidePressed())
            {
                isInvisible = true;
                invisibleTimer = invisibleDuration;
                cooldownTimer = cooldown;
            }

            RequestCloakEffect(user, isInvisible);

            if (!isInvisible) return;

            PlayerDeath death = user.GetComponent<PlayerDeath>();
            if (death != null)
            {
                death.RequestDeathBlock();
                if (death.ConsumeDeathBlockedThisFrame())
                {
                    isInvisible = false;
                    RequestCloakEffect(user, false);
                    death.GrantInvincibility(invincibleDuration);
                    return;
                }
            }

            invisibleTimer -= Time.deltaTime;
            if (invisibleTimer > 0f)
            {
                return;
            }

            isInvisible = false;
            RequestCloakEffect(user, false);

            if (death != null)
            {
                death.ForceDie();
            }
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
