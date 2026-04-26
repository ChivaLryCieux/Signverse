using UnityEngine;

namespace Skills
{
    [CreateAssetMenu(fileName = "33-dd", menuName = "Game/Skills/33 DD Dash")]
    public class Skill33DDUltraDash : SkillBase
    {
        // ===== 元数据 =====

        // ===== 物理控制 =====
        [Header("基础冲刺")]
        [SerializeField] protected float dashDistance = 9f;
        [SerializeField] protected float dashDuration = 0.12f;
        [SerializeField] protected float cooldown = 1.25f;

        [SerializeField] protected AnimationCurve dashPostureCurve = new AnimationCurve(
            new Keyframe(0f, 0f),
            new Keyframe(0.25f, 1f),
            new Keyframe(1f, 0f)
        );
        [SerializeField] [Min(1)]
        protected int dashCurveSamples = 20;

        protected float cooldownTimer;
        protected float dashTimer;
        protected float dashElapsed;
        protected float dashCurveArea = 1f;
        protected Vector3 dashDirection;
        protected bool isDashing;

        protected virtual float DashDistance => dashDistance;
        protected virtual float DashDuration => dashDuration;
        protected virtual float DashCooldown => cooldown;

        // 满足姿态和冷却条件时开始冲刺。
        public override void OnActivate(GameObject user, PlayerCC controller, PlayerCC.Posture posture)
        {
            if (posture == PlayerCC.Posture.Climbing)
            {
                return;
            }

            if (isDashing || cooldownTimer > 0f)
            {
                return;
            }

            if (!TryGetDashDirection(controller, out Vector3 dashDirection))
            {
                return;
            }

            StartDash(controller, dashDirection);
        }

        // 每帧处理冲刺冷却、冲刺推进和按键触发。
        public override void OnUpdate(GameObject user, PlayerCC controller, PlayerCC.Posture posture)
        {
            if (posture == PlayerCC.Posture.Climbing)
            {
                if (isDashing)
                {
                    StopDash(controller);
                }

                return;
            }

            if (cooldownTimer > 0f)
            {
                cooldownTimer -= Time.deltaTime;
            }

            if (isDashing)
            {
                UpdateDash(controller);
                return;
            }

            if (controller.WasDashPressed())
            {
                OnActivate(user, controller, posture);
            }
        }

        // 根据输入或角色朝向计算本次冲刺方向。
        protected virtual bool TryGetDashDirection(PlayerCC controller, out Vector3 dashDirection)
        {
            Vector2 input = controller.GetMoveInput();

            if (input.x > 0.1f)
            {
                dashDirection = Vector3.right;
                return true;
            }

            if (input.x < -0.1f)
            {
                dashDirection = Vector3.left;
                return true;
            }

            dashDirection = controller.GetFacing().sqrMagnitude > 0.01f ? controller.GetFacing() : Vector3.right;
            return true;
        }

        // 初始化冲刺计时、方向、曲线面积和动画姿态值。
        protected void StartDash(PlayerCC controller, Vector3 dashDirection)
        {
            float actualDuration = Mathf.Max(0.01f, DashDuration);
            Vector3 normalizedDirection = dashDirection.normalized;

            isDashing = true;
            dashTimer = actualDuration;
            dashElapsed = 0f;
            this.dashDirection = normalizedDirection;
            dashCurveArea = Mathf.Max(0.01f, CalculateCurveArea(dashPostureCurve, dashCurveSamples));
            cooldownTimer = DashCooldown;
            controller.SetDashPosture(EvaluateDashPosture(0f));

            if (Mathf.Abs(normalizedDirection.x) > 0.01f)
            {
                controller.SetFacing(normalizedDirection.x > 0f ? Vector3.right : Vector3.left);
            }
        }

        // 按曲线逐帧推进角色，并在碰撞或时间结束时停止。
        protected virtual void UpdateDash(PlayerCC controller)
        {
            float actualDuration = Mathf.Max(0.01f, DashDuration);
            float step = Mathf.Min(Time.deltaTime, dashTimer);
            dashElapsed += step;
            dashTimer -= step;

            float normalizedTime = Mathf.Clamp01(dashElapsed / actualDuration);
            float posture = EvaluateDashPosture(normalizedTime);
            controller.SetDashPosture(posture);

            Vector3 dashDelta = dashDirection * (DashDistance * posture / (dashCurveArea * actualDuration)) * step;
            CollisionFlags flags = controller.GetCharacterController().Move(dashDelta);

            if ((flags & (CollisionFlags.Sides | CollisionFlags.Above)) != 0 || dashTimer <= 0f)
            {
                StopDash(controller);
            }
        }

        // 停止冲刺并重置冲刺运行时状态。
        protected void StopDash(PlayerCC controller)
        {
            isDashing = false;
            dashTimer = 0f;
            dashElapsed = 0f;
            dashDirection = Vector3.zero;

            if (controller != null)
            {
                controller.SetDashPosture(0f);
            }
        }

        // ===== 动画控制 =====

        // 采样冲刺姿态曲线，输出 0 到 1 的动画/速度权重。
        protected float EvaluateDashPosture(float normalizedTime)
        {
            if (dashPostureCurve == null || dashPostureCurve.length == 0)
            {
                return 1f;
            }

            return Mathf.Clamp01(dashPostureCurve.Evaluate(Mathf.Clamp01(normalizedTime)));
        }

        // 估算曲线面积，用来归一化冲刺总距离。
        private float CalculateCurveArea(AnimationCurve curve, int sampleCount)
        {
            if (curve == null || curve.length == 0)
            {
                return 1f;
            }

            int samples = Mathf.Max(1, sampleCount);
            float area = 0f;
            float previousValue = Mathf.Max(0f, curve.Evaluate(0f));

            for (int i = 1; i <= samples; i++)
            {
                float time = i / (float)samples;
                float value = Mathf.Max(0f, curve.Evaluate(time));
                area += (previousValue + value) * 0.5f / samples;
                previousValue = value;
            }

            return area;
        }
    }
}
