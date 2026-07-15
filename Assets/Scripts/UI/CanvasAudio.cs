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
    public AudioClip pauseButtonSFX;

    [Header("联动特效音效")]
    public AudioClip linkedDiamondLineSFX;
    public AudioClip linkedDiamondBgSFX;

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
    public void PlayPauseButtonSFX()
    {
        audioSource.PlayOneShot(pauseButtonSFX);
    }

    public void PlayLinkedDiamondLineSFX()
    {
        if (linkedDiamondLineSFX != null)
        {
            audioSource.PlayOneShot(linkedDiamondLineSFX);
        }
    }

    public void PlayLinkedDiamondBgSFX()
    {
        if (linkedDiamondBgSFX != null)
        {
            audioSource.PlayOneShot(linkedDiamondBgSFX);
        }
    }
}
