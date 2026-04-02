using UnityEngine;

namespace Skills
{
    [CreateAssetMenu(fileName = "23-jd", menuName = "Game/Skills/23 JD Blink Jump")]
    public class Skill23JDBlinkJump : SkillBase
    {
        [Header("跃迁闪现")]
        public float jumpHeight = 2.5f;
        public float blinkDistance = 4f;

        public override void OnActivate(GameObject user, PlayerCC controller)
        {
            float verticalVel = Mathf.Sqrt(jumpHeight * -2f * controller.gravity);
            controller.SetVerticalVelocity(verticalVel);

            Vector3 dir = controller.GetFacing().sqrMagnitude > 0.01f ? controller.GetFacing() : Vector3.right;
            controller.GetCharacterController().Move(dir * blinkDistance);
        }
    }
}
