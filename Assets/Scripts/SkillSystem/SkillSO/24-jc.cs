using UnityEngine;

namespace Skills
{
    [CreateAssetMenu(fileName = "24-jc", menuName = "Game/Skills/24 JC Placeholder")]
    public class Skill24JCPlaceholder : SkillBase
    {
        // ===== 元数据 =====
        [Header("预留参数")]
        [TextArea] public string designNote = "jump + cloak 组合技能预留。";

        // ===== 物理控制 =====
        public override void OnActivate(GameObject user, PlayerCC controller, PlayerCC.Posture posture) { }

        // ===== 动画控制 =====
    }
}
