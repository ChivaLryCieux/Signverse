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

        public override void OnActivate(GameObject user, PlayerCC controller, PlayerCC.Posture posture) { }

        public override void OnUpdate(GameObject user, PlayerCC controller, PlayerCC.Posture posture)
        {
            timer += Time.deltaTime;

            if (!isInvisible && timer >= interval)
            {
                timer = 0f;
                invisibleTimer = invisibleDuration;
                isInvisible = true;
                SetVisible(user, false);
            }

            if (!isInvisible) return;

            invisibleTimer -= Time.deltaTime;
            if (invisibleTimer > 0f) return;

            isInvisible = false;
            SetVisible(user, true);
        }

        private void SetVisible(GameObject user, bool visible)
        {
            Renderer[] renderers = user.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in renderers)
            {
                renderer.enabled = visible;
            }
        }

        // ===== 动画控制 =====
    }
}
