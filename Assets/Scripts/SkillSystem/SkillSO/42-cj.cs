using UnityEngine;

namespace Skills
{
    [CreateAssetMenu(fileName = "42-cj", menuName = "Game/Skills/42 CJ Auto Cloak")]
    public class Skill42CJAutoCloak : SkillBase
    {
        // ===== 元数据 =====

        // ===== 物理控制 =====
        [Header("隐身循环")]
        public float interval = 1.5f;
        public float invisibleDuration = 0.5f;

        private float timer;
        private float invisibleTimer;
        private bool isInvisible;

        // 自动隐身是循环型技能，激活时不需要额外处理。
        public override void OnActivate(GameObject user, PlayerCC controller, PlayerCC.Posture posture) { }

        // 每帧推进隐身循环计时，并自动切换可见性。
        public override void OnUpdate(GameObject user, PlayerCC controller, PlayerCC.Posture posture)
        {
            timer += Time.deltaTime;

            if (!isInvisible && timer >= interval)
            {
                timer = 0f;
                invisibleTimer = invisibleDuration;
                isInvisible = true;
            }

            RequestCloakEffect(user, isInvisible);

            if (!isInvisible) return;

            invisibleTimer -= Time.deltaTime;
            if (invisibleTimer > 0f) return;

            isInvisible = false;
            RequestCloakEffect(user, false);
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
