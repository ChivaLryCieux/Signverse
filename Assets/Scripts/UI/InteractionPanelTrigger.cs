using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Collider))]
public class InteractionPanelTrigger : MonoBehaviour
{
    [Header("Panel 内容")]
    [SerializeField] private Sprite background;
    [SerializeField] [TextArea(3, 8)] private string bodyText;

    [Header("Panel 控制器")]
    [SerializeField] private InteractionPanelController panelController;
    [SerializeField] private bool hidePanelOnExit = true;

    private bool playerInRange;

    private void Reset()
    {
        Collider triggerCollider = GetComponent<Collider>();
        triggerCollider.isTrigger = true;
    }

    private void OnValidate()
    {
        Collider triggerCollider = GetComponent<Collider>();
        if (triggerCollider != null)
        {
            triggerCollider.isTrigger = true;
        }
    }

    private void Awake()
    {
        ResolvePanelController();
    }

    private void Update()
    {
        if (!playerInRange || !WasInteractPressed())
        {
            return;
        }

        ResolvePanelController();

        if (panelController == null)
        {
            Debug.LogWarning("InteractionPanelTrigger 没有找到 InteractionPanelController，无法显示 Panel。", this);
            return;
        }

        if (panelController.IsDetailOpenFor(this))
        {
            panelController.HideFromTrigger(this);
            return;
        }

        panelController.ShowDetail(this, background, bodyText);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponentInParent<PlayerCC>() == null)
        {
            return;
        }

        playerInRange = true;
        ResolvePanelController();

        if (panelController == null)
        {
            Debug.LogWarning("InteractionPanelTrigger 没有找到 InteractionPanelController，无法显示 Panel。", this);
            return;
        }

        panelController.ShowFixed(this);
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.GetComponentInParent<PlayerCC>() == null)
        {
            return;
        }

        playerInRange = false;

        if (hidePanelOnExit && panelController != null)
        {
            panelController.HideFromTrigger(this);
        }
    }

    private bool WasInteractPressed()
    {
        return Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;
    }

    private void ResolvePanelController()
    {
        if (panelController != null)
        {
            return;
        }

        panelController = InteractionPanelController.Instance;

        if (panelController == null)
        {
            panelController = FindObjectOfType<InteractionPanelController>();
        }
    }
}
