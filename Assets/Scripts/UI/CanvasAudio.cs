using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class CanvasAudio : MonoBehaviour
{
    public AudioSource audioSource;

    [Header("audio")]
    public AudioClip showNarrativeSFX;
    public AudioClip closeNarrativeSFX; 
    // Start is called before the first frame update
    void Start()
    {
        if(audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void showNarrative()
    {
        audioSource.PlayOneShot(showNarrativeSFX);
    }
    public void closeNarrative()
    {
        audioSource.PlayOneShot(closeNarrativeSFX);
    }
}
