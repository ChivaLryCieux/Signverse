using UnityEngine;

namespace Skills
{
    [CreateAssetMenu(fileName = "41-cm", menuName = "Game/Skills/41 CM Decoy")]
    public class Skill41CMDecoy : SkillBase
    {
        [Header("替身设置")]
        public KeyCode summonKey = KeyCode.C;
        public GameObject decoyPrefab;
        public float lifetime = 3f;

        public override void OnActivate(GameObject user, PlayerCC controller) { }

        public override void OnUpdate(GameObject user, PlayerCC controller)
        {
            if (!Input.GetKeyDown(summonKey)) return;

            if (decoyPrefab == null)
            {
                Debug.Log("41-cm: 尚未指定 decoyPrefab");
                return;
            }

            GameObject clone = Instantiate(decoyPrefab, user.transform.position, user.transform.rotation);
            Destroy(clone, lifetime);
        }
    }
}
