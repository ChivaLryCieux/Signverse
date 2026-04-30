using System.Collections.Generic;
using UnityEngine;

namespace Skills
{
    [CreateAssetMenu(fileName = "41-cm", menuName = "Game/Skills/41 CM Decoy")]
    public class Skill41CMDecoy : SkillBase
    {
        // ===== 元数据 =====

        // ===== 物理控制 =====
        [Header("替身设置")]
        public string standChildName = "Stand";
        [Min(0.01f)] public float duration = 10f;
        public string[] autoEquippedSkillIDs = { "10-xx", "20-xx" };

        private Transform standTransform;
        private CharacterController standController;
        private Renderer[] standRenderers;
        private readonly List<SkillBase> autoEquippedSkills = new List<SkillBase>();
        private readonly List<SkillBase> addedSkillsToUnlocked = new List<SkillBase>();
        private readonly List<SkillBase> addedSkillsToEquipped = new List<SkillBase>();
        private float activeTimer;
        private bool isControllingStand;

        // 替身技能是按隐藏键触发，激活入口暂不处理。
        public override void OnActivate(GameObject user, PlayerCC controller, PlayerCC.Posture posture) { }

        // 第一次按隐藏键接管 Stand，持续期间再次按隐藏键会把玩家本体瞬移到 Stand 位置。
        public override void OnUpdate(GameObject user, PlayerCC controller, PlayerCC.Posture posture)
        {
            if (!isControllingStand)
            {
                if (controller.WasHidePressed())
                {
                    StartStandControl(user, controller);
                }

                return;
            }

            activeTimer -= Time.deltaTime;

            if (controller.WasHidePressed())
            {
                StopStandControl(user, controller, true);
                return;
            }

            if (activeTimer <= 0f)
            {
                StopStandControl(user, controller, false);
            }
        }

        // ===== 动画控制 =====

        private void StartStandControl(GameObject user, PlayerCC controller)
        {
            if (!ResolveStand(user, controller))
            {
                Debug.LogWarning($"41-cm: 没有在玩家子物体中找到 {standChildName}。", user);
                return;
            }

            standTransform.position = user.transform.position;
            standTransform.rotation = user.transform.rotation;
            standTransform.gameObject.tag = user.tag;
            standTransform.gameObject.layer = user.layer;
            standTransform.gameObject.SetActive(true);
            SetStandVisible(true);

            activeTimer = duration;
            isControllingStand = true;
            controller.BeginControlProxy(standController);
            AutoEquipStandSkills(controller);
        }

        private void StopStandControl(GameObject user, PlayerCC controller, bool teleportPlayerToStand)
        {
            controller.EndControlProxy(teleportPlayerToStand);
            RemoveAutoEquippedStandSkills(controller);
            isControllingStand = false;
            activeTimer = 0f;

            if (standTransform != null)
            {
                SetStandVisible(false);
                standTransform.gameObject.SetActive(false);
                standTransform.position = user.transform.position;
                standTransform.rotation = user.transform.rotation;
            }
        }

        private bool ResolveStand(GameObject user, PlayerCC controller)
        {
            if (standTransform == null)
            {
                standTransform = FindChildRecursive(user.transform, standChildName);
            }

            if (standTransform == null)
            {
                return false;
            }

            standController = standTransform.GetComponent<CharacterController>();
            if (standController == null)
            {
                standController = standTransform.gameObject.AddComponent<CharacterController>();
            }

            CopyCharacterControllerSettings(controller.GetCharacterController(), standController);
            standRenderers = standTransform.GetComponentsInChildren<Renderer>(true);
            return true;
        }

        private void AutoEquipStandSkills(PlayerCC controller)
        {
            ResetAutoEquipState();

            if (controller == null || controller.masterDatabase == null || autoEquippedSkillIDs == null)
            {
                return;
            }

            for (int i = 0; i < autoEquippedSkillIDs.Length; i++)
            {
                string skillID = autoEquippedSkillIDs[i];
                if (string.IsNullOrWhiteSpace(skillID))
                {
                    continue;
                }

                SkillBase skill = controller.masterDatabase.GetSkillByID(skillID);
                if (skill == null)
                {
                    Debug.LogWarning($"41-cm: SkillDatabase 中找不到自动装备技能 {skillID}。", controller);
                    continue;
                }

                autoEquippedSkills.Add(skill);

                if (controller.unlockedSkills != null && !controller.unlockedSkills.Contains(skill))
                {
                    controller.unlockedSkills.Add(skill);
                    addedSkillsToUnlocked.Add(skill);
                }

                if (controller.equippedSkills != null && !controller.equippedSkills.Contains(skill))
                {
                    controller.equippedSkills.Add(skill);
                    addedSkillsToEquipped.Add(skill);
                }
            }
        }

        private void RemoveAutoEquippedStandSkills(PlayerCC controller)
        {
            if (controller == null)
            {
                ResetAutoEquipState();
                return;
            }

            if (controller.unlockedSkills != null)
            {
                for (int i = 0; i < addedSkillsToUnlocked.Count; i++)
                {
                    controller.unlockedSkills.Remove(addedSkillsToUnlocked[i]);
                }
            }

            if (controller.equippedSkills != null)
            {
                for (int i = 0; i < addedSkillsToEquipped.Count; i++)
                {
                    controller.equippedSkills.Remove(addedSkillsToEquipped[i]);
                }
            }

            ResetAutoEquipState();
        }

        private void ResetAutoEquipState()
        {
            autoEquippedSkills.Clear();
            addedSkillsToUnlocked.Clear();
            addedSkillsToEquipped.Clear();
        }

        private void SetStandVisible(bool visible)
        {
            if (standRenderers == null)
            {
                return;
            }

            for (int i = 0; i < standRenderers.Length; i++)
            {
                if (standRenderers[i] != null)
                {
                    standRenderers[i].enabled = visible;
                }
            }
        }

        private static void CopyCharacterControllerSettings(CharacterController source, CharacterController target)
        {
            if (source == null || target == null)
            {
                return;
            }

            target.enabled = false;
            target.center = source.center;
            target.radius = source.radius;
            target.height = source.height;
            target.slopeLimit = source.slopeLimit;
            target.stepOffset = source.stepOffset;
            target.skinWidth = source.skinWidth;
            target.minMoveDistance = source.minMoveDistance;
            target.detectCollisions = source.detectCollisions;
            target.enableOverlapRecovery = source.enableOverlapRecovery;
            target.enabled = true;
        }

        private static Transform FindChildRecursive(Transform parent, string childName)
        {
            if (parent == null || string.IsNullOrWhiteSpace(childName))
            {
                return null;
            }

            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (child.name == childName)
                {
                    return child;
                }

                Transform nested = FindChildRecursive(child, childName);
                if (nested != null)
                {
                    return nested;
                }
            }

            return null;
        }
    }
}
