using UnityEngine;
using System.Collections.Generic;

namespace Skills
{
    [CreateAssetMenu(fileName = "SkillDatabase", menuName = "Game/Skill Database")]
    public class SkillDatabase : ScriptableObject
    {
        public List<SkillBase> allSkills; // 拖入所有的技能 .asset

        // 通过 ID 快速查找技能，用于解锁逻辑
        public SkillBase GetSkillByID(string id)
        {
            return allSkills.Find(s => s.skillID == id);
        }
    }
}