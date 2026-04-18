using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Skills
{
    [CreateAssetMenu(fileName = "SkillDatabase", menuName = "Game/Skill Database")]
    public class SkillDatabase : ScriptableObject
    {
        public List<SkillBase> allSkills; // 拖入所有的技能 .asset

        // 通过 ID 快速查找技能，用于解锁逻辑
        public SkillBase GetSkillByID(string id)
        {
            if (allSkills == null || string.IsNullOrWhiteSpace(id))
            {
                return null;
            }

            string normalizedId = id.Trim();
            return allSkills.Find(s => s != null && (s.skillID == normalizedId || s.name == normalizedId));
        }

#if UNITY_EDITOR
        [ContextMenu("自动收集所有 SkillBase 资产")]
        private void AutoCollectSkillAssets()
        {
            string[] guids = AssetDatabase.FindAssets("t:ScriptableObject");
            allSkills = new List<SkillBase>();

            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                SkillBase skill = AssetDatabase.LoadAssetAtPath<SkillBase>(path);
                if (skill == null || allSkills.Contains(skill))
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(skill.skillID))
                {
                    Undo.RecordObject(skill, "Fill Skill ID");
                    skill.skillID = skill.name;
                    EditorUtility.SetDirty(skill);
                }

                allSkills.Add(skill);
            }

            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
            Debug.Log($"SkillDatabase 已收集 {allSkills.Count} 个技能资产。", this);
        }
#endif
    }
}
