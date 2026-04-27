using System.Collections.Generic;
using Skills;
using UnityEngine;
using UnityEngine.InputSystem;

public class AnimatorStateDebugger : MonoBehaviour
{
    
    [Header("Animator")]
    public Animator animator;

    // Lry的修改：正式动画同步模式的数据源引用。PlayerCC 是技能系统与物理控制的状态聚合点，动画层应从这里读取权威状态，避免重复实现状态机判定。
    [Header("Lry的修改：PlayerCC 状态同步")]
    public PlayerCC controller;

    // Lry的修改：直接查看/判断的“已装备技能数组”。数据源来自 PlayerCC.equippedSkills，是 UI 装备槽同步后的技能 loadout。
    // Lry的修改：用于让 AnimatorStateDebugger 能看到技能上下文；注意它不等于“当前正在触发的动作”。
    [Header("动画侧已装备技能视图")]
    public List<SkillBase> equippedSkills = new List<SkillBase>();
    public string skillID;
    // Lry的修改：开启后，本脚本会使用 PlayerCC -> Animator 的正式同步链路；关闭后保留原有 AnimatorStateDebugger 的纯输入调试链路。
    public bool usePlayerCCState = true;

    // Lry的修改：保留 Run 由运动输入驱动的行为，但输入来源改为 PlayerCC.GetMoveInput()，这样会尊重技能层的输入锁与姿态约束。
    public bool syncRunFromPlayerInput = true;

    [Header("Input System Class")]
    public PlayerControls inputActions; // 你的输入系统生成类

    Vector2 move;
    float jumpAxis;
    bool dashPressed;
    bool hidePressed;

    // Lry的修改：缓存 Animator 参数是否存在。专业术语上这是 parameter capability detection，用于降低 Animator Controller 迭代时的运行时耦合。
    bool hasRun;
    bool hasClimb;
    bool hasClimbVel;
    bool hasClimbInput;
    bool hasClimbExitUp;
    bool hasClimbExitDown;
    bool hasVerticalVelocity;
    bool hasJump;
    bool hasJumpType;
    bool hasIsGrounded;
    bool hasDashPosture;
    bool hasDash;
    bool hasHide;

    [Header("环境检测")]
    public float groundDetect = 0.5f;

    [Header("状态检测判断")]
    public bool isGrounded = true;
    public bool isMidAir = false;
    public bool isClimbing = false;


    public enum Locomotion
    {
        isGrounded,
        isMidAir,
        isClimbing
    }

    public Locomotion locomotion;

    [Header("用于快速切换攀爬状态")]
    public bool enterClimbing = false;

    
    [Header("Jump Settings")]
    public float jumpChangeRate = 5f;

    float jumpHold = 0f;
    bool jumpRising = false;
    bool jumpFalling = false;

    // [Header("动画补丁引用部分")]
    // public CharacterController cc;
    // public Transform player;
    // public Transform targetPos;

    void Awake()
    {
        inputActions = new PlayerControls();

        // Lry的修改：自动补齐 Animator 引用，支持脚本挂在带 Animator 的角色模型节点上，减少 Inspector 手动绑定错误。
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        // Lry的修改：自动向父级查找 PlayerCC，因为当前项目里 PlayerCC 通常挂在 Player 根节点，Animator 在子模型节点上。
        if (controller == null)
        {
            controller = GetComponentInParent<PlayerCC>();
        }

        // Lry的修改：启动时缓存参数表，后续 SetParameter 走存在性检查，避免 Animator Controller 参数名未统一时直接抛异常。
        CacheAnimatorParameters();
    }

    void OnEnable()
    {
        inputActions.Enable();
    }

    void OnDisable()
    {
        inputActions.Disable();
    }

    void Update()
    {
        // Lry的修改：正式运行模式。这里与下方原 Debugger 逻辑存在职责冲突：
        // Lry的修改：原逻辑通过 Keyboard/Input/Raycast 自行模拟 locomotion；正式逻辑必须读取 PlayerCC 的权威状态，避免动画状态与技能状态发生 state divergence。
        if (usePlayerCCState && controller != null)
        {
            // Lry的修改：在正式动画同步前刷新技能视图，在 HandleJumpFromPlayerCC 里能读取到最新技能上下文。
            SyncEquippedSkillViewFromPlayerCC();

            HandleAnimatorFromPlayerCC();
            return;
        }

        // Lry的修改：如果 usePlayerCCState 开启但 controller 没有绑定，会自动回退到原调试链路；这保证旧动画测试场景不被破坏。

        //简单的攀爬Posture控制
        if (Keyboard.current.cKey.wasPressedThisFrame)
        {
            enterClimbing = !enterClimbing;
        }


        SwitchLocomotion();
  

        HandleInput();

        HandleAnimator();
    }

