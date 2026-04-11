using Skills;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Collider))]
public class Skill3DSlot : MonoBehaviour
{
    [Header("Visuals")]
    [SerializeField] private GameObject emptyVisual;
    [SerializeField] private GameObject equippedVisual;
    [SerializeField] private GameObject highlight;
    [SerializeField] private Renderer targetRenderer;
    [SerializeField] private string shaderHighlightProperty = "_Highlight";
    [SerializeField] private float selectedScale = 1.08f;

    public int Index { get; private set; }
    public SkillBase EquippedSkill { get; private set; }
    public bool HasSkill => EquippedSkill != null;

    private SkillPauseUIController controller;
    private MaterialPropertyBlock propertyBlock;
    private Vector3 originalScale;

    private void Awake()
    {
        originalScale = transform.localScale;
        propertyBlock = new MaterialPropertyBlock();
        SetHighlighted(false);
    }

    public void Initialize(SkillPauseUIController owner, int index)
    {
        controller = owner;
        Index = index;
        originalScale = transform.localScale;
        if (propertyBlock == null)
        {
            propertyBlock = new MaterialPropertyBlock();
        }
        SetSelected(false);
        SetHighlighted(false);
    }

    public void SetSkill(SkillBase skill)
    {
        EquippedSkill = skill;
        RefreshVisuals();
    }

    public void ClearSkill()
    {
        EquippedSkill = null;
        RefreshVisuals();
        SetSelected(false);
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

        if (targetRenderer != null && !string.IsNullOrEmpty(shaderHighlightProperty))
        {
            targetRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetFloat(shaderHighlightProperty, enabled ? 1f : 0f);
            targetRenderer.SetPropertyBlock(propertyBlock);
        }
    }

    private void RefreshVisuals()
    {
        if (emptyVisual != null)
        {
            emptyVisual.SetActive(!HasSkill);
        }

        if (equippedVisual != null)
        {
            equippedVisual.SetActive(HasSkill);
        }
    }

    private void OnMouseEnter()
    {
        controller?.OnEquipSlotHover(this);
    }

    private void OnMouseExit()
    {
        controller?.OnEquipSlotExit(this);
    }

    private void OnMouseDown()
    {
        controller?.OnEquipSlotLeftClicked(this);
    }

    private void OnMouseOver()
    {
        if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
        {
            controller?.OnEquipSlotRightClicked(this);
        }
    }
}
