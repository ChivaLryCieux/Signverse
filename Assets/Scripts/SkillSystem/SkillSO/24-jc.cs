using UnityEngine;

namespace Skills
{
    [CreateAssetMenu(fileName = "24-jc", menuName = "Game/Skills/24 JC Placeholder")]
    public class Skill24JCPlaceholder : SkillBase
    {
        [Header("预留参数")]
        [TextArea] public string designNote = "jump + cloak 组合技能预留。";

        public override void OnActivate(GameObject user, PlayerCC controller) { }
    }
}
