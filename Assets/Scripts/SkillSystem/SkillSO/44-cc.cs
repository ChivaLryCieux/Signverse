using UnityEngine;

namespace Skills
{
    [CreateAssetMenu(fileName = "44-cc", menuName = "Game/Skills/44 CC Placeholder")]
    public class Skill44CCPlaceholder : SkillBase
    {
        [Header("预留参数")]
        [TextArea] public string designNote = "cloak + cloak 组合技能预留。";

        public override void OnActivate(GameObject user, PlayerCC controller) { }
    }
}
