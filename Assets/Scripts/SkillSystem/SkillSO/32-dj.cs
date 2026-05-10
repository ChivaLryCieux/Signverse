using UnityEngine;

namespace Skills
{
    [CreateAssetMenu(fileName = "32-dj", menuName = "Game/Skills/32 DJ Eight Direction Dash")]
    public class Skill32DJEightDirectionDash : Skill30StdDash
    {
        [Header("八向冲刺")]
        [SerializeField] [Range(0.1f, 2f)]
        private float diagonalBias = 0.7f;

        protected override bool TryGetDashDirection(PlayerCC controller, out Vector3 dashDirection)
        {
            Vector2 input = controller.GetMoveInput();
            bool isDiagonal = Mathf.Abs(input.x) > 0.1f && Mathf.Abs(input.y) > 0.1f;
            if (isDiagonal)
            {
                input.x *= diagonalBias;
            }

            dashDirection = new Vector3(input.x, input.y, 0f).normalized;
            return dashDirection.sqrMagnitude >= 0.01f;
        }
    }
}
