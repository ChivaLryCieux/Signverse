using UnityEngine;

namespace Skills
{
    [CreateAssetMenu(fileName = "LongJumpSkill", menuName = "Game/Skills/Long Jump")]
    public class LongJumpSkill : SkillBase
    {
        [Header("蓄力与距离设置")]
        public float minChargeTime = 0.4f;
        public float minJumpDistance = 4f;   
        public float maxJumpDistance = 15f;  
        public float jumpHeight = 3.5f;      
        public float maxChargeTime = 1.5f;   

        [Header("运行时内部状态")]
        private float chargeTimer = 0f;
        private bool isCharging = false;
        
        // 核心：这两个变量必须在类级别，才能跨帧保存
        private float airForwardSpeed = 0f; 
        private Vector3 moveDirection = Vector3.zero; 

        public override void OnActivate(GameObject user, PlayerCC controller)
        {
            if (controller.isGrounded)
            {
                isCharging = true;
                chargeTimer = 0f;
                airForwardSpeed = 0f; 
                Debug.Log("<color=yellow>技能激活：进入蓄力状态</color>");
            }
        }

        public override void OnUpdate(GameObject user, PlayerCC controller)
        {
            // 1. 蓄力逻辑
            if (isCharging)
            {
                if (controller.IsJumpPressed())
                {
                    chargeTimer += Time.deltaTime;
                    chargeTimer = Mathf.Min(chargeTimer, maxChargeTime);
                }
                else
                {
                    if (chargeTimer >= minChargeTime)
                    {
                        Launch(controller);
                    }
                    else
                    {
                        Debug.Log("<color=orange>蓄力不足，立定跳远未触发</color>");
                    }

                    isCharging = false;
                }
                return; // 蓄力时不需要执行位移
            }

            // 2. 核心修复：空中的水平位移 (X轴)
            if (!controller.isGrounded && airForwardSpeed > 0.1f)
            {
                // 确保方向不是 Vector3.zero
                if (moveDirection == Vector3.zero) moveDirection = controller.GetFacing();

                // 计算本帧位移量
                Vector3 moveDelta = moveDirection * airForwardSpeed * Time.deltaTime;
                
                // 执行位移；一旦撞到侧面，立刻停止水平惯性，让角色自然下落
                CollisionFlags collisionFlags = controller.GetCharacterController().Move(moveDelta);

                // 调试射线：在 Scene 窗口看红色的水平线，代表你的惯性
                Debug.DrawRay(user.transform.position, moveDelta * 10f, Color.red);
                
                // 碰撞侧墙或进入攀爬状态时，只取消水平推进，不影响重力下落
                if ((collisionFlags & CollisionFlags.Sides) != 0 || controller.isClimbing)
                {
                    airForwardSpeed = 0f;
                }
            }

            // 3. 落地重置一切
            if (controller.isGrounded)
            {
                airForwardSpeed = 0;
                moveDirection = Vector3.zero;
            }
        }

        private void Launch(PlayerCC controller)
        {
            float usableChargeRange = Mathf.Max(0.01f, maxChargeTime - minChargeTime);
            float power = Mathf.Clamp01((chargeTimer - minChargeTime) / usableChargeRange);

            // 1. 锁定方向：绝对不能是 zero
            moveDirection = controller.GetFacing();
            if (moveDirection.sqrMagnitude < 0.01f) 
                moveDirection = Vector3.right; // 保底方向

            // 2. 设置 Y 轴速度 (交给 PlayerCC 处理重力)
            float vVel = Mathf.Sqrt(jumpHeight * -2f * controller.gravity);
            controller.SetVerticalVelocity(vVel);

            // 3. 设置 X 轴速度 (在 OnUpdate 里持续应用)
            airForwardSpeed = Mathf.Lerp(minJumpDistance, maxJumpDistance, power);

            Debug.Log($"<color=cyan>发射成功！</color> 方向:{moveDirection} 速度:{airForwardSpeed} 蓄力:{chargeTimer:F2}s");
        }
    }
}
