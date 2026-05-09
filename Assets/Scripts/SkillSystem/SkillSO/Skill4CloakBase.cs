using UnityEngine;

namespace Skills
{
    public abstract class Skill4CloakBase : SkillBase
    {
        [Header("体积雾")]
        [Range(0f, 1f)] public float volumeTargetWeight = 1f;
        [Min(0f)] public float volumeEnterLerpSpeed = 4f;
        [Min(0f)] public float volumeExitLerpSpeed = 4f;

        protected void RequestCloakEffect(GameObject user, bool active)
        {
            if (user == null)
            {
                return;
            }

            CloakEffectController cloakEffect = user.GetComponent<CloakEffectController>();
            if (cloakEffect == null)
            {
                cloakEffect = user.AddComponent<CloakEffectController>();
            }

            cloakEffect.RequestCloak(this, active, volumeTargetWeight, volumeEnterLerpSpeed, volumeExitLerpSpeed, false);
        }
    }
}
