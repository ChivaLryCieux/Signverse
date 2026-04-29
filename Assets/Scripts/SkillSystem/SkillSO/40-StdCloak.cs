using UnityEngine;

namespace Skills
{
    [CreateAssetMenu(fileName = "40-StdCloak", menuName = "Game/Skills/40 Standard Cloak")]
    public class Skill40StdCloak : SkillBase
    {
        [Header("基础隐身")]
        [Tooltip("勾选后按下隐藏键切换隐身开关。")]
        public bool toggleOnHidePressed = true;

        private bool isCloaking;

        public override void OnActivate(GameObject user, PlayerCC controller, PlayerCC.Posture posture) { }

        public override void OnUpdate(GameObject user, PlayerCC controller, PlayerCC.Posture posture)
        {
            if (toggleOnHidePressed && controller.WasHidePressed())
            {
                isCloaking = !isCloaking;
            }

            CloakEffectController cloakEffect = user.GetComponent<CloakEffectController>();
            if (cloakEffect == null)
            {
                cloakEffect = user.AddComponent<CloakEffectController>();
            }

            cloakEffect.RequestCloak(this, isCloaking);
        }
    }
}
