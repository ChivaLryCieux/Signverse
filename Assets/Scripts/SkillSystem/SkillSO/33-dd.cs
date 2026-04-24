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

        protected float cooldownTimer;
        protected float dashTimer;
        protected Vector3 dashVelocity;
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
                    StopDash();
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
            dashVelocity = normalizedDirection * (DashDistance / actualDuration);
            cooldownTimer = DashCooldown;

            if (Mathf.Abs(normalizedDirection.x) > 0.01f)
            {
                controller.SetFacing(normalizedDirection.x > 0f ? Vector3.right : Vector3.left);
            }
        }

        protected virtual void UpdateDash(PlayerCC controller)
        {
            dashTimer -= Time.deltaTime;

            CollisionFlags flags = controller.GetCharacterController().Move(dashVelocity * Time.deltaTime);

            if ((flags & (CollisionFlags.Sides | CollisionFlags.Above)) != 0 || dashTimer <= 0f)
            {
                StopDash();
            }
        }

        protected void StopDash()
        {
            isDashing = false;
            dashTimer = 0f;
            dashVelocity = Vector3.zero;
        }
    }
}
