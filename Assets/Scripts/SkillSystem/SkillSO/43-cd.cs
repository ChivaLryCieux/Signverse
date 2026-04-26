using UnityEngine;

namespace Skills
{
    [CreateAssetMenu(fileName = "43-cd", menuName = "Game/Skills/43 CD Cloak Kill")]
    public class Skill43CDCloakKill : SkillBase
    {
        // ===== 元数据 =====

        // ===== 物理控制 =====
        [Header("潜行刺杀")]
        public float invisibleDuration = 0.75f;
        public float triggerRadius = 1f;
        public LayerMask enemyMask;

        private float invisibleTimer;
        private bool isInvisible;

        // 潜行刺杀由隐藏键触发，激活入口暂不处理。
        public override void OnActivate(GameObject user, PlayerCC controller, PlayerCC.Posture posture) { }

        // 每帧处理短暂隐身、范围击杀和隐身结束恢复。
        public override void OnUpdate(GameObject user, PlayerCC controller, PlayerCC.Posture posture)
        {
            if (!isInvisible && controller.WasHidePressed())
            {
                isInvisible = true;
                invisibleTimer = invisibleDuration;
                SetVisible(user, false);
            }

            if (!isInvisible) return;

            invisibleTimer -= Time.deltaTime;
            Collider[] hits = Physics.OverlapSphere(user.transform.position, triggerRadius, enemyMask);
            foreach (Collider hit in hits)
            {
                if (hit.gameObject == user) continue;
                Destroy(hit.gameObject);
            }

            if (invisibleTimer > 0f) return;

            isInvisible = false;
            SetVisible(user, true);
        }

        // 切换角色所有子 Renderer 的显示状态。
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
