using UnityEngine;

namespace Skills
{
    [CreateAssetMenu(fileName = "12-mj", menuName = "Game/Skills/12 MJ Climb")]
    public class Skill12MJClimb : Skill11MMCustomPortal
    {
        // ===== 元数据 =====

        // ===== 物理控制 =====
        [Header("攀爬设置")]
        public float climbSpeed = 4f;
        public float climbRayLength = 0.8f;
        public float rayOffsetY = -0.8f;
        public LayerMask climbableMask;
        public float inputThreshold = 0.1f;

        [Header("射线调试")]
        public bool drawClimbRay = true;
        public Color climbRayHitColor = Color.green;
        public Color climbRayMissColor = Color.red;

        [Header("底部退出检测")]
        public LayerMask groundMask;
        public float groundExitCheckDistance = 0.25f;
        public float groundExitCheckRadius = 0.18f;
        public bool drawGroundExitCheck = true;
        public Color groundExitHitColor = Color.cyan;
        public Color groundExitMissColor = Color.yellow;

        [Header("边缘翻越")]
        [Tooltip("攀爬到顶部时，向面朝方向和上方移动的距离，用于把角色送上平台。")]
        public Vector2 exitUpOffset = new Vector2(0.6f, 1.0f);
        [Tooltip("顶部翻越位移持续时间，避免瞬移感。")]
        public float exitUpDuration = 0.25f;

        private bool isExitingUp;
        private float exitUpTimer;
        private Vector3 exitUpVelocity;

        // 攀爬是持续检测型技能，激活时不需要额外处理。
        public override void OnActivate(GameObject user, PlayerCC controller, PlayerCC.Posture posture) { }

        // 每帧检测可攀爬面、输入和当前姿态，调度进入/维持/退出攀爬。
        public override void OnUpdate(GameObject user, PlayerCC controller, PlayerCC.Posture posture)
        {
            if (isExitingUp)
            {
                UpdateExitUp(controller);
                return;
            }

            Vector2 input = controller.GetMoveInput();
            Vector3 rayOrigin = user.transform.position + Vector3.up * rayOffsetY;
            bool canClimb = Physics.Raycast(rayOrigin, controller.GetFacing(), climbRayLength, climbableMask);
            DrawClimbRay(controller, rayOrigin, canClimb);
            float vertical = Mathf.Abs(input.y) > inputThreshold ? input.y : 0f;
            if (posture != PlayerCC.Posture.Climbing)
            {
                TryEnterClimb(controller, canClimb, vertical);
                posture = controller.CurrentPosture;

                if (posture != PlayerCC.Posture.Climbing)
                {
                    base.OnUpdate(user, controller, posture);
                }

                return;
            }

            UpdateClimb(controller, canClimb, vertical, posture);

            posture = controller.CurrentPosture;

            if (posture != PlayerCC.Posture.Climbing)
            {
                base.OnUpdate(user, controller, posture);
            }
        }

        // 判断是否满足从地面或边缘进入攀爬的条件。
        private void TryEnterClimb(PlayerCC controller, bool canClimb, float vertical)
        {
            bool enteringFromGround = canClimb && vertical > inputThreshold;
            bool enteringFromLedge = controller.IsInClimbTransitionTrigger && vertical < -inputThreshold;

            if (!enteringFromGround && !enteringFromLedge)
            {
                controller.SetClimbState(false, 0f);
                return;
            }

            if (enteringFromLedge)
            {
                controller.RequestClimbExitDownAnimation();
            }

            controller.SetVerticalVelocity(0f);
            controller.SetClimbState(true, vertical);
            MoveClimb(controller, vertical);
        }

        // 处理攀爬中的移动、底部退出和顶部翻越。
        private void UpdateClimb(PlayerCC controller, bool canClimb, float vertical, PlayerCC.Posture posture)
        {
            bool wantsToClimbDown = vertical < -inputThreshold;
            bool canExitToGround = posture == PlayerCC.Posture.Grounded || IsGroundBelow(controller);

            if (canExitToGround && wantsToClimbDown)
            {
                StopClimb(controller);
                return;
            }

            if (controller.IsInClimbTransitionTrigger && vertical > inputThreshold)
            {
                StartExitUp(controller);
                return;
            }

            if (!canClimb && vertical > inputThreshold)
            {
                StartExitUp(controller);
                return;
            }

            if (!canClimb && !controller.IsInClimbTransitionTrigger)
            {
                StopClimb(controller);
                return;
            }

            controller.SetVerticalVelocity(0f);
            controller.SetClimbState(true, vertical);

            if (Mathf.Abs(vertical) > inputThreshold)
            {
                MoveClimb(controller, vertical);

                if (vertical < -inputThreshold && IsGroundBelow(controller))
                {
                    StopClimb(controller);
                }
            }
        }

