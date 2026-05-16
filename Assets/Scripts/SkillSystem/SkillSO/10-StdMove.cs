using UnityEngine;

namespace Skills
{
    [CreateAssetMenu(fileName = "10-StdMove", menuName = "Game/Skills/10 Standard Move")]
    public class Skill10StdMove : SkillBase
    {
        [Header("基础移动")]
        public float moveSpeed = 6f;

        public override void OnActivate(GameObject user, PlayerCC controller, PlayerCC.Posture posture) { }

        public override void OnUpdate(GameObject user, PlayerCC controller, PlayerCC.Posture posture)
        {
            Vector2 input = controller.GetMoveInput();
            float horizontal = input.x;

            if (Mathf.Abs(horizontal) <= 0.01f)
            {
                return;
            }

            Vector3 moveDelta = new Vector3(horizontal * moveSpeed, 0f, 0f) * Time.deltaTime;
            controller.MoveCharacter(moveDelta);
        }
    }
}
