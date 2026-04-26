using UnityEngine;

namespace Skills
{
    // ===== 元数据 =====
    public enum SkillType { Passive, Active }
    
    public abstract class SkillBase : ScriptableObject
    {
        public string skillID;
        public string skillName;
        public SkillType type;

        // ===== 物理控制 =====
        public virtual void OnActivate(GameObject user, PlayerCC controller)
        {
            OnActivate(user, controller, controller.CurrentPosture);
        }

        public abstract void OnActivate(GameObject user, PlayerCC controller, PlayerCC.Posture posture);

        public virtual void OnUpdate(GameObject user, PlayerCC controller)
        {
            OnUpdate(user, controller, controller.CurrentPosture);
        }

        public virtual void OnUpdate(GameObject user, PlayerCC controller, PlayerCC.Posture posture) { }

        // ===== 动画控制 =====
    }
}
