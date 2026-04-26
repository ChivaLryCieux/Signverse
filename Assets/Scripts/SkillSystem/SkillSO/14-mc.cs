using UnityEngine;

namespace Skills
{
    [CreateAssetMenu(fileName = "14-mc", menuName = "Game/Skills/14 MC Placeholder")]
    public class Skill14MCPlaceholder : SkillBase
    {
        // ===== 元数据 =====
        [Header("预留参数")]
        [TextArea] public string designNote = "move + cloak 组合技能预留，后续可替换为正式机制。";

        // ===== 物理控制 =====
        // 组合技能占位，当前激活时不执行效果。
        public override void OnActivate(GameObject user, PlayerCC controller, PlayerCC.Posture posture) { }

        // ===== 动画控制 =====
    }
}
