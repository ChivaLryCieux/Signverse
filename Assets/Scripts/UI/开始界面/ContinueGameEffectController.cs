using UnityEngine;

public class ContinueGameEffectController : MonoBehaviour
{
    [Header("动画")]
    [SerializeField] private Animator animator;

    [Header("音效")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip newGameSFX;


    [SerializeField] private SavePanelController savePanelController;


    private void Awake()
    {
        

        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
    }


    private void OnEnable()
    {
        if (savePanelController != null)
        {
            savePanelController.OnContinueGameEvent.AddListener(PlayContinueGameEffect);
        }
    }


    private void OnDisable()
    {
        if (savePanelController != null)
        {
            savePanelController.OnContinueGameEvent.RemoveListener(PlayContinueGameEffect);
        }
    }


    public void PlayContinueGameEffect()
    {
        if (animator != null)
        {
            animator.SetTrigger("Toggle");
        }

        if (audioSource != null && newGameSFX != null)
        {
            audioSource.PlayOneShot(newGameSFX);
        }
    }
}