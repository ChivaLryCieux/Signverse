using UnityEngine;

namespace Skills

{
    [CreateAssetMenu(fileName = "12-mj", menuName = "Game/Skills/12 MJ Climb")]
    public class Skill12MJClimb : SkillBase
    {
        // ===== 元数据 =====

        // ===== 物理控制 =====

        [Header("基础移动")]
        public float moveSpeed = 6f;

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
        // [Header("Exit 瞬间位移脉冲, x=向前，y=向上")]
        // public Vector2 exitImpulse = new Vector2(0.6f, 0.8f); // x=向前，y=向上

        // [Header("边缘翻越")]
        // [Tooltip("翻越结束时的前移距离。y 已不使用，胶囊底部会直接放到 Trigger 顶部。")]
        // public Vector2 exitUpOffset = new Vector2(0.6f, 1.0f);
        // [Tooltip("顶部翻越动画播放多久后，把玩家胶囊放到 Trigger 顶部。")]
        // public float exitUpDuration = 3.1f;
        // [Tooltip("顶部翻越触发冷却，避免落到平台后仍在 Trigger 内导致重复播放翻越动画。")]
        // public float exitUpCooldown = 5f;
        // [Tooltip("翻越结束后向下贴地的距离，防止脚本位移或动画位移让胶囊略微悬空。")]
        // public float exitUpGroundSnapDistance = 1.2f;
        // public bool debugClimbLogs = false;



        

        private bool isExitingUp;
        private float exitUpTimer;
        private float nextExitUpAllowedTime;
        private Vector3 exitUpTopPosition;

        

        // 攀爬是持续检测型技能，激活时不需要额外处理。
        public override void OnActivate(GameObject user, PlayerCC controller, PlayerCC.Posture posture) { }

        // 每帧检测可攀爬面、输入和当前姿态，调度进入/维持/退出攀爬。
        public override void OnUpdate(GameObject user, PlayerCC controller, PlayerCC.Posture posture)
        {
            
            Animator animator = user.GetComponentInChildren<Animator>();

            if (animator != null && animator.GetBool("Climb_Exit_Up"))
            {
                controller.SetClimbState(false, 0f);   // ❗退出攀爬
                controller.SetVerticalVelocity(0f);    // 可选（防止抖动）

                return;
            }
            // if (isExitingUp)
            // {
            //     UpdateExitUp(controller);
            //     return;
            // }

            Vector2 input = controller.GetMoveInput();
            Vector3 rayOrigin = user.transform.position + Vector3.up * rayOffsetY;
            bool canClimbByRay = climbableMask.value != 0 && Physics.Raycast(rayOrigin, controller.GetFacing(), climbRayLength, climbableMask);
            bool canClimb = canClimbByRay || controller.IsInClimbVolume;
            float vertical = Mathf.Abs(input.y) > inputThreshold ? input.y : 0f;

            DebugClimb(controller, $"Update posture={posture}, vertical={vertical:F2}, canClimbByRay={canClimbByRay}, isInClimbVolume={controller.IsInClimbVolume}, canClimb={canClimb}, activeTrigger={(controller.ActiveClimbTransitionTrigger != null ? controller.ActiveClimbTransitionTrigger.name : "null")}");
            DrawClimbRay(controller, rayOrigin, canClimb);
            if (posture != PlayerCC.Posture.Climbing)
            {
                TryEnterClimb(controller, canClimb, vertical);
                posture = controller.CurrentPosture;

                if (posture != PlayerCC.Posture.Climbing)
                {
                    MoveHorizontal(controller);
                }

                return;
            }

            UpdateClimb(controller, canClimb, vertical, posture);

            posture = controller.CurrentPosture;

            if (posture != PlayerCC.Posture.Climbing)
            {
                MoveHorizontal(controller);
            }
        }

        private void MoveHorizontal(PlayerCC controller)
        {
            Vector2 input = controller.GetMoveInput();
            float horizontal = input.x;

            if (Mathf.Abs(horizontal) <= 0.01f)
            {
                return;
            }

            Vector3 moveDelta = new Vector3(horizontal * moveSpeed, 0f, 0f) * Time.deltaTime;
            controller.MoveWithGroundProtection(moveDelta);
        }

