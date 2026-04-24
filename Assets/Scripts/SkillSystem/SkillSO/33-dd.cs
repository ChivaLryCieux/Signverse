using UnityEngine;

namespace Skills
{
    [CreateAssetMenu(fileName = "33-dd", menuName = "Game/Skills/33 DD Dash")]
    public class Skill33DDUltraDash : SkillBase
    {
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

        public override void OnActivate(GameObject user, PlayerCC controller)
        {
            if (controller.isClimbing)
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

        public override void OnUpdate(GameObject user, PlayerCC controller)
        {
            if (controller.isClimbing)
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
                OnActivate(user, controller);
            }
        }

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

        protected float EvaluateDashPosture(float normalizedTime)
        {
            if (dashPostureCurve == null || dashPostureCurve.length == 0)
            {
                return 1f;
            }

            return Mathf.Clamp01(dashPostureCurve.Evaluate(Mathf.Clamp01(normalizedTime)));
        }

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