    //========================
    // Locomotion选择
    //========================
    void SwitchLocomotion()
{
    // 1️⃣ 默认状态：Ground
    locomotion = Locomotion.isGrounded;

    // 2️⃣ 如果有垂直输入（爬梯等）
    if (enterClimbing)
    {
        locomotion = Locomotion.isClimbing;
        return;
    }
    if (!enterClimbing)
    {
         locomotion = Locomotion.isGrounded;
        return;
    }

    // 3️⃣ 向下发射射线检测地面
    RaycastHit hit;

    if (Physics.Raycast(transform.position , Vector3.down, out hit, groundDetect))
    {
        // 检测到地面，且没有垂直输入
        if (Mathf.Abs(move.y) <= 0.01f)
        {
            locomotion = Locomotion.isGrounded;
        }
    }
    else
    {
        // 没检测到地面 → 在空中
        locomotion = Locomotion.isMidAir;
    }
}
  

    //========================
    // 输入获取
    //========================

    void HandleInput()
    {
        move = inputActions.Player.Move.ReadValue<Vector2>();

        jumpAxis = inputActions.Player.Jump.ReadValue<float>();

        dashPressed = inputActions.Player.Dash.triggered;

        hidePressed = inputActions.Player.Hide.triggered;
    }

    //========================
    // Animator处理
    //========================

    void HandleAnimator()
    {
        HandleRun();

        HandleClimb();

        HandleJump();

        HandleDash();

        HandleHide();
    }

    // Lry的修改：正式动画同步入口。该方法把 PlayerCC 的领域状态映射为 Animator 参数，是典型的 presentation adapter / synchronization layer。
    void HandleAnimatorFromPlayerCC()
    {
        HandleRunFromPlayerCC();

        HandleClimbFromPlayerCC();

        HandleJumpFromPlayerCC();

        HandleDashFromPlayerCC();

        HandleHideFromPlayerCC();

        HandleClimbExitTriggersFromPlayerCC();
    }

    // Lry的修改：同步动画侧技能视图。优先读取 PlayerCC.equippedSkills，也就是 UI 装备槽同步后的真实装备技能。
    // Lry的修改：Inspector 可见的 List<SkillBase>，降低调试成本。
    void SyncEquippedSkillViewFromPlayerCC()
    {
        equippedSkills.Clear();

        if (controller.equippedSkills != null)
        {
            for (int i = 0; i < controller.equippedSkills.Count; i++)
            {
                SkillBase skill = controller.equippedSkills[i];
                if (skill != null)
                {
                    equippedSkills.Add(skill);
                }
            }
        }
    }

    // Lry的修改：可以用这个接口写类似 HasEquippedSkill("22-jj") 的分支，而不是直接遍历 List，减少字符串判断重复代码。
    public bool HasEquippedSkill(string skillID)
    {
        if (string.IsNullOrEmpty(skillID) || equippedSkills == null)
        {
            return false;
        }

        for (int i = 0; i < equippedSkills.Count; i++)
        {
            SkillBase skill = equippedSkills[i];
            if (skill != null && skill.skillID == skillID)
            {
                return true;
            }
        }

        return false;
    }

    // Lry的修改：Run 参数仍然表示 locomotion 中的水平/移动意图，但攀爬态下强制关闭，避免与 Climb 状态并发竞争。
    void HandleRunFromPlayerCC()
    {
        if (!syncRunFromPlayerInput)
        {
            return;
        }

        Vector2 playerMove = controller.GetMoveInput();
        bool running = controller.CurrentPosture != PlayerCC.Posture.Climbing &&
                       (Mathf.Abs(playerMove.x) > 0.1f || Mathf.Abs(playerMove.y) > 0.1f);

        SetBoolIfExists(hasRun, "Run", running);
    }

    // Lry的修改：Climb/ClimbInput 由攀爬技能通过 PlayerCC.SetClimbState 写入，本层只做参数转发，不再自行判断射线或按键。
    void HandleClimbFromPlayerCC()
    {
        
        bool climbing = controller.CurrentPosture == PlayerCC.Posture.Climbing;
        float climbInput = controller.ClimbInput;

        SetBoolIfExists(hasClimb, "Climb", climbing);
        SetFloatIfExists(hasClimbVel, "ClimbVel", climbInput, 0.1f);
        SetFloatIfExists(hasClimbInput, "ClimbInput", climbInput, 0.1f);
    }

