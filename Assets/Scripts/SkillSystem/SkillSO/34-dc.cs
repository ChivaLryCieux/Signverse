using UnityEngine;

namespace Skills
{
    [CreateAssetMenu(fileName = "34-dc", menuName = "Game/Skills/34 DC Invincible Dash")]
    public class Skill34DCInvincibleDash : Skill30StdDash
    {
        [Header("无敌冲刺")]
        [SerializeField] [Min(0f)]
        private float invincibleDuration = 0.2f;

        protected override void OnDashStarted(PlayerCC controller, float duration)
        {
            PlayerDeath death = controller.GetComponent<PlayerDeath>();
            if (death != null)
            {
                death.GrantInvincibility(Mathf.Max(invincibleDuration, duration));
            }
        }
    }
}
