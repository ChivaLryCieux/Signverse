using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class ProximityDistanceSFX : MonoBehaviour
{
    [Header("距离检测")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private Transform volumeCenter;
    [SerializeField] private Transform playerOverride;
    [SerializeField, Min(0.01f)] private float audibleDistance = 8f;
    [SerializeField, Min(0f)] private float startPlaybackDistance = 8f;
    [SerializeField, Min(0.02f)] private float playerSearchInterval = 0.5f;

    [Header("音量")]
    [SerializeField, Range(0f, 1f)] private float maxVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float minVolume = 0f;
    [SerializeField, Min(0f)] private float volumeLerpSpeed = 8f;
    [SerializeField] private bool playWhenInRange = true;
    [SerializeField] private bool stopWhenSilent = true;

    private AudioSource audioSource;
    private Transform player;
    private float nextPlayerSearchTime;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();

        if (volumeCenter == null)
        {
            volumeCenter = transform;
        }

        player = playerOverride;
        audioSource.loop = true;
        audioSource.playOnAwake = false;
        audioSource.volume = 0f;
    }

    private void Update()
    {
        ResolvePlayer();

        float targetVolume = 0f;

        if (player != null)
        {
            float distance = Vector3.Distance(player.position, volumeCenter.position);

            if (distance <= audibleDistance)
            {
                float distance01 = Mathf.Clamp01(distance / audibleDistance);
                targetVolume = Mathf.Lerp(maxVolume, minVolume, distance01);
            }

            if (playWhenInRange && !audioSource.isPlaying && distance <= GetPlaybackDistance())
            {
                audioSource.Play();
            }
        }

        audioSource.volume = Mathf.MoveTowards(audioSource.volume, targetVolume, volumeLerpSpeed * Time.deltaTime);

        if (stopWhenSilent && audioSource.isPlaying && audioSource.volume <= 0.001f && targetVolume <= 0f)
        {
            audioSource.Stop();
        }
    }

    private void ResolvePlayer()
    {
        if (playerOverride != null)
        {
            player = playerOverride;
            return;
        }

        if (player != null || Time.time < nextPlayerSearchTime)
        {
            return;
        }

        nextPlayerSearchTime = Time.time + playerSearchInterval;

        if (!string.IsNullOrWhiteSpace(playerTag))
        {
            GameObject taggedPlayer = GameObject.FindGameObjectWithTag(playerTag);
            if (taggedPlayer != null)
            {
                player = taggedPlayer.transform;
                return;
            }
        }

        PlayerCC playerController = FindObjectOfType<PlayerCC>();
        if (playerController != null)
        {
            player = playerController.transform;
        }
    }

    private float GetPlaybackDistance()
    {
        return startPlaybackDistance > 0f ? startPlaybackDistance : audibleDistance;
    }
}
