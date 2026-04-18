using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PickupUISlotView : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("显示")]
    [SerializeField] private Image iconImage;
    [SerializeField] private GameObject highlight;
    [SerializeField] private float hoverScale = 1.08f;

    private PickupUIController owner;
    private PickupItemId itemId;
    private Vector3 baseScale = Vector3.one;
    private bool initialized;

    private void Awake()
    {
        baseScale = transform.localScale;
        SetHighlight(false);
    }

    public void Initialize(PickupUIController controller, PickupItemId id, Sprite icon)
    {
        owner = controller;
        itemId = id;
        initialized = true;
        baseScale = transform.localScale;

        if (iconImage != null)
        {
            iconImage.sprite = icon;
            iconImage.enabled = icon != null;
        }

        SetHighlight(false);
    }

    public void SetIcon(Sprite icon)
    {
        if (iconImage == null)
        {
            return;
        }

        iconImage.sprite = icon;
        iconImage.enabled = icon != null;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!initialized || owner == null || eventData.button != PointerEventData.InputButton.Left)
        {
            return;
        }

        owner.Equip(itemId);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!initialized)
        {
            return;
        }

        transform.localScale = baseScale * hoverScale;
        SetHighlight(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        transform.localScale = baseScale;
        SetHighlight(false);
    }

    private void SetHighlight(bool visible)
    {
        if (highlight != null)
        {
            highlight.SetActive(visible);
        }
    }
}