    // Lry的修改：Jump/VerticalVelocity 由物理层真实速度驱动。这里与原 jumpHold 曲线冲突：原曲线是测试用 procedural animation parameter，不代表 CharacterController 的真实起落速度。
    void HandleJumpFromPlayerCC()
    {
        SetFloatImmediateIfExists(hasVerticalVelocity, "VerticalVelocity", controller.VerticalVelocity);
        SetFloatImmediateIfExists(hasJump, "Jump", controller.VerticalVelocity);
        SetIntIfExists(hasJumpType, "JumpType", controller.JumpType);
        SetBoolIfExists(hasIsGrounded, "IsGrounded", controller.CurrentPosture == PlayerCC.Posture.Grounded);
    }

    // Lry的修改：DashPosture 是技能层输出的 0-1 姿态权重，适合驱动 BlendTree 或过渡条件；Dash Bool 同步为兼容旧状态机参数。
    void HandleDashFromPlayerCC()
    {
        float dashPosture = controller.DashPosture;

        SetFloatImmediateIfExists(hasDashPosture, "DashPosture", dashPosture);
        SetBoolIfExists(hasDash, "Dash", dashPosture > 0.01f);
    }

    // Lry的修改：当前 PlayerCC 只有 Hide 输入接口，没有独立隐身状态字段；这里同步输入态是兼容旧 AnimatorStateDebugger 的 Hide Bool，后续若有隐身技能状态，应改为读取领域状态。
    void HandleHideFromPlayerCC()
    {
        SetBoolIfExists(hasHide, "Hide", controller.IsHidePressed());
    }

    // Lry的修改：攀爬翻越属于 one-shot animation event，必须使用 request/consume 模式转成 Trigger，防止每帧重复触发同一个过渡。
    void HandleClimbExitTriggersFromPlayerCC()
    {
        if (controller.ConsumeClimbExitUpAnimationRequest())
        {
            SetTriggerIfExists(hasClimbExitUp, "Climb_Exit_Up");
        }

        if (controller.ConsumeClimbExitDownAnimationRequest())
        {
            SetTriggerIfExists(hasClimbExitDown, "Climb_Exit_Down");
        }
    }

    //========================
    // Run
    //========================

    void HandleRun()
    {
        if (locomotion == Locomotion.isGrounded)
        {
            if (Mathf.Abs(move.x) > 0.01f)
            {
                animator.SetBool("Run", true);
            }
            else
            {
                animator.SetBool("Run", false);
            }
        }
        else
        {
            animator.SetBool("Run", false);
        }
    }

    //========================
    // Climb
    //========================

  void HandleClimb()
{
    if (locomotion == Locomotion.isClimbing)
    {
        float climbVelTarget = 0f;

        if (Mathf.Abs(move.y) > 0.01f)
        {
            animator.SetBool("Climb", true);

            if (move.y > 0.01f)
            {
                climbVelTarget = 1f;   // 向上
            }
            else if (move.y < -0.01f)
            {
                climbVelTarget = -1f;   // 向下/停止
            }
        }
        else
        {
            climbVelTarget = 0f;
        }

        // 平滑实现+阈值吸附
        
        animator.SetFloat("ClimbInput", climbVelTarget , 0.2f , Time.deltaTime * 6f);

        float climbVelCurrent = animator.GetFloat("ClimbInput");
        

        // ⭐关键：硬归零

        if (Mathf.Abs(climbVelCurrent - climbVelTarget) < 0.1f)
        {
            animator.SetFloat("ClimbInput", climbVelTarget);
           
        }
        
    }

    else
    {
        animator.SetBool("Climb", false);

        // 不在攀爬状态时归零
        animator.SetFloat("ClimbInput", 0f, 0.2f, Time.deltaTime);
    }
}

    //========================
    // Jump（核心部分）
    //========================

    void HandleJump()
{
    // 按下Jump，开始上升
    if (jumpAxis > 0f  && !jumpRising && !jumpFalling)
    {
        jumpRising = true;
    }

    // 上升阶段：0 → 1
    if (jumpRising)
    {
        jumpHold += jumpChangeRate * Time.deltaTime;

        if (jumpHold >= 1f)
        {
            jumpHold = 1f;

            jumpRising = false;
            jumpFalling = true;
        }
    }

    // 下降阶段：1 → 0
    if (jumpFalling)
    {
        jumpHold -= jumpChangeRate * Time.deltaTime;

        if (jumpHold <= 0f)
        {
            jumpHold = 0f;

            jumpFalling = false;
        }
    }

    // 持续更新 Animator
    animator.SetFloat("Jump", jumpHold);
}

    //========================
    // Dash
    //========================

    void HandleDash()
    {
        if (dashPressed)
        {
            animator.SetBool("Dash", true);
        }
        else
        {
            animator.SetBool("Dash", false);
        }
    }

