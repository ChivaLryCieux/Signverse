using UnityEngine;

public class NewGameEffectController : MonoBehaviour
{
    [Header("动画")]
    [SerializeField] private Animator animator;

    [Header("音效")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip[] newGameSFX = new AudioClip[0];

    [SerializeField] private SavePanelController savePanelController;


    private void Awake()
    {
        GameObject.DontDestroyOnLoad(this.gameObject);//此处是否需要保持eventsystem存疑
        


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
            savePanelController.OnNewGameEvent.AddListener(PlayNewGameEffect);
        }
    }


    private void OnDisable()
    {
        if (savePanelController != null)
        {
            savePanelController.OnNewGameEvent.RemoveListener(PlayNewGameEffect);
        }
    }


    public void PlayNewGameEffect()
    {
        if (animator != null)
        {
            animator.SetTrigger("Toggle");
        }

        if (audioSource != null && newGameSFX != null)
        {
            foreach (var clip in newGameSFX)
            {
                audioSource.PlayOneShot(clip);
            }
        }
    }
}