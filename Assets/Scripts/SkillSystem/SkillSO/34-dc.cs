using UnityEngine;

namespace Skills
{
    [CreateAssetMenu(fileName = "34-dc", menuName = "Game/Skills/34 DC Placeholder")]
    public class Skill34DCPlaceholder : Skill33DDUltraDash
    {
        // ===== 元数据 =====

        // ===== 物理控制 =====
        protected override bool UsesUltraDashAnimation => false;
    }
}
