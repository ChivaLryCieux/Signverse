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

        // 替身技能是按隐藏键触发，激活入口暂不处理。
        public override void OnActivate(GameObject user, PlayerCC controller, PlayerCC.Posture posture) { }

        // 每帧监听隐藏键，生成替身并按生命周期销毁。
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
