using UnityEngine;

namespace Skills
{
    public enum SkillType { Passive, Active }
    
    public abstract class SkillBase : ScriptableObject
    {
        public string skillID;
        public string skillName;
        public SkillType type;
        
        public abstract void OnActivate(GameObject user, PlayerCC controller);
        
        public virtual void OnUpdate(GameObject user, PlayerCC controller) { }
    }
}