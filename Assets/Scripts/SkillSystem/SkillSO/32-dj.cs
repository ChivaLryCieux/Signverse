using UnityEngine;

namespace Skills
{
    [CreateAssetMenu(fileName = "32-dj", menuName = "Game/Skills/32 DJ Eight Direction Dash")]
    public class Skill32DJEightDirectionDash : StdDash
    {
        protected override float DashCooldown => 0.8f;

        protected override bool TryGetDashDirection(PlayerCC controller, out Vector3 dashDirection)
        {
            Vector2 input = controller.GetMoveInput();
            dashDirection = new Vector3(input.x, input.y, 0f);
            return dashDirection.sqrMagnitude >= 0.01f;
        }
    }
}
