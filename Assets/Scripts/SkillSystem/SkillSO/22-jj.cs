using UnityEngine;

namespace Skills
{
    [CreateAssetMenu(fileName = "22-jj", menuName = "Game/Skills/22 JJ Z Axis Jump")]
    public class Skill22JJZAxisJump : SkillBase
    {
        [Header("伪 Z 轴跳")]
        public float jumpHeight = 3f;
        [TextArea] public string note = "当前 PlayerCC 在 LateUpdate 会强制把 Z 锁定为 0，所以这里只做成更高的跳跃占位版。";

        public override void OnActivate(GameObject user, PlayerCC controller)
        {
            float verticalVel = Mathf.Sqrt(jumpHeight * -2f * controller.gravity);
            controller.SetVerticalVelocity(verticalVel);
        }
    }
}
