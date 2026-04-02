using UnityEngine;

namespace Skills
{
    [CreateAssetMenu(fileName = "12-mj", menuName = "Game/Skills/12 MJ Climb")]
    public class Skill12MJClimb : SkillBase
    {
        [Header("攀爬设置")]
        public float climbSpeed = 4f;
        public float climbRayLength = 0.8f;
        public float rayOffsetY = -0.8f;
        public LayerMask climbableMask;

        public override void OnActivate(GameObject user, PlayerCC controller) { }

        public override void OnUpdate(GameObject user, PlayerCC controller)
        {
            Vector2 input = controller.GetMoveInput();
            Vector3 rayOrigin = user.transform.position + Vector3.up * rayOffsetY;
            bool canClimb = Physics.Raycast(rayOrigin, controller.GetFacing(), climbRayLength, climbableMask);

            if (canClimb && (Mathf.Abs(input.y) > 0.1f || controller.IsJumpPressed()))
            {
                controller.isClimbing = true;
                float vertical = controller.IsJumpPressed() ? 1f : input.y;
                Vector3 climbDelta = new Vector3(0f, vertical * climbSpeed, 0f);
                controller.GetCharacterController().Move(climbDelta * Time.deltaTime);
                return;
            }

            controller.isClimbing = false;
        }
    }
}
