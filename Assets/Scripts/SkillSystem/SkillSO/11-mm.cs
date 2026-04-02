using UnityEngine;

namespace Skills
{
    [CreateAssetMenu(fileName = "11-mm", menuName = "Game/Skills/11 MM Custom Portal")]
    public class Skill11MMCustomPortal : SkillBase
    {
        [Header("传送门设置")]
        public KeyCode placePortalKey = KeyCode.P;
        public float portalTriggerRadius = 0.75f;
        public float portalYOffset = 0.5f;

        private bool hasEntryPortal;
        private bool hasExitPortal;
        private Vector3 entryPortalPosition;
        private Vector3 exitPortalPosition;

        public override void OnActivate(GameObject user, PlayerCC controller) { }

        public override void OnUpdate(GameObject user, PlayerCC controller)
        {
            if (Input.GetKeyDown(placePortalKey))
            {
                Vector3 portalPos = user.transform.position + Vector3.up * portalYOffset;
                if (!hasEntryPortal)
                {
                    entryPortalPosition = portalPos;
                    hasEntryPortal = true;
                    Debug.Log("11-mm: 已放置入口传送门");
                }
                else
                {
                    exitPortalPosition = portalPos;
                    hasExitPortal = true;
                    Debug.Log("11-mm: 已放置出口传送门");
                }
            }

            if (!hasEntryPortal || !hasExitPortal) return;

            if (Vector3.Distance(user.transform.position, entryPortalPosition) <= portalTriggerRadius)
            {
                CharacterController cc = controller.GetCharacterController();
                cc.enabled = false;
                user.transform.position = exitPortalPosition;
                cc.enabled = true;
            }
        }
    }
}
