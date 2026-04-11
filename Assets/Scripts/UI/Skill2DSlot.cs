using Skills;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Skill2DSlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("Visuals")]
    [SerializeField] private Image frontImage;
    [SerializeField] private Image backImage;
    [SerializeField] private GameObject emptyState;
    [SerializeField] private GameObject highlight;
    [SerializeField] private float selectedScale = 1.12f;

    public int Index { get; private set; }
    public SkillBase AssignedSkill { get; private set; }
    public bool IsUnlocked { get; private set; }
    public bool IsFaceUp { get; private set; } = true;

    private SkillPauseUIController controller;
    private RectTransform rectTransform;
    private Vector3 originalScale;

    private void Awake()
    {
        rectTransform = transform as RectTransform;
        originalScale = transform.localScale;
        SetHighlighted(false);
    }

    public void Initialize(SkillPauseUIController owner, int index)
    {
        controller = owner;
        Index = index;

        if (rectTransform == null)
        {
            rectTransform = transform as RectTransform;
        }

        originalScale = transform.localScale;
        SetSelected(false);
        SetHighlighted(false);
    }

    public void Assign(SkillBase skill, Sprite frontSprite, Sprite backSprite)
    {
        AssignedSkill = skill;

        if (frontImage != null)
        {
            frontImage.sprite = frontSprite;
        }

        if (backImage != null)
        {
            backImage.sprite = backSprite;
        }

        SetFaceUp(true);
    }

    public void SetUnlocked(bool unlocked)
    {
        IsUnlocked = unlocked;
        gameObject.SetActive(unlocked);

        if (!unlocked)
        {
            SetSelected(false);
            SetHighlighted(false);
        }
    }

    public void SetFaceUp(bool faceUp)
    {
        IsFaceUp = faceUp;

        if (frontImage != null)
        {
            frontImage.gameObject.SetActive(faceUp);
        }

        if (backImage != null)
        {
            backImage.gameObject.SetActive(!faceUp);
        }

        if (emptyState != null)
        {
            emptyState.SetActive(!faceUp);
        }
    }

    public void SetSelected(bool selected)
    {
        transform.localScale = selected ? originalScale * selectedScale : originalScale;
        SetHighlighted(selected);
    }

    public void SetHighlighted(bool enabled)
    {
        if (highlight != null)
        {
            highlight.SetActive(enabled);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        controller?.OnLibrarySlotHover(this);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        controller?.OnLibrarySlotExit(this);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            controller?.OnLibrarySlotClicked(this);
        }
    }
}