        // 根据竖直输入沿 Y 轴移动角色。
        private void MoveClimb(PlayerCC controller, float vertical)
        {
            Vector3 climbDelta = new Vector3(0f, vertical * climbSpeed, 0f);
            controller.GetCharacterController().Move(climbDelta * Time.deltaTime);
        }

        // 在 Scene 视图绘制前方攀爬检测射线。
        private void DrawClimbRay(PlayerCC controller, Vector3 rayOrigin, bool canClimb)
        {
            if (!drawClimbRay)
            {
                return;
            }

            Color rayColor = canClimb ? climbRayHitColor : climbRayMissColor;
            Debug.DrawRay(rayOrigin, controller.GetFacing().normalized * climbRayLength, rayColor);
        }

        // 检测角色脚下是否有可落地地面。
        private bool IsGroundBelow(PlayerCC controller)
        {
            CharacterController characterController = controller.GetCharacterController();
            Vector3 origin = GetGroundExitCheckOrigin(characterController);
            float checkDistance = Mathf.Max(0.01f, groundExitCheckDistance);
            float checkRadius = Mathf.Max(0.01f, groundExitCheckRadius);
            bool hitGround = Physics.SphereCast(origin, checkRadius, Vector3.down, out _, checkDistance, groundMask);

            DrawGroundExitCheck(origin, checkRadius, checkDistance, hitGround);
            return hitGround;
        }

        // 计算底部落地检测的起点。
        private Vector3 GetGroundExitCheckOrigin(CharacterController characterController)
        {
            Vector3 worldCenter = characterController.transform.TransformPoint(characterController.center);
            float bottomOffset = Mathf.Max(0f, characterController.height * 0.5f - characterController.radius);
            return worldCenter + Vector3.down * bottomOffset + Vector3.up * 0.05f;
        }

        // 在 Scene 视图绘制底部落地检测范围。
        private void DrawGroundExitCheck(Vector3 origin, float radius, float distance, bool hitGround)
        {
            if (!drawGroundExitCheck)
            {
                return;
            }

            Color color = hitGround ? groundExitHitColor : groundExitMissColor;
            Debug.DrawRay(origin, Vector3.down * distance, color);
            Debug.DrawRay(origin + Vector3.right * radius, Vector3.down * distance, color);
            Debug.DrawRay(origin + Vector3.left * radius, Vector3.down * distance, color);
            Debug.DrawRay(origin + Vector3.forward * radius, Vector3.down * distance, color);
            Debug.DrawRay(origin + Vector3.back * radius, Vector3.down * distance, color);
        }

        // ===== 动画控制 =====

        // 开始顶部翻越流程，并请求对应动画。
        private void StartExitUp(PlayerCC controller)
        {
            Vector3 facing = controller.GetFacing().sqrMagnitude > 0.01f ? controller.GetFacing().normalized : Vector3.right;
            Vector3 exitDelta = new Vector3(facing.x * exitUpOffset.x, exitUpOffset.y, 0f);
            float duration = Mathf.Max(0.01f, exitUpDuration);

            controller.RequestClimbExitUpAnimation();
            controller.SetVerticalVelocity(0f);
            controller.SetClimbState(true, 0f);

            isExitingUp = true;
            exitUpTimer = duration;
            exitUpVelocity = exitDelta / duration;
        }

        // 顶部翻越期间逐帧推动角色到平台上。
        private void UpdateExitUp(PlayerCC controller)
        {
            if (controller.CurrentPosture == PlayerCC.Posture.Grounded)
            {
                FinishExitUp(controller);
                return;
            }

            float step = Mathf.Min(Time.deltaTime, exitUpTimer);

            controller.SetVerticalVelocity(0f);
            controller.SetClimbState(true, 0f);
            controller.GetCharacterController().Move(exitUpVelocity * step);

            exitUpTimer -= step;

            if (exitUpTimer > 0f)
            {
                return;
            }

            FinishExitUp(controller);
        }

        // 结束顶部翻越，清理翻越状态并退出攀爬。
        private void FinishExitUp(PlayerCC controller)
        {
            isExitingUp = false;
            exitUpTimer = 0f;
            exitUpVelocity = Vector3.zero;
            StopClimb(controller);
        }

        // 统一关闭 PlayerCC 的攀爬状态。
        private void StopClimb(PlayerCC controller)
        {
            controller.SetClimbState(false, 0f);
        }
    }
}
