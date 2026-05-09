using Skills;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ClimbTransitionTrigger : MonoBehaviour
{
    [Tooltip("只有拥有该技能的玩家进入时，才会注册为攀爬转换区域。")]
    [SerializeField] private string requiredSkillID = "12-mj";

    [Header("攀爬区域")]
    [Tooltip("开启后，玩家在这个 Trigger 内就会被视为可攀爬，不再强制依赖 climbableMask 射线命中。")]
    [SerializeField] private bool actsAsClimbVolume = true;
    [Tooltip("玩家距离这个 Trigger 顶部小于该距离时，自动触发顶部翻越动画。")]
    [SerializeField] private float autoExitUpTopDistance = 0.35f;

    [Header("顶部翻越强制位移")]
    [Tooltip("到达顶部并完成翻越动画后应用的强制位移。X=面朝方向前进距离，Y=向上距离。")]
    [SerializeField] private Vector2 exitUpForcedOffset = new Vector2(0.6f, 0.8f);
    [Tooltip("强制位移持续时间。越小越接近瞬移，越大越平滑。")]
    [SerializeField] private float exitUpForcedMoveDuration = 0.18f;
    [Tooltip("触发顶部翻越后的冷却，避免仍停留在 Trigger 内时重复触发。")]
    [SerializeField] private float exitUpCooldown = 0.75f;

    [Header("调试")]
    [SerializeField] private bool debugLogs = false;

    private float nextExitUpAllowedTime;

    public bool ActsAsClimbVolume => actsAsClimbVolume;
    public bool DebugLogs => debugLogs;

    public bool CanAutoExitUp(PlayerCC controller)
    {
        if (controller == null)
        {
            return false;
        }

        Collider triggerCollider = GetComponent<Collider>();
        if (triggerCollider == null)
        {
            return false;
        }

        float distanceToTop = triggerCollider.bounds.max.y - controller.transform.position.y;
        return distanceToTop <= Mathf.Max(0f, autoExitUpTopDistance);
    }

    public Vector3 GetExitTopPosition(PlayerCC controller, float forwardOffset)
    {
        if (controller == null)
        {
            return transform.position;
        }

        Collider triggerCollider = GetComponent<Collider>();
        float topY = triggerCollider != null ? triggerCollider.bounds.max.y : transform.position.y;
        Vector3 facing = controller.GetFacing().sqrMagnitude > 0.01f ? controller.GetFacing().normalized : Vector3.right;
        Vector3 position = controller.transform.position + new Vector3(facing.x * forwardOffset, 0f, 0f);
        position.y = topY;
        position.z = 0f;
        return position;
    }

    public bool TryStartExitUp(PlayerCC controller)
    {
        if (!CanUseClimbTransition(controller) || !CanAutoExitUp(controller))
        {
            return false;
        }

        if (Time.time < nextExitUpAllowedTime)
        {
            return true;
        }

        nextExitUpAllowedTime = Time.time + Mathf.Max(0f, exitUpCooldown);
        controller.QueueClimbExitUpForcedMove(exitUpForcedOffset, exitUpForcedMoveDuration);
        controller.RequestClimbExitUpAnimation();
        controller.SetVerticalVelocity(0f);
        controller.SetClimbState(true, 0f);

        if (debugLogs)
        {
            Debug.Log($"[ClimbTransitionTrigger] Start exit up. player={controller.name}, offset={exitUpForcedOffset}, duration={exitUpForcedMoveDuration:F2}", this);
        }

        return true;
    }

    private void Reset()
    {
        Collider triggerCollider = GetComponent<Collider>();
        triggerCollider.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerCC controller = other.GetComponentInParent<PlayerCC>();

        if (!CanUseClimbTransition(controller))
        {
            if (debugLogs)
            {
                Debug.Log($"[ClimbTransitionTrigger] OnTriggerEnter ignored. other={other.name}, player={(controller != null ? controller.name : "null")}, requiredSkill={requiredSkillID}", this);
            }
            return;
        }

        controller.EnterClimbTransitionTrigger(this);

        if (debugLogs)
        {
            Debug.Log($"[ClimbTransitionTrigger] Entered climb volume. player={controller.name}, actsAsClimbVolume={actsAsClimbVolume}", this);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        PlayerCC controller = other.GetComponentInParent<PlayerCC>();

        if (!CanUseClimbTransition(controller))
        {
            if (debugLogs)
            {
                Debug.Log($"[ClimbTransitionTrigger] OnTriggerExit ignored. other={other.name}, player={(controller != null ? controller.name : "null")}, requiredSkill={requiredSkillID}", this);
            }
            return;
        }

        controller.ExitClimbTransitionTrigger(this);

        if (debugLogs)
        {
            Debug.Log($"[ClimbTransitionTrigger] Exited climb volume. player={controller.name}", this);
        }
    }

    private bool CanUseClimbTransition(PlayerCC controller)
    {
        if (controller == null)
        {
            return false;
        }

        if (controller.HasUnlockedSkill(requiredSkillID))
        {
            return true;
        }

        return controller.HasUnlockedSkill<Skill12MJClimb>();
    }
}
