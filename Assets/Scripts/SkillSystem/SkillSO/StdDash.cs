using UnityEngine;

namespace Skills
{
    [CreateAssetMenu(fileName = "StdDash", menuName = "Game/Skills/Standard Dash")]
    public class StdDash : SkillBase
    {
        [Header("冲刺设置")]
        [SerializeField] protected KeyCode dashKey = KeyCode.L;
        [SerializeField] protected float dashDistance = 4f;
        [SerializeField] protected float dashDuration = 0.12f;
        [SerializeField] protected float cooldown = 0.4f;

        protected float cooldownTimer;
        protected float dashTimer;
        protected Vector3 dashVelocity;
        protected bool isDashing;

        protected virtual float DashDistance => dashDistance;
        protected virtual float DashDuration => dashDuration;
        protected virtual float DashCooldown => cooldown;

        public override void OnActivate(GameObject user, PlayerCC controller) { }

        public override void OnUpdate(GameObject user, PlayerCC controller)
        {
            if (cooldownTimer > 0f)
            {
                cooldownTimer -= Time.deltaTime;
            }

            if (isDashing)
            {
                UpdateDash(controller);
                return;
            }

            if (!Input.GetKeyDown(dashKey) || cooldownTimer > 0f)
            {
                return;
            }

            if (!TryGetDashDirection(controller, out Vector3 dashDirection))
            {
                return;
            }

            StartDash(controller, dashDirection);
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

            dashDirection = Vector3.zero;
            return false;
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

            CollisionFlags flags =
                controller.GetCharacterController().Move(dashVelocity * Time.deltaTime);

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
