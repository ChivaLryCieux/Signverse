using UnityEngine;

namespace Skills
{
    [CreateAssetMenu(fileName = "32-dj", menuName = "Game/Skills/32 DJ Eight Direction Dash")]
    public class Skill32DJEightDirectionDash : SkillBase
    {
        // ===== 元数据 =====

        // ===== 物理控制 =====
        [Header("八向冲刺")]
        [SerializeField] private float dashDistance = 9f;
        [SerializeField] private float dashDuration = 0.12f;
        [SerializeField] private float cooldown = 0.8f;
        [SerializeField] private float fallDeathGraceAfterDash = 2f;
        [SerializeField] [Range(0.1f, 2f)]
        private float diagonalBias = 0.7f; // 控制斜前方角度

        [SerializeField] private AnimationCurve dashPostureCurve = new AnimationCurve(
            new Keyframe(0f, 0f),
            new Keyframe(0.25f, 1f),
            new Keyframe(1f, 0f)
        );
        [SerializeField] [Min(1)]
        private int dashCurveSamples = 20;

        private float cooldownTimer;
        private float dashTimer;
        private float dashElapsed;
        private float dashCurveArea = 1f;
        private Vector3 dashDirection;
        private bool isDashing;

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

            if (!TryGetDashDirection(controller, out Vector3 direction))
            {
                return;
            }

            StartDash(controller, direction);
        }

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

        private bool TryGetDashDirection(PlayerCC controller, out Vector3 dashDirection)
        {
            Vector2 input = controller.GetMoveInput();
            // 判断是否是对角输入
            bool isDiagonal = Mathf.Abs(input.x) > 0.1f && Mathf.Abs(input.y) > 0.1f;
            if (isDiagonal)
            {

                // 压缩横向分量，让角度更靠前
                input.x *= diagonalBias;
            }

            dashDirection = new Vector3(input.x, input.y, 0f).normalized;//nromalize之后dashDirection永远为1，不会使得斜向冲刺变短
            return dashDirection.sqrMagnitude >= 0.01f;
        }

        private void StartDash(PlayerCC controller, Vector3 direction)
        {
            float actualDuration = Mathf.Max(0.01f, dashDuration);
            Vector3 normalizedDirection = direction.normalized;

            isDashing = true;
            dashTimer = actualDuration;
            dashElapsed = 0f;
            dashDirection = normalizedDirection;
            dashCurveArea = Mathf.Max(0.01f, CalculateCurveArea(dashPostureCurve, dashCurveSamples));
            cooldownTimer = cooldown;
            controller.SetDashPosture(EvaluateDashPosture(0f));
            controller.SetUltraDashActive(false);

            if (Mathf.Abs(normalizedDirection.x) > 0.01f)
            {
                controller.SetFacing(normalizedDirection.x > 0f ? Vector3.right : Vector3.left);
            }
        }

        private void UpdateDash(PlayerCC controller)
        {
            float actualDuration = Mathf.Max(0.01f, dashDuration);
            float step = Mathf.Min(Time.deltaTime, dashTimer);
            dashElapsed += step;
            dashTimer -= step;

            float normalizedTime = Mathf.Clamp01(dashElapsed / actualDuration);
            float posture = EvaluateDashPosture(normalizedTime);
            controller.SetDashPosture(posture);
            controller.SetUltraDashActive(false);

            Vector3 dashDelta = dashDirection * (dashDistance * posture / (dashCurveArea * actualDuration)) * step;
            CollisionFlags flags = controller.MoveWithGroundProtection(dashDelta);

            if ((flags & (CollisionFlags.Sides | CollisionFlags.Above)) != 0 || dashTimer <= 0f)
            {
                StopDash(controller);
            }
        }

        private void StopDash(PlayerCC controller)
        {
            isDashing = false;
            dashTimer = 0f;
            dashElapsed = 0f;
            dashDirection = Vector3.zero;

            if (controller != null)
            {
                controller.SetDashPosture(0f);
                controller.SetUltraDashActive(false);
                GrantFallDeathGrace(controller);
            }
        }

        private void GrantFallDeathGrace(PlayerCC controller)
        {
            if (fallDeathGraceAfterDash <= 0f || controller == null)
            {
                return;
            }

            PlayerDeath death = controller.GetComponent<PlayerDeath>();
            if (death != null)
            {
                death.GrantInvincibility(fallDeathGraceAfterDash);
            }
        }

        private float EvaluateDashPosture(float normalizedTime)
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

        // ===== 动画控制 =====
    }
}
