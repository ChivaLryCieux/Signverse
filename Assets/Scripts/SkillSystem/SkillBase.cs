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
        // 使用当前 PlayerCC 姿态调用技能激活逻辑，兼容旧的两参数调用方式。
        public virtual void OnActivate(GameObject user, PlayerCC controller)
        {
            OnActivate(user, controller, controller.CurrentPosture);
        }

        // 技能被主动触发时执行，子类在这里实现核心激活效果。
        public abstract void OnActivate(GameObject user, PlayerCC controller, PlayerCC.Posture posture);

        // 使用当前 PlayerCC 姿态调用每帧技能更新逻辑，兼容旧的两参数调用方式。
        public virtual void OnUpdate(GameObject user, PlayerCC controller)
        {
            OnUpdate(user, controller, controller.CurrentPosture);
        }

        // 技能每帧更新入口，子类按需要覆盖。
        public virtual void OnUpdate(GameObject user, PlayerCC controller, PlayerCC.Posture posture) { }

        // ===== 动画控制 =====
    }
}
