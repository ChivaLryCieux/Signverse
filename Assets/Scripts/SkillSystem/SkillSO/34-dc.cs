using UnityEngine;

namespace Skills
{
    [CreateAssetMenu(fileName = "34-dc", menuName = "Game/Skills/34 DC Placeholder")]
    public class Skill34DCPlaceholder : SkillBase
    {
        // ===== 元数据 =====
        [Header("预留参数")]
        [TextArea] public string designNote = "dash + cloak 组合技能预留。";

        // ===== 物理控制 =====
        // 组合技能占位，当前激活时不执行效果。
        public override void OnActivate(GameObject user, PlayerCC controller, PlayerCC.Posture posture) { }

        // ===== 动画控制 =====
    }
}