    //========================
    // Hide
    //========================

    void HandleHide()
    {
        if (hidePressed)
        {
            animator.SetBool("Hide", true);
        }
        else
        {
            animator.SetBool("Hide", false);
        }
    }

    //用来暂时实现悬崖边上翻越的功能event
    public void LockRootMotion()
    {
        animator.applyRootMotion = false;
        Debug.Log("RM Locked!");
    }
    public void ApplyRootMotion()
    {
        animator.applyRootMotion = true;
        Debug.Log("RM Applaied!");
    }

    // Lry的修改：读取 Animator Controller 参数表，确认当前 Controller 支持哪些参数，避免正式同步器和动画状态机参数命名不同步时直接崩溃。
    void CacheAnimatorParameters()
    {
        if (animator == null)
        {
            return;
        }

        AnimatorControllerParameter[] parameters = animator.parameters;
        for (int i = 0; i < parameters.Length; i++)
        {
            string parameterName = parameters[i].name;
            AnimatorControllerParameterType parameterType = parameters[i].type;

            if (parameterName == "Run" && parameterType == AnimatorControllerParameterType.Bool) hasRun = true;
            else if (parameterName == "Climb" && parameterType == AnimatorControllerParameterType.Bool) hasClimb = true;
            else if (parameterName == "ClimbVel" && parameterType == AnimatorControllerParameterType.Float) hasClimbVel = true;
            else if (parameterName == "ClimbInput" && parameterType == AnimatorControllerParameterType.Float) hasClimbInput = true;
            else if (parameterName == "Climb_Exit_Up" && parameterType == AnimatorControllerParameterType.Trigger) hasClimbExitUp = true;
            else if (parameterName == "Climb_Exit_Down" && parameterType == AnimatorControllerParameterType.Trigger) hasClimbExitDown = true;
            else if (parameterName == "VerticalVelocity" && parameterType == AnimatorControllerParameterType.Float) hasVerticalVelocity = true;
            else if (parameterName == "Jump" && parameterType == AnimatorControllerParameterType.Float) hasJump = true;
            else if (parameterName == "JumpType" && parameterType == AnimatorControllerParameterType.Int) hasJumpType = true;
            else if (parameterName == "IsGrounded" && parameterType == AnimatorControllerParameterType.Bool) hasIsGrounded = true;
            else if (parameterName == "DashPosture" && parameterType == AnimatorControllerParameterType.Float) hasDashPosture = true;
            else if (parameterName == "Dash" && parameterType == AnimatorControllerParameterType.Bool) hasDash = true;
            else if (parameterName == "Hide" && parameterType == AnimatorControllerParameterType.Bool) hasHide = true;
        }
    }

    // Lry的修改：以下 SetParameterIfExists 方法是 defensive animator binding，解决动画参数增删改名期间的空接口问题。
    void SetBoolIfExists(bool exists, string parameterName, bool value)
    {
        if (exists)
        {
            animator.SetBool(parameterName, value);
        }
    }

    // Lry的修改：带 damping 的 Float 写入用于平滑 BlendTree 参数，尤其是攀爬输入的 -1/0/1 切换。
    void SetFloatIfExists(bool exists, string parameterName, float value, float dampTime)
    {
        if (exists)
        {
            animator.SetFloat(parameterName, value, dampTime, Time.deltaTime);
        }
    }

    // Lry的修改：即时 Float 写入用于物理量同步，如 VerticalVelocity/DashPosture，避免阻尼造成动画滞后于真实运动。
    void SetFloatImmediateIfExists(bool exists, string parameterName, float value)
    {
        if (exists)
        {
            animator.SetFloat(parameterName, value);
        }
    }

    // Lry的修改：JumpType 是离散枚举型动画参数，使用 Integer 保留类型语义。
    void SetIntIfExists(bool exists, string parameterName, int value)
    {
        if (exists)
        {
            animator.SetInteger(parameterName, value);
        }
    }

    // Lry的修改：Trigger 只用于一次性状态跳转，配合 PlayerCC 的 Consume 方法保证事件只被状态机消费一次。
    void SetTriggerIfExists(bool exists, string parameterName)
    {
        if (exists)
        {
            animator.SetTrigger(parameterName);
        }
    }



    // public void OnVaultEndSnap()
    // {
        
        
    //     // 2. 临时关闭 CharacterController 防止碰撞干扰
    //     cc.enabled = false;

    //     // 3. 直接移动角色根节点
    //     player.transform.position = targetPos.position;

    //     // 4. 重新启用 CharacterController
    //     cc.enabled = true;

    //     Debug.Log("Vault End Snap Executed");
    // }
}
