using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class CartoonPanelController : MonoBehaviour, IPointerClickHandler
{
    private static CartoonPanelController activePanel;

    [Header("Panel")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Image cartoonImage;
    [SerializeField] private Button startGameButton;
    [SerializeField] private bool playOnStart = true;
    [SerializeField] private bool hideWhenFinished = true;

    [Header("Images")]
    [SerializeField] private Sprite[] pictures;
    [SerializeField] private bool preserveImageAspect = true;

    [Header("Fade")]
    [SerializeField] private float fadeDuration = 0.35f;
    [SerializeField] private bool useUnscaledTime = true;

    [Header("Player Lock")]
    [SerializeField] private PlayerCC player;
    [SerializeField] private bool disablePlayerInput = true;
    [SerializeField] private bool controlCursor = true;

    private CanvasGroup panelCanvasGroup;
    private Coroutine transitionCoroutine;
    private int currentIndex;
    private bool isShowing;
    private bool isTransitioning;
    private bool playerInputWasDisabledByPanel;
    private bool previousCursorVisible;
    private CursorLockMode previousCursorLockState;
    private bool cursorWasChangedByPanel;

    public bool IsShowing => isShowing;
    public bool IsOnLastPicture => pictures != null && pictures.Length > 0 && currentIndex >= pictures.Length - 1;
    public static bool IsPlaying => activePanel != null && activePanel.isShowing;

    private void Reset()
    {
        panelRoot = gameObject;
        panelCanvasGroup = GetComponent<CanvasGroup>();
        cartoonImage = GetComponentInChildren<Image>(true);
        startGameButton = GetComponentInChildren<Button>(true);
    }

    private void Awake()
    {
        ResolveReferences();
        SetupStartButton();

        if (playOnStart)
        {
            Show();
        }
        else
        {
            HideImmediate();
        }
    }

    private void OnDestroy()
    {
        RestorePlayerInput();
        RestoreCursor();
        if (activePanel == this)
        {
            activePanel = null;
        }
    }

    private void Update()
    {
        if (!isShowing || isTransitioning || IsOnLastPicture)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            PlayNextPicture();
        }
    }

    public void Show()
    {
        ResolveReferences();

        if (pictures == null || pictures.Length == 0)
        {
            Debug.LogWarning("CartoonPanelController 没有配置 pictures，开场漫画不会播放。", this);
            HideImmediate();
            return;
        }

        currentIndex = 0;
        isShowing = true;
        activePanel = this;
        SetPanelVisible(true);
        SetStartButtonVisible(false);
        SetPicture(currentIndex, 0f);
        LockPlayerInput();

        if (transitionCoroutine != null)
        {
            StopCoroutine(transitionCoroutine);
        }

        transitionCoroutine = StartCoroutine(ShowFirstPictureCoroutine());
    }

    public void FinishAndStartGame()
    {
        if (!isShowing)
        {
            return;
        }

        if (transitionCoroutine != null)
        {
            StopCoroutine(transitionCoroutine);
            transitionCoroutine = null;
        }

        StartCoroutine(FinishCoroutine());
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!isShowing || isTransitioning || pictures == null || pictures.Length == 0)
        {
            return;
        }

        if (IsOnLastPicture)
        {
            return;
        }

        PlayNextPicture();
    }

    private void PlayNextPicture()
    {
        if (transitionCoroutine != null)
        {
            StopCoroutine(transitionCoroutine);
        }

        transitionCoroutine = StartCoroutine(SwitchPictureCoroutine());
    }

    private IEnumerator ShowFirstPictureCoroutine()
    {
        isTransitioning = true;
        yield return FadeImage(1f);

        isTransitioning = false;
        transitionCoroutine = null;

        if (IsOnLastPicture)
        {
            SetStartButtonVisible(true);
        }
    }

    private IEnumerator SwitchPictureCoroutine()
    {
        isTransitioning = true;
        SetStartButtonVisible(false);

        yield return FadeImage(0f);

        currentIndex = Mathf.Min(currentIndex + 1, pictures.Length - 1);
        SetPicture(currentIndex, 0f);

        yield return FadeImage(1f);

        isTransitioning = false;
        transitionCoroutine = null;

        if (IsOnLastPicture)
        {
            SetStartButtonVisible(true);
        }
    }

    private IEnumerator FinishCoroutine()
    {
        isTransitioning = true;
        SetStartButtonVisible(false);
        yield return FadePanel(0f);

        if (hideWhenFinished)
        {
            SetPanelVisible(false);
        }

        RestorePlayerInput();
        RestoreCursor();
        isShowing = false;
        isTransitioning = false;

        if (activePanel == this)
        {
            activePanel = null;
        }
    }

    private IEnumerator FadeImage(float targetAlpha)
    {
        if (cartoonImage == null)
        {
            yield break;
        }

        float startAlpha = cartoonImage.color.a;
        float duration = Mathf.Max(0.01f, fadeDuration);
        float timer = 0f;

        while (timer < duration)
        {
            timer += GetDeltaTime();
            SetImageAlpha(Mathf.Lerp(startAlpha, targetAlpha, timer / duration));
            yield return null;
        }

        SetImageAlpha(targetAlpha);
    }

    private IEnumerator FadePanel(float targetAlpha)
    {
        if (panelCanvasGroup == null)
        {
            yield break;
        }

        float startAlpha = panelCanvasGroup.alpha;
        float duration = Mathf.Max(0.01f, fadeDuration);
        float timer = 0f;

        while (timer < duration)
        {
            timer += GetDeltaTime();
            panelCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, timer / duration);
            yield return null;
        }

        panelCanvasGroup.alpha = targetAlpha;
    }

    private void SetPicture(int index, float alpha)
    {
        if (cartoonImage == null || pictures == null || index < 0 || index >= pictures.Length)
        {
            return;
        }

        cartoonImage.sprite = pictures[index];
        cartoonImage.preserveAspect = preserveImageAspect;
        SetImageAlpha(alpha);
    }

    private void SetImageAlpha(float alpha)
    {
        if (cartoonImage == null)
        {
            return;
        }

        Color color = cartoonImage.color;
        color.a = Mathf.Clamp01(alpha);
        cartoonImage.color = color;
    }

    private void LockPlayerInput()
    {
        if (!disablePlayerInput)
        {
            return;
        }

        ResolvePlayer();

        if (player == null)
        {
            Debug.LogWarning("CartoonPanelController 没有找到 PlayerCC，无法锁定玩家输入。", this);
            return;
        }

        player.DisableInput();
        player.ClearMovementLocks();
        playerInputWasDisabledByPanel = true;
    }

    private void RestorePlayerInput()
    {
        if (!playerInputWasDisabledByPanel)
        {
            return;
        }

        if (player != null)
        {
            player.EnableInput();
        }

        playerInputWasDisabledByPanel = false;
    }

    private void ResolveReferences()
    {
        if (panelRoot == null)
        {
            panelRoot = gameObject;
        }

        panelCanvasGroup = panelRoot.GetComponent<CanvasGroup>();
        if (panelCanvasGroup == null)
        {
            panelCanvasGroup = panelRoot.AddComponent<CanvasGroup>();
        }

        if (cartoonImage == null)
        {
            cartoonImage = panelRoot.GetComponentInChildren<Image>(true);
        }

        if (startGameButton == null)
        {
            startGameButton = panelRoot.GetComponentInChildren<Button>(true);
        }

        ResolvePlayer();
    }

    private void ResolvePlayer()
    {
        if (player != null && player.gameObject.activeInHierarchy)
        {
            return;
        }

        GameObject taggedPlayer = GameObject.FindGameObjectWithTag("Player");
        if (taggedPlayer != null)
        {
            player = taggedPlayer.GetComponentInParent<PlayerCC>();
            if (player == null)
            {
                player = taggedPlayer.GetComponentInChildren<PlayerCC>(true);
            }
        }

        if (player == null)
        {
            player = FindObjectOfType<PlayerCC>(true);
        }
    }

    private void SetupStartButton()
    {
        if (startGameButton == null)
        {
            return;
        }

        startGameButton.onClick.RemoveListener(FinishAndStartGame);
        startGameButton.onClick.AddListener(FinishAndStartGame);
        SetStartButtonVisible(false);
    }

    private void SetPanelVisible(bool visible)
    {
        if (panelRoot != null && !panelRoot.activeSelf && visible)
        {
            panelRoot.SetActive(true);
        }

        if (panelCanvasGroup != null)
        {
            panelCanvasGroup.alpha = visible ? 1f : 0f;
            panelCanvasGroup.interactable = visible;
            panelCanvasGroup.blocksRaycasts = visible;
        }

        if (panelRoot != null && !visible)
        {
            panelRoot.SetActive(false);
        }

        if (controlCursor && visible)
        {
            if (!cursorWasChangedByPanel)
            {
                previousCursorVisible = Cursor.visible;
                previousCursorLockState = Cursor.lockState;
            }

            cursorWasChangedByPanel = true;
            Cursor.visible = visible;
            Cursor.lockState = CursorLockMode.None;
        }
    }

    private void SetStartButtonVisible(bool visible)
    {
        if (startGameButton == null)
        {
            return;
        }

        startGameButton.gameObject.SetActive(visible);
        startGameButton.interactable = visible;
    }

    private void HideImmediate()
    {
        isShowing = false;
        isTransitioning = false;
        if (activePanel == this)
        {
            activePanel = null;
        }

        SetStartButtonVisible(false);
        SetPanelVisible(false);
        RestorePlayerInput();
        RestoreCursor();
    }

    private float GetDeltaTime()
    {
        return useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
    }

    private void RestoreCursor()
    {
        if (!controlCursor || !cursorWasChangedByPanel)
        {
            return;
        }

        Cursor.visible = previousCursorVisible;
        Cursor.lockState = previousCursorLockState;
        cursorWasChangedByPanel = false;
    }
}
