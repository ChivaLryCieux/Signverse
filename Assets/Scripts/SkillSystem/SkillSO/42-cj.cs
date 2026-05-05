using UnityEngine;

namespace Skills
{
    [CreateAssetMenu(fileName = "42-cj", menuName = "Game/Skills/42 CJ Auto Cloak")]
    public class Skill42CJAutoCloak : Skill4CloakBase
    {
        // ===== 元数据 =====

        // ===== 物理控制 =====
        [Header("被动隐身循环")]
        [Min(0.01f)] public float invisibleDuration = 2f;
        [Min(0f)] public float pauseDuration = 4f;

        private float timer;
        private bool isInvisible = true;

        // 自动隐身是循环型技能，激活时不需要额外处理。
        public override void OnActivate(GameObject user, PlayerCC controller, PlayerCC.Posture posture) { }

        // 每帧推进隐身循环计时，并自动切换可见性。
        public override void OnUpdate(GameObject user, PlayerCC controller, PlayerCC.Posture posture)
        {
            RequestCloakEffect(user, isInvisible);

            timer += Time.deltaTime;
            float phaseDuration = isInvisible ? invisibleDuration : pauseDuration;

            if (timer < phaseDuration)
            {
                return;
            }

            timer = 0f;
            isInvisible = !isInvisible;
            RequestCloakEffect(user, isInvisible);
        }
        // ===== 动画控制 =====
    }
}
