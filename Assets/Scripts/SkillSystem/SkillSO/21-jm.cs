using UnityEngine;

namespace Skills
{
    [CreateAssetMenu(fileName = "21-jm", menuName = "Game/Skills/21 JM Long Jump")]
    public class Skill21JMStandingLongJump : SkillBase
    {
        [Header("蓄力跳远")]
        public float minChargeTime = 0.4f;
        public float minJumpDistance = 2f;
        public float maxJumpDistance = 15f;
        public float jumpHeight = 2f;
        public float maxChargeTime = 1f;

        private float chargeTimer;
        private bool isCharging;
        private float airForwardSpeed;
        private Vector3 moveDirection = Vector3.zero;

        public override void OnActivate(GameObject user, PlayerCC controller, PlayerCC.Posture posture)
        {
            if (posture != PlayerCC.Posture.Grounded)
            {
                return;
            }

            isCharging = true;
            chargeTimer = 0f;
            airForwardSpeed = 0f;
        }

        public override void OnUpdate(GameObject user, PlayerCC controller, PlayerCC.Posture posture)
        {
            if (!isCharging && controller.WasJumpPressed())
            {
                OnActivate(user, controller, posture);
            }

            if (isCharging)
            {
                UpdateCharge(controller);
                return;
            }

            if (posture == PlayerCC.Posture.Airborne && airForwardSpeed > 0.1f)
            {
                MoveInAir(user, controller, posture);
                return;
            }

            if (posture == PlayerCC.Posture.Grounded)
            {
                airForwardSpeed = 0f;
                moveDirection = Vector3.zero;
            }
        }

        private void UpdateCharge(PlayerCC controller)
        {
            if (controller.IsJumpPressed())
            {
                chargeTimer = Mathf.Min(chargeTimer + Time.deltaTime, maxChargeTime);
                return;
            }

            if (chargeTimer >= minChargeTime)
            {
                Launch(controller);
            }

            isCharging = false;
        }

        private void Launch(PlayerCC controller)
        {
            float usableChargeRange = Mathf.Max(0.01f, maxChargeTime - minChargeTime);
            float power = Mathf.Clamp01((chargeTimer - minChargeTime) / usableChargeRange);

            moveDirection = controller.GetFacing();
            if (moveDirection.sqrMagnitude < 0.01f)
            {
                moveDirection = Vector3.right;
            }

            float verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * controller.gravity);
            controller.SetVerticalVelocity(verticalVelocity);
            airForwardSpeed = Mathf.Lerp(minJumpDistance, maxJumpDistance, power);
        }

        private void MoveInAir(GameObject user, PlayerCC controller, PlayerCC.Posture posture)
        {
            if (moveDirection.sqrMagnitude < 0.01f)
            {
                moveDirection = controller.GetFacing().sqrMagnitude > 0.01f ? controller.GetFacing() : Vector3.right;
            }

            Vector3 moveDelta = moveDirection.normalized * airForwardSpeed * Time.deltaTime;
            CollisionFlags collisionFlags = controller.GetCharacterController().Move(moveDelta);
            Debug.DrawRay(user.transform.position, moveDelta * 10f, Color.red);

            if ((collisionFlags & CollisionFlags.Sides) != 0 || posture == PlayerCC.Posture.Climbing)
            {
                airForwardSpeed = 0f;
            }
        }
    }
}
