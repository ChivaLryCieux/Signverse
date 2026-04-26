using UnityEngine;

namespace Skills
{
    [CreateAssetMenu(fileName = "23-jd", menuName = "Game/Skills/23 JD Jetpack")]
    public class Skill23JDBlinkJump : SkillBase
    {
        // ===== 元数据 =====

        // ===== 物理控制 =====
        [Header("喷气背包")]
        public float startHeight = 1f;
        public float accumulateThrustSpeed = 1f;
        public float maxThrustHeight = 5f;
        public float PureThrustTime = 0.5f;

        [Header("运行时")]
        public float ThrustHeight;

        private bool isCharging;

        // 激活喷气背包蓄力流程。
        public override void OnActivate(GameObject user, PlayerCC controller, PlayerCC.Posture posture)
        {
            BeginCharge();
        }

        // 每帧处理按住蓄力和松开发射。
        public override void OnUpdate(GameObject user, PlayerCC controller, PlayerCC.Posture posture)
        {
            if (controller.WasJumpPressed())
            {
                BeginCharge();
            }

            if (!isCharging)
            {
                return;
            }

            if (controller.IsJumpPressed())
            {
                ThrustHeight = Mathf.Min(maxThrustHeight, ThrustHeight + accumulateThrustSpeed * Time.deltaTime);
                return;
            }

            if (controller.WasJumpReleased())
            {
                Launch(controller);
            }
        }

        // 初始化蓄力状态和起始推力高度。
        private void BeginCharge()
        {
            isCharging = true;
            ThrustHeight = Mathf.Clamp(startHeight, 0f, maxThrustHeight);
        }

        // 根据蓄力高度设置竖直速度，并短暂禁用横向输入。
        private void Launch(PlayerCC controller)
        {
            isCharging = false;

            float launchHeight = Mathf.Clamp(ThrustHeight, 0f, maxThrustHeight);
            float verticalVelocity = Mathf.Sqrt(launchHeight * -2f * controller.gravity);
            controller.SetVerticalVelocity(verticalVelocity);
            controller.DisableMoveXFor(PureThrustTime);
        }

        // ===== 动画控制 =====
    }
}
