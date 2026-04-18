using UnityEngine;

namespace Skills
{
    [CreateAssetMenu(fileName = "32-dj", menuName = "Game/Skills/32 DJ Eight Direction Dash")]
    public class Skill32DJEightDirectionDash : StdDash
    {
        protected override float DashCooldown => 0.8f;
        [SerializeField] [Range(0.1f, 2f)]
        private float diagonalBias = 0.7f; // 控制斜前方角度



        protected override bool TryGetDashDirection(PlayerCC controller, out Vector3 dashDirection)
        {
            Vector2 input = controller.GetMoveInput();
            // 判断是否是对角输入
            bool isDiagonal =Mathf.Abs(input.x) > 0.1f && Mathf.Abs(input.y) > 0.1f;
            if (isDiagonal)
            {

                // 压缩横向分量，让角度更靠前
                input.x *= diagonalBias;
            }

            dashDirection = new Vector3(input.x, input.y, 0f).normalized;//nromalize之后dashDirection永远为1，不会使得斜向冲刺变短
            return dashDirection.sqrMagnitude >= 0.01f;
        }
    }
}
