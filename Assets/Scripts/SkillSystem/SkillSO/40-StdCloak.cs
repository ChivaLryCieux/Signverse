using UnityEngine;

namespace Skills
{
    [CreateAssetMenu(fileName = "40-StdCloak", menuName = "Game/Skills/40 Standard Cloak")]
    public class Skill40StdCloak : SkillBase
    {
        [Header("基础隐身")]
        [Min(0.01f)] public float cloakDuration = 5f;
        [Min(0f)] public float cooldownDuration = 6f;

        [Header("体积雾")]
        [Range(0f, 1f)] public float volumeTargetWeight = 1f;
        [Min(0f)] public float volumeEnterLerpSpeed = 4f;
        [Min(0f)] public float volumeExitLerpSpeed = 4f;

        private bool isCloaking;
        private float cloakTimer;
        private float cooldownTimer;
        private GameObject lastUser;
        private int lastUpdateFrame = -1;

        public override void OnActivate(GameObject user, PlayerCC controller, PlayerCC.Posture posture) { }

        public override void OnUpdate(GameObject user, PlayerCC controller, PlayerCC.Posture posture)
        {
            bool resumedAfterInactive = lastUser != user || lastUpdateFrame < 0 || Time.frameCount - lastUpdateFrame > 1;
            lastUser = user;
            lastUpdateFrame = Time.frameCount;

            if (resumedAfterInactive)
            {
                ResetRuntimeState();
            }

            if (cooldownTimer > 0f)
            {
                cooldownTimer -= Time.deltaTime;
            }

            CloakEffectController cloakEffect = user.GetComponent<CloakEffectController>();
            if (cloakEffect == null)
            {
                cloakEffect = user.AddComponent<CloakEffectController>();
            }

            if (!isCloaking)
            {
                if (cooldownTimer <= 0f && controller.WasHidePressed())
                {
                    isCloaking = true;
                    cloakTimer = cloakDuration;
                }
                else
                {
                    return;
                }
            }

            cloakTimer -= Time.deltaTime;
            if (cloakTimer <= 0f)
            {
                isCloaking = false;
                cooldownTimer = cooldownDuration;
                cloakEffect.RequestCloak(this, false, volumeTargetWeight, volumeEnterLerpSpeed, volumeExitLerpSpeed);
                return;
            }

            cloakEffect.RequestCloak(this, true, volumeTargetWeight, volumeEnterLerpSpeed, volumeExitLerpSpeed);
        }

        private void ResetRuntimeState()
        {
            isCloaking = false;
            cloakTimer = 0f;
            cooldownTimer = 0f;
        }
    }
}
