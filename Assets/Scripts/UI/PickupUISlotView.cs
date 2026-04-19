using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PickupUISlotView : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    public enum SlotRole
    {
        Unlock,
        Equipped
    }

    [Header("显示")]
    [SerializeField] private Image iconImage;
    [SerializeField] private Sprite emptySprite;
    [SerializeField] private GameObject highlight;
    [SerializeField] private float hoverScale = 1.08f;

    private PickupUIController owner;
    private SlotRole role;
    private PickupItemId itemId;
    private int equippedIndex = -1;
    private Vector3 baseScale = Vector3.one;
    private bool initialized;
    private bool hasItem;
    private bool hasBaseScale;
    private bool selected;

    private void Awake()
    {
        if (iconImage == null)
        {
            iconImage = GetComponent<Image>();
        }

        CacheBaseScale();
        EnsureRaycastTarget();
        SetHighlight(false);
    }

    public void SetSelected(bool value)
    {
        selected = value;
        transform.localScale = selected ? baseScale * hoverScale : baseScale;
        SetHighlight(selected);
    }

    public void InitializeUnlockSlot(PickupUIController controller, PickupItemId id, Sprite icon)
    {
        owner = controller;
        role = SlotRole.Unlock;
        itemId = id;
        equippedIndex = -1;
        initialized = true;
        hasItem = true;
        selected = false;

        CacheBaseScale();
        transform.localScale = baseScale;
        EnsureRaycastTarget();
        SetIcon(icon);

        SetHighlight(false);
    }

    public void InitializeEquippedSlot(PickupUIController controller, int index)
    {
        owner = controller;
        role = SlotRole.Equipped;
        equippedIndex = index;
        initialized = true;
        hasItem = false;
        selected = false;

        CacheBaseScale();
        transform.localScale = baseScale;
        EnsureRaycastTarget();
        ClearIcon();
        SetHighlight(false);
    }

    public void SetItem(PickupItemId id, Sprite icon)
    {
        itemId = id;
        hasItem = true;
        SetIcon(icon);
    }

    public void ClearIcon()
    {
        hasItem = false;
        transform.localScale = baseScale;

        if (iconImage != null)
        {
            iconImage.sprite = emptySprite;
            iconImage.enabled = true;
            Color color = iconImage.color;
            color.a = emptySprite != null ? 1f : 0f;
            iconImage.color = color;
        }

        SetHighlight(false);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!initialized || owner == null || eventData.button != PointerEventData.InputButton.Left)
        {
            return;
        }

        if (role == SlotRole.Unlock)
        {
            if (hasItem)
            {
                owner.SelectForEquip(itemId);
            }

            return;
        }

        owner.OnEquippedSlotClicked(equippedIndex);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!initialized)
        {
            return;
        }

        bool interactive = hasItem || (role == SlotRole.Equipped && owner != null && owner.HasSelectedUnlockItem);
        if (!interactive)
        {
            return;
        }

        transform.localScale = baseScale * hoverScale;
        SetHighlight(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        transform.localScale = selected ? baseScale * hoverScale : baseScale;
        SetHighlight(selected);
    }

    private void SetIcon(Sprite icon)
    {
        if (iconImage == null)
        {
            return;
        }

        iconImage.sprite = icon;
        iconImage.enabled = true;
        Color color = iconImage.color;
        color.a = icon != null ? 1f : 0f;
        iconImage.color = color;
    }

    private void EnsureRaycastTarget()
    {
        if (iconImage != null)
        {
            iconImage.raycastTarget = true;
        }
    }

    private void CacheBaseScale()
    {
        if (hasBaseScale)
        {
            return;
        }

        baseScale = transform.localScale;
        hasBaseScale = true;
    }

    private void SetHighlight(bool visible)
    {
        if (highlight != null)
        {
            highlight.SetActive(visible);
        }
    }
}
