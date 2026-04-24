using UnityEngine;

namespace Skills
{
    [CreateAssetMenu(fileName = "12-mj", menuName = "Game/Skills/12 MJ Climb")]
    public class Skill12MJClimb : Skill11MMCustomPortal
    {
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

        [Header("边缘翻越")]
        [Tooltip("攀爬到顶部时，向面朝方向和上方移动的距离，用于把角色送上平台。")]
        public Vector2 exitUpOffset = new Vector2(0.6f, 1.0f);
        [Tooltip("顶部翻越位移持续时间，避免瞬移感。")]
        public float exitUpDuration = 0.25f;

        private bool isExitingUp;
        private float exitUpTimer;
        private Vector3 exitUpVelocity;

        public override void OnActivate(GameObject user, PlayerCC controller) { }

        public override void OnUpdate(GameObject user, PlayerCC controller)
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

            if (!controller.isClimbing)
            {
                TryEnterClimb(controller, canClimb, vertical);

                if (!controller.isClimbing)
                {
                    base.OnUpdate(user, controller);
                }

                return;
            }

            UpdateClimb(controller, canClimb, vertical);

            if (!controller.isClimbing)
            {
                base.OnUpdate(user, controller);
            }
        }

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

        private void UpdateClimb(PlayerCC controller, bool canClimb, float vertical)
        {
            if (controller.IsInClimbTransitionTrigger && vertical > inputThreshold)
            {
                StartExitUp(controller);
                return;
            }

            if (controller.isGrounded && vertical <= 0f)
            {
                controller.SetClimbState(false, 0f);
                return;
            }

            if (!canClimb && vertical > inputThreshold)
            {
                StartExitUp(controller);
                return;
            }

            if (!canClimb && !controller.IsInClimbTransitionTrigger)
            {
                controller.SetClimbState(false, 0f);
                return;
            }

            controller.SetVerticalVelocity(0f);
            controller.SetClimbState(true, vertical);

            if (Mathf.Abs(vertical) > inputThreshold)
            {
                MoveClimb(controller, vertical);
            }
        }

        private void MoveClimb(PlayerCC controller, float vertical)
        {
            Vector3 climbDelta = new Vector3(0f, vertical * climbSpeed, 0f);
            controller.GetCharacterController().Move(climbDelta * Time.deltaTime);
        }

        private void DrawClimbRay(PlayerCC controller, Vector3 rayOrigin, bool canClimb)
        {
            if (!drawClimbRay)
            {
                return;
            }

            Color rayColor = canClimb ? climbRayHitColor : climbRayMissColor;
            Debug.DrawRay(rayOrigin, controller.GetFacing().normalized * climbRayLength, rayColor);
        }

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

        private void UpdateExitUp(PlayerCC controller)
        {
            float step = Mathf.Min(Time.deltaTime, exitUpTimer);

            controller.SetVerticalVelocity(0f);
            controller.SetClimbState(true, 0f);
            controller.GetCharacterController().Move(exitUpVelocity * step);

            exitUpTimer -= step;

            if (exitUpTimer > 0f)
            {
                return;
            }

            isExitingUp = false;
            exitUpTimer = 0f;
            exitUpVelocity = Vector3.zero;
            controller.SetClimbState(false, 0f);
        }
    }
}
