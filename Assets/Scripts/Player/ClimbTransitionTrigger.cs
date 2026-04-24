using Skills;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ClimbTransitionTrigger : MonoBehaviour
{
    [Tooltip("只有拥有该技能的玩家进入时，才会注册为攀爬转换区域。")]
    [SerializeField] private string requiredSkillID = "12-mj";

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
            return;
        }

        controller.EnterClimbTransitionTrigger();
    }

    private void OnTriggerExit(Collider other)
    {
        PlayerCC controller = other.GetComponentInParent<PlayerCC>();

        if (!CanUseClimbTransition(controller))
        {
            return;
        }

        controller.ExitClimbTransitionTrigger();
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
