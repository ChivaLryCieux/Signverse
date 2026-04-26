using UnityEngine;

namespace Skills
{
    [CreateAssetMenu(fileName = "44-cc", menuName = "Game/Skills/44 CC Placeholder")]
    public class Skill44CCPlaceholder : SkillBase
    {
        // ===== 元数据 =====
        [Header("预留参数")]
        [TextArea] public string designNote = "cloak + cloak 组合技能预留。";

        // ===== 物理控制 =====
        // 组合技能占位，当前激活时不执行效果。
        public override void OnActivate(GameObject user, PlayerCC controller, PlayerCC.Posture posture) { }

        // ===== 动画控制 =====
    }
}
