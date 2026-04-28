using UnityEngine;

namespace Skills
{
    [CreateAssetMenu(fileName = "40-StdCloak", menuName = "Game/Skills/40 Standard Cloak")]
    public class Skill40StdCloak : SkillBase
    {
        [Header("基础隐身")]
        [Tooltip("勾选后按住隐藏键隐身，松开退出。")]
        public bool cloakWhileHideHeld = true;

        public override void OnActivate(GameObject user, PlayerCC controller, PlayerCC.Posture posture) { }

        public override void OnUpdate(GameObject user, PlayerCC controller, PlayerCC.Posture posture)
        {
            CloakEffectController cloakEffect = user.GetComponent<CloakEffectController>();
            if (cloakEffect == null)
            {
                cloakEffect = user.AddComponent<CloakEffectController>();
            }

            cloakEffect.RequestCloak(this, cloakWhileHideHeld && controller.IsHidePressed());
        }
    }
}
