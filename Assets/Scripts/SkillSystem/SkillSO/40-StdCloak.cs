using UnityEngine;

namespace Skills
{
    [CreateAssetMenu(fileName = "40-StdCloak", menuName = "Game/Skills/40 Standard Cloak")]
    public class Skill40StdCloak : SkillBase
    {
        [Header("预留参数")]
        [TextArea] public string designNote = "cloak + empty standard skill placeholder.";

        public override void OnActivate(GameObject user, PlayerCC controller, PlayerCC.Posture posture) { }
    }
}
