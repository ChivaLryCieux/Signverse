using UnityEngine;

namespace Skills
{
    public abstract class SkillBase : ScriptableObject
    {
        public string skillID;
        public string skillName;
        
        public abstract void OnActivate(GameObject user, PlayerCC controller);
        
        public virtual void OnUpdate(GameObject user, PlayerCC controller) { }
    }
}