using UnityEngine;

namespace Skills
{
    [CreateAssetMenu(fileName = "43-cd", menuName = "Game/Skills/43 CD Cloak Kill")]
    public class Skill43CDCloakKill : SkillBase
    {
        [Header("潜行刺杀")]
        public float invisibleDuration = 0.75f;
        public float triggerRadius = 1f;
        public LayerMask enemyMask;

        private float invisibleTimer;
        private bool isInvisible;

        public override void OnActivate(GameObject user, PlayerCC controller) { }

        public override void OnUpdate(GameObject user, PlayerCC controller)
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

        private void SetVisible(GameObject user, bool visible)
        {
            Renderer[] renderers = user.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in renderers)
            {
                renderer.enabled = visible;
            }
        }
    }
}
