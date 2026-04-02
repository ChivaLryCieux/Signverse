using UnityEngine;

namespace Skills
{
    [CreateAssetMenu(fileName = "14-mc", menuName = "Game/Skills/14 MC Placeholder")]
    public class Skill14MCPlaceholder : SkillBase
    {
        [Header("预留参数")]
        [TextArea] public string designNote = "move + cloak 组合技能预留，后续可替换为正式机制。";

        public override void OnActivate(GameObject user, PlayerCC controller) { }
    }
}