        // 判断是否满足从地面或边缘进入攀爬的条件。
        private void TryEnterClimb(PlayerCC controller, bool canClimb, float vertical)
        {
            bool enteringFromGround = canClimb && vertical > inputThreshold;
            bool enteringFromLedge = controller.IsInClimbTransitionTrigger && vertical < -inputThreshold;
            DebugClimb(controller, $"TryEnterClimb canClimb={canClimb}, vertical={vertical:F2}, enteringFromGround={enteringFromGround}, enteringFromLedge={enteringFromLedge}");

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
                DebugClimb(controller, "StopClimb: wants down and ground below.");
                StopClimb(controller);
                return;
            }

            if (controller.CanAutoExitUpFromActiveClimbTrigger())
            {
                // TryStartExitUp(controller, "near climb trigger top");
                return;
            }

            if (!canClimb && !controller.IsInClimbTransitionTrigger)
            {
                DebugClimb(controller, "StopClimb: no climb surface and not in trigger.");
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
        // private void TryStartExitUp(PlayerCC controller, string reason)
        // {
        //     if (Time.time < nextExitUpAllowedTime)
        //     {
        //         DebugClimb(controller, $"Skip StartExitUp: cooldown active. reason={reason}, remaining={nextExitUpAllowedTime - Time.time:F2}s");
        //         return;
        //     }

        //     DebugClimb(controller, $"StartExitUp: {reason}.");
        //     StartExitUp(controller);
        // }

        // private void StartExitUp(PlayerCC controller)
        // {
        //     // nextExitUpAllowedTime = Time.time + Mathf.Max(0f, exitUpCooldown);
        //     // Vector3 facing = controller.GetFacing().sqrMagnitude > 0.01f ? controller.GetFacing().normalized : Vector3.right;
        //     // float duration = Mathf.Max(0.01f, exitUpDuration);

        //     controller.RequestClimbExitUpAnimation();
        //     controller.SetVerticalVelocity(0f);
        //     controller.SetClimbState(true, 0f);
        //     ClimbTransitionTrigger activeTrigger = controller.ActiveClimbTransitionTrigger;
        //     // exitUpTopPosition = activeTrigger != null
        //     //     ? activeTrigger.GetExitTopPosition(controller, exitUpOffset.x)
        //     //     : controller.transform.position + new Vector3(facing.x * exitUpOffset.x, exitUpOffset.y, 0f);

        //     // isExitingUp = true;
        //     // exitUpTimer = duration;
        //     // DebugClimb(controller, $"ExitUp started. duration={duration:F2}, topPosition={exitUpTopPosition}");
        // }

        // // 顶部翻越期间只等待动画播放，胶囊在结束时一次性放到 Trigger 顶部。
        // private void UpdateExitUp(PlayerCC controller)
        // {
        //     float step = Mathf.Min(Time.deltaTime, exitUpTimer);

        //     controller.SetVerticalVelocity(0f);
        //     controller.SetClimbState(true, 0f);

        //     exitUpTimer -= step;

        //     if (exitUpTimer > 0f)
        //     {
        //         return;
        //     }

        //     DebugClimb(controller, "FinishExitUp: timer completed.");
        //     FinishExitUp(controller);
        // }

        // // 结束顶部翻越，清理翻越状态并退出攀爬。
        // private void FinishExitUp(PlayerCC controller)
        // {
        //     // controller.PlaceCapsuleBottomAt(exitUpTopPosition);
        //     // Vector3 facing = controller.GetFacing().sqrMagnitude > 0.01f 
        //     // ? controller.GetFacing().normalized 
        //     // : Vector3.right;

        //     // Vector3 impulse = new Vector3(facing.x * exitImpulse.x, exitImpulse.y, 0f);

        //     // controller.GetCharacterController().Move(impulse);


        //     isExitingUp = false;
        //     exitUpTimer = 0f;
        //     exitUpTopPosition = Vector3.zero;
        //     controller.RequestGravitySuppressed();
        //     StopClimb(controller);
        // }

        private void DebugClimb(PlayerCC controller, string message)
        {
            // if (!debugClimbLogs)
            // {
            //     return;
            // }

            Debug.Log($"[Skill12MJClimb] {message}", controller);
        }

        // 统一关闭 PlayerCC 的攀爬状态。
        private void StopClimb(PlayerCC controller)
        {
            controller.SetClimbState(false, 0f);
        }
    }
}
