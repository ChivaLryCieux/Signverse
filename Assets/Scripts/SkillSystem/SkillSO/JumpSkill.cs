using UnityEngine;

namespace Skills
{
    [CreateAssetMenu(fileName = "JumpSkill", menuName = "Game/Skills/Jump")]
    public class JumpSkill : SkillBase
    {
        public float jumpHeight = 2.2f;

        public override void OnActivate(GameObject user, PlayerCC controller)
        {
            float gravity = controller.gravity;
            float verticalVel = Mathf.Sqrt(jumpHeight * -2f * gravity);
            controller.SetVerticalVelocity(verticalVel);
        }
    }
}