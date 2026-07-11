using System.Collections;
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

    [Header("磁吸 / 悬停增强")]
    [Tooltip("拖拽时图标压在装备槽上，目标槽的放大倍数。")]
    [SerializeField] private float dropTargetScale = 1.15f;
    [Tooltip("悬停或被磁吸时图标向上抬起的像素，作用于 iconImage，不影响布局。")]
    [SerializeField] private float hoverLift = 6f;
    [Tooltip("拾取解锁时图标淡入弹出的时长（秒）。")]
    [SerializeField] private float appearDuration = 0.25f;

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
    private bool hovering;
    private bool isDropTarget;
    private Vector2 baseIconPos;
    private bool hasBaseIconPos;
    private Coroutine appearRoutine;

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
        CacheBaseIconPos();
        EnsureRaycastTarget();
        SetHighlight(false);
    }

    public void SetSelected(bool value)
    {
        selected = value;
        ApplyVisual();
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
        hovering = false;
        isDropTarget = false;

        CacheBaseScale();
        CacheBaseIconPos();
        EnsureRaycastTarget();
        SetIcon(icon);

        ApplyVisual();
    }

    public void InitializeEquippedSlot(PickupUIController controller, int index)
    {
        owner = controller;
        role = SlotRole.Equipped;
        equippedIndex = index;
        initialized = true;
        hasItem = false;
        selected = false;
        hovering = false;
        isDropTarget = false;

        CacheBaseScale();
        CacheBaseIconPos();
        EnsureRaycastTarget();
        ClearIcon();
        ApplyVisual();
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

        if (iconImage != null)
        {
            iconImage.sprite = emptySprite;
            iconImage.enabled = true;
            Color color = iconImage.color;
            color.a = emptySprite != null ? 1f : 0f;
            iconImage.color = color;
        }

        ApplyVisual();
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

        Vector3 sourcePos = GetIconScreenPosition();

        if (role == SlotRole.Unlock && hasItem)
        {
            if (owner.BeginDragFromUnlockSlot(itemId, sourcePos))
            {
                wasDragging = true;
                PlayClickSfx();
            }
            else
            {
                wasDragging = false;
            }
        }
        else if (role == SlotRole.Equipped && hasItem && owner.IsEquippedSlotUnlocked(equippedIndex))
        {
            if (owner.BeginDragFromEquippedSlot(equippedIndex, sourcePos))
            {
                wasDragging = true;
                PlayClickSfx();
            }
            else
            {
                wasDragging = false;
            }
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

        // 模仿目标选择状态：左键点击直接确认目标（纯点击路径）
        if (eventData.button == PointerEventData.InputButton.Left && role == SlotRole.Unlock && hasItem)
        {
            if (owner.TryCompleteMimicTargetSelection(itemId))
            {
                wasDragging = false;
                return;
            }
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

        hovering = true;
        ApplyVisual();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        hovering = false;
        ApplyVisual();
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

    // 拖拽磁吸：controller 检测到浮动图标压在本槽上时调用，切换磁吸高亮。
    public void SetDropTarget(bool value)
    {
        isDropTarget = value;
        ApplyVisual();
    }

    // 拾取解锁后由 controller 调用，图标原地淡入 + 弹出。
    public void PlayUnlockAppearAnimation()
    {
        if (iconImage == null || appearDuration <= 0f)
        {
            return;
        }

        if (appearRoutine != null)
        {
            StopCoroutine(appearRoutine);
        }

        appearRoutine = StartCoroutine(AppearRoutine());
    }

    private IEnumerator AppearRoutine()
    {
        CacheBaseIconPos();

        Color color = iconImage.color;
        float elapsed = 0f;

        while (elapsed < appearDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(elapsed / appearDuration);

            // alpha 线性淡入
            color.a = p;
            iconImage.color = color;

            // scale：从 0.6 -> 1.0，带轻微 overshoot（先冲到 ~1.08 再回落）
            float overshoot = Mathf.Sin(p * Mathf.PI) * 0.08f;
            transform.localScale = baseScale * (0.6f + 0.4f * p + overshoot);

            yield return null;
        }

        // 精确落点
        color.a = 1f;
        iconImage.color = color;
        appearRoutine = null;
        ApplyVisual();
    }

    // 统一视觉状态：缩放（磁吸 > 悬停/选中 > 默认）+ 图标上浮 + 高亮。
    private void ApplyVisual()
    {
        float scaleMul = 1f;
        if (isDropTarget)
        {
            scaleMul = dropTargetScale;
        }
        else if (hovering || selected)
        {
            scaleMul = hoverScale;
        }

        transform.localScale = baseScale * scaleMul;

        if (iconImage != null && iconImage.rectTransform != null && hasBaseIconPos)
        {
            bool lift = hovering || isDropTarget;
            iconImage.rectTransform.anchoredPosition = baseIconPos + (lift ? Vector2.up * hoverLift : Vector2.zero);
        }

        SetHighlight(hovering || selected || isDropTarget);
    }

    private void CacheBaseIconPos()
    {
        if (hasBaseIconPos || iconImage == null || iconImage.rectTransform == null)
        {
            return;
        }

        baseIconPos = iconImage.rectTransform.anchoredPosition;
        hasBaseIconPos = true;
    }

    private void PlayClickSfx()
    {
        if (audioSource == null)
        {
            return;
        }

        if (clickSFXList == null || clickSFXList.Count == 0)
        {
            return;
        }

        AudioClip clip = clickSFXList[Random.Range(0, clickSFXList.Count)];
        if (clip == null)
        {
            return;
        }

        audioSource.PlayOneShot(clip);
    }
}
