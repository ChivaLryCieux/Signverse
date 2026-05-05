using UnityEngine;

namespace Skills
{
    [CreateAssetMenu(fileName = "StdDash", menuName = "Game/Skills/Legacy/Standard Dash")]
    public class StdDash : Skill33DDUltraDash
    {
        // ===== 元数据 =====

        // ===== 物理控制 =====
        protected override bool UsesUltraDashAnimation => false;

        // ===== 动画控制 =====
    }
}
