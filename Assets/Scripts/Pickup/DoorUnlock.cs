using UnityEngine;

public class DoorUnlock : MonoBehaviour
{

    Animator doorAnimator;
    public AudioClip openDoor;
    AudioSource audioSource;
    [Range (0 , 1)]
    public float volume;
    
    private bool unlocked;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        doorAnimator = GetComponent<Animator>();
    }

    void OnTriggerEnter(Collider other)
    {
        if (unlocked)
        {
            return;
        }

        PlayerCC player = other.GetComponent<PlayerCC>();

        if (player == null)
        {
            return;
        }

        if (player.hasDoorPickup)
        {
            unlocked = true;

            doorAnimator.SetBool("HasKey" , true);
            audioSource.PlayOneShot(openDoor , volume);
            
        }
    }
}