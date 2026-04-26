using UnityEngine;

namespace Skills
{
    [CreateAssetMenu(fileName = "31-dm", menuName = "Game/Skills/31 DM Dash Distance Up")]
    public class Skill31DMDashDistanceUp : Skill33DDUltraDash
    {
        // ===== 元数据 =====

        // ===== 物理控制 =====
        protected override float DashDistance => 7f;

        // ===== 动画控制 =====
    }
}
