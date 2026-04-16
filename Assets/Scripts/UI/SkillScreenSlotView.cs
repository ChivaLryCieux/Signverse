using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SkillScreenSlotView : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    public enum SlotRole
    {
        Backpack,
        Config
    }

    public enum SlotColumn
    {
        Main,
        Sub
    }

    [Header("通用")]
    [SerializeField] private Image iconImage;
    [SerializeField] private GameObject highlight;
    [SerializeField] private float selectedScale = 1.08f;

    [Header("配置栏")]
    [SerializeField] private Sprite emptyConfigSprite;

    [Header("交互")]
    [SerializeField] private float doubleClickInterval = 0.28f;

    public SlotRole Role { get; private set; }
    public SlotColumn Column { get; private set; }
    public int RowIndex { get; private set; }

    private SkillPauseUIController owner;
    private Sprite backpackNormalSprite;
    private Sprite backpackSelectedSprite;
    private Vector3 baseScale = Vector3.one;
    private float lastLeftClickTime = -10f;
    private bool isSelected;

    private void Awake()
    {
        baseScale = transform.localScale;
        SetSelected(false);
    }

    public void Initialize(SkillPauseUIController controller, SlotRole role, int rowIndex, SlotColumn column)
    {
        owner = controller;
        Role = role;
        RowIndex = rowIndex;
        Column = column;

        baseScale = transform.localScale;
        SetSelected(false);

        if (Role == SlotRole.Config && emptyConfigSprite != null)
        {
            iconImage.sprite = emptyConfigSprite;
        }
    }

    public void SetBackpackSprites(Sprite normalSprite, Sprite selectedSprite)
    {
        backpackNormalSprite = normalSprite;
        backpackSelectedSprite = selectedSprite;

        if (iconImage != null)
        {
            iconImage.sprite = backpackNormalSprite;
        }
    }

    public void SetConfigSprite(string skillId, Sprite sprite)
    {
        if (iconImage != null)
        {
            iconImage.sprite = sprite;
        }
    }

    public void ClearConfigSprite()
    {
        if (iconImage != null)
        {
            iconImage.sprite = emptyConfigSprite;
        }
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected;
        transform.localScale = selected ? baseScale * selectedScale : baseScale;

        if (Role == SlotRole.Backpack && iconImage != null)
        {
            if (selected && backpackSelectedSprite != null)
            {
                iconImage.sprite = backpackSelectedSprite;
            }
            else
            {
                iconImage.sprite = backpackNormalSprite;
            }
        }

        if (highlight != null)
        {
            highlight.SetActive(selected);
        }
    }

    public void SetVisible(bool visible)
    {
        gameObject.SetActive(visible);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (owner == null)
        {
            return;
        }

        if (eventData.button == PointerEventData.InputButton.Left)
        {
            float now = Time.unscaledTime;
            if (now - lastLeftClickTime <= doubleClickInterval)
            {
                owner.OnSlotDoubleClick(this);
            }
            else
            {
                owner.OnSlotLeftClick(this);
            }

            lastLeftClickTime = now;
            return;
        }

        if (eventData.button == PointerEventData.InputButton.Right)
        {
            owner.OnSlotRightClick(this);
            return;
        }

        if (eventData.button == PointerEventData.InputButton.Middle)
        {
            owner.OnSlotMiddleClick(this);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (highlight != null && !isSelected)
        {
            highlight.SetActive(true);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (highlight != null && !isSelected)
        {
            highlight.SetActive(false);
        }
    }
}
