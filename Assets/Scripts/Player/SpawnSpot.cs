using UnityEngine;

public class SpawnSpot : MonoBehaviour
{
    public GameObject player;

    public GameObject spawnSpotObject;

   void Awake()
    {
        if (player == null)
        {
            PlayerCC foundPlayer = FindFirstObjectByType<PlayerCC>();

            if (foundPlayer != null)
            {
                player = foundPlayer.gameObject;
            }
        }

        if (player != null && spawnSpotObject != null)
        {
            // 如果玩家有 CharacterController，先禁用再移动
            var cc = player.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            player.transform.position = spawnSpotObject.transform.position;

            if (cc != null) cc.enabled = true;
        }
    }
}