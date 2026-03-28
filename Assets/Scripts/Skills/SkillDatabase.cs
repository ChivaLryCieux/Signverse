using UnityEngine;
using System.Collections.Generic;

namespace Skills
{
    [CreateAssetMenu(fileName = "SkillDatabase", menuName = "Game/Skill Database")]
    public class SkillDatabase : ScriptableObject
    {
        // 将你创建的所有技能文件拖进这个列表
        public List<SkillBase> allSkills;

        // 方便通过 ID 查找技能
        public SkillBase GetSkillByID(string id)
        {
            // 使用 Find 逻辑，确保 allSkills 不为 null
            return allSkills?.Find(s => s.skillID == id);
        }
    }
}