using UnityEngine;

namespace Skills
{
    [CreateAssetMenu(fileName = "MoveSkill", menuName = "Game/Skills/Move")]
    public class MoveSkill : SkillBase
    {
        [Header("基础移动")]
        public float moveSpeed = 6f;

        [Header("爬墙设置")]
        public float climbSpeed = 4f;
        public float climbRayLength = 0.8f;
        public LayerMask climbableMask;
        public float rayOffsetY = -0.8f; // 你之前的脚底偏移

        public override void OnActivate(GameObject user, PlayerCC controller) { }

        public override void OnUpdate(GameObject user, PlayerCC controller)
        {
            Vector2 input = controller.GetMoveInput();
            
            // 1. 水平移动处理
            Vector3 moveVec = new Vector3(input.x * moveSpeed, 0, 0);
            controller.GetCharacterController().Move(moveVec * Time.deltaTime);

            // 2. 爬墙射线检测
            Vector3 rayOrigin = user.transform.position + Vector3.up * rayOffsetY;
            bool canClimb = Physics.Raycast(rayOrigin, controller.GetFacing(), climbRayLength, climbableMask);

            // 3. 爬墙状态判定 (W/S 或 空格)
            // 检查空格键是否被按住（通过 controller 拿到输入）
            // 注意：这里需要确保 PlayerCC 暴露了输入类，或者在 MoveSkill 里判断
            bool jumpHeld = Input.GetKey(KeyCode.Space); // 临时方案，稍后可优化为 InputSystem

            if (canClimb && (Mathf.Abs(input.y) > 0.1f || jumpHeld))
            {
                controller.isClimbing = true;
                
                // 计算垂直速度：W/S 或 空格向上
                float vSpeed = input.y;
                if (jumpHeld) vSpeed = 1.0f; // 空格强制向上爬

                Vector3 climbVec = new Vector3(0, vSpeed * climbSpeed, 0);
                controller.GetCharacterController().Move(climbVec * Time.deltaTime);
            }
            else
            {
                controller.isClimbing = false;
            }

            // 4. 处理转向
            if (input.x > 0.01f) controller.SetFacing(Vector3.right);
            else if (input.x < -0.01f) controller.SetFacing(Vector3.left);
        }
    }
}