using UnityEngine;

public class SpawnSpot : MonoBehaviour
{
    public GameObject player;

    public GameObject spawnSpotObject;

    void Start()
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
            player.transform.position = spawnSpotObject.transform.position;
        }
    }
}