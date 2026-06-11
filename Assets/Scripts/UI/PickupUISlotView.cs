using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;

public class PickupUISlotView : MonoBehaviour,
    IPointerClickHandler, IPointerDownHandler,
    IBeginDragHandler, IDragHandler, IEndDragHandler,
    IPointerEnterHandler, IPointerExitHandler
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

    [Header("点击音效")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private List<AudioClip> clickSFXList = new List<AudioClip>();

    private PickupUIController owner;
    private SlotRole role;
    private PickupItemId itemId;
    private int equippedIndex = -1;
    private Vector3 baseScale = Vector3.one;
    private bool initialized;
    private bool hasItem;
    private bool hasBaseScale;
    private bool selected;
    private bool wasDragging;

    private void Awake()
    {
        if (iconImage == null)
        {
            iconImage = GetComponent<Image>();
        }
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
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

    public void SetIconVisualVisible(bool visible)
    {
        if (iconImage == null)
        {
            return;
        }

        Color color = iconImage.color;
        color.a = visible && iconImage.sprite != null ? 1f : 0f;
        iconImage.color = color;
    }

    public Vector2 GetIconSize()
    {
        if (iconImage != null)
        {
            return iconImage.rectTransform.rect.size;
        }

        if (transform is RectTransform rectTransform)
        {
            return rectTransform.rect.size;
        }

        return new Vector2(64f, 64f);
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

    // ── 拖动接口 ──

    public void OnPointerDown(PointerEventData eventData)
    {
        wasDragging = false;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!initialized || owner == null || eventData.button != PointerEventData.InputButton.Left)
        {
            return;
        }

        wasDragging = true;

        Vector3 sourcePos = GetIconScreenPosition();

        if (role == SlotRole.Unlock && hasItem)
        {
            owner.BeginDragFromUnlockSlot(itemId, sourcePos);
        }
        else if (role == SlotRole.Equipped && hasItem && owner.IsEquippedSlotUnlocked(equippedIndex))
        {
            owner.BeginDragFromEquippedSlot(equippedIndex, sourcePos);
        }
        else
        {
            wasDragging = false;
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        // 拖动跟随由 PickupUIController.UpdateDrag 处理
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!initialized || owner == null || eventData.button != PointerEventData.InputButton.Left)
        {
            return;
        }

        if (wasDragging)
        {
            owner.EndDrag(eventData.position);
        }
    }

    // ── 点击接口（仅保留右键详情面板）──

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!initialized || owner == null)
        {
            return;
        }

        // 左键拖动结束后不再触发点击
        if (eventData.button == PointerEventData.InputButton.Left && wasDragging)
        {
            wasDragging = false;
            return;
        }

        // 右键打开详情面板
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            if (role == SlotRole.Unlock && hasItem)
            {
                owner.ShowDetailPanel(itemId);
            }

            return;
        }
    }

    // ── 悬停接口 ──

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!initialized)
        {
            return;
        }

        bool slotUnlocked = role != SlotRole.Equipped || owner == null || owner.IsEquippedSlotUnlocked(equippedIndex);
        bool interactive = slotUnlocked && hasItem;
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

    // ── 内部方法 ──

    private Vector3 GetIconScreenPosition()
    {
        if (iconImage != null && iconImage.rectTransform != null)
        {
            Vector3[] corners = new Vector3[4];
            iconImage.rectTransform.GetWorldCorners(corners);
            return (corners[0] + corners[2]) * 0.5f;
        }

        return transform.position;
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
