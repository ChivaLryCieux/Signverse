using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Collider))]
public class CipherPanelTrigger : MonoBehaviour
{
    [Header("Cipher Panel")]
    [SerializeField] private CipherPanelController cipherPanel;
    [SerializeField] private bool closePanelOnExit = true;
    [SerializeField] private bool hidePromptAfterUnlocked = true;

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
        ResolveCipherPanel();
    }

    private void OnEnable()
    {
        ResolveCipherPanel();

        if (cipherPanel != null)
        {
            cipherPanel.Unlocked += HandleUnlocked;
        }
    }

    private void OnDisable()
    {
        if (cipherPanel != null)
        {
            cipherPanel.Unlocked -= HandleUnlocked;
        }
    }

    private void Update()
    {
        if (!playerInRange || !WasInteractPressed())
        {
            return;
        }

        ResolveCipherPanel();

        if (cipherPanel == null)
        {
            Debug.LogWarning("CipherPanelTrigger 没有找到 CipherPanelController，无法打开 CipherPanel。", this);
            return;
        }

        if (cipherPanel.IsUnlocked)
        {
            cipherPanel.ShowPrompt(false);
            return;
        }

        cipherPanel.Toggle();
        cipherPanel.ShowPrompt(!cipherPanel.IsOpen);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponentInParent<PlayerCC>() == null)
        {
            return;
        }

        playerInRange = true;
        ResolveCipherPanel();

        if (cipherPanel == null)
        {
            Debug.LogWarning("CipherPanelTrigger 没有找到 CipherPanelController，无法显示解锁提示。", this);
            return;
        }

        cipherPanel.ShowPrompt(!cipherPanel.IsUnlocked || !hidePromptAfterUnlocked);
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.GetComponentInParent<PlayerCC>() == null)
        {
            return;
        }

        playerInRange = false;

        if (cipherPanel == null)
        {
            return;
        }

        cipherPanel.ShowPrompt(false);

        if (closePanelOnExit)
        {
            cipherPanel.Close();
        }
    }

    private void HandleUnlocked()
    {
        if (hidePromptAfterUnlocked && cipherPanel != null)
        {
            cipherPanel.ShowPrompt(false);
        }
    }

    private bool WasInteractPressed()
    {
        return !CartoonPanelController.IsPlaying &&
               Keyboard.current != null &&
               Keyboard.current.eKey.wasPressedThisFrame;
    }

    private void ResolveCipherPanel()
    {
        if (cipherPanel != null)
        {
            return;
        }

        cipherPanel = FindObjectOfType<CipherPanelController>();
    }
}
