using UnityEngine;

namespace Skills
{
    [CreateAssetMenu(fileName = "41-cm", menuName = "Game/Skills/41 CM Decoy")]
    public class Skill41CMDecoy : SkillBase
    {
        // ===== 元数据 =====

        // ===== 物理控制 =====
        [Header("替身设置")]
        public GameObject decoyPrefab;
        public float lifetime = 3f;

        public override void OnActivate(GameObject user, PlayerCC controller, PlayerCC.Posture posture) { }

        public override void OnUpdate(GameObject user, PlayerCC controller, PlayerCC.Posture posture)
        {
            if (!controller.WasHidePressed()) return;

            if (decoyPrefab == null)
            {
                Debug.Log("41-cm: 尚未指定 decoyPrefab");
                return;
            }

            GameObject clone = Instantiate(decoyPrefab, user.transform.position, user.transform.rotation);
            Destroy(clone, lifetime);
        }

        // ===== 动画控制 =====
    }
}
