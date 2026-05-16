using UnityEngine;

namespace Skills
{
    [CreateAssetMenu(fileName = "33-dd", menuName = "Game/Skills/33 DD Dash")]
    public class Skill33DDUltraDash : SkillBase
    {
        // ===== 元数据 =====

        // ===== 物理控制 =====
        [Header("超级冲刺")]
        [SerializeField] protected float dashSpeed = 18f;
        [SerializeField] protected float cooldown = 1.25f;

        protected float cooldownTimer;
        protected Vector3 dashDirection;
        protected bool isDashing;

        protected virtual float DashSpeed => dashSpeed;
        protected virtual float DashCooldown => cooldown;
        protected virtual bool UsesUltraDashAnimation => true;

        // 满足姿态和冷却条件时开始持续冲刺。
        public override void OnActivate(GameObject user, PlayerCC controller, PlayerCC.Posture posture)
        {
            if (posture == PlayerCC.Posture.Climbing)
            {
                return;
            }

            if (isDashing)
            {
                StopDash(controller);
                return;
            }

            if (cooldownTimer > 0f)
            {
                return;
            }

            if (!TryGetDashDirection(controller, out Vector3 dashDirection))
            {
                return;
            }

            StartDash(controller, dashDirection);
        }

        // 每帧处理冲刺冷却、持续冲刺推进和按键切换。
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

            if (controller.WasDashPressed())
            {
                OnActivate(user, controller, posture);
                return;
            }

            if (isDashing)
            {
                UpdateDash(controller);
            }
        }

        // 超级冲刺始终沿角色当前朝向前进。
        protected virtual bool TryGetDashDirection(PlayerCC controller, out Vector3 dashDirection)
        {
            dashDirection = controller.GetFacing().sqrMagnitude > 0.01f ? controller.GetFacing() : Vector3.right;
            return true;
        }

        // 初始化冲刺方向和动画姿态值。
        protected void StartDash(PlayerCC controller, Vector3 dashDirection)
        {
            Vector3 normalizedDirection = dashDirection.normalized;
            ResetFallDeathHeight(controller);

            isDashing = true;
            this.dashDirection = normalizedDirection;
            controller.SetVerticalVelocity(0f);
            controller.RequestGravitySuppressed();
            controller.SetDashPosture(1f);
            controller.SetUltraDashActive(UsesUltraDashAnimation);

            if (Mathf.Abs(normalizedDirection.x) > 0.01f)
            {
                controller.SetFacing(normalizedDirection.x > 0f ? Vector3.right : Vector3.left);
            }
        }

        // 持续向前冲刺，并在冲刺期间关闭重力。
        protected virtual void UpdateDash(PlayerCC controller)
        {
            controller.SetVerticalVelocity(0f);
            controller.RequestGravitySuppressed();
            controller.SetDashPosture(1f);
            controller.SetUltraDashActive(UsesUltraDashAnimation);

            Vector3 dashDelta = dashDirection * DashSpeed * Time.deltaTime;
            CollisionFlags flags = controller.MoveCharacter(dashDelta);

            if ((flags & (CollisionFlags.Sides | CollisionFlags.Above)) != 0)
            {
                StopDash(controller);
            }
        }

        // 停止冲刺并重置冲刺运行时状态。
        protected void StopDash(PlayerCC controller)
        {
            isDashing = false;
            dashDirection = Vector3.zero;
            cooldownTimer = DashCooldown;

            if (controller != null)
            {
                controller.SetDashPosture(0f);
                controller.SetUltraDashActive(false);
            }
        }

        protected void ResetFallDeathHeight(PlayerCC controller)
        {
            if (controller == null)
            {
                return;
            }

            PlayerDeath death = controller.GetComponent<PlayerDeath>();
            if (death != null)
            {
                death.ResetFallDeathHeight();
            }
        }
    }
}
