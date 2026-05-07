using UnityEngine;

namespace Skills
{
    [CreateAssetMenu(fileName = "31-dm", menuName = "Game/Skills/31 DM Dash Distance Up")]
    public class Skill31DMDashDistanceUp : Skill30StdDash
    {
        // ===== 元数据 =====

        // ===== 物理控制 =====
        [SerializeField] private float upgradedDashDistance = 14f;

        protected override float DashDistance => upgradedDashDistance;

        // ===== 动画控制 =====
    }
}
