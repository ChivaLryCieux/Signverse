using UnityEngine;

public class CipherPanelKey : MonoBehaviour
{
    [SerializeField] private CipherPanelController panelController;
    [Tooltip("从 1 开始的密码键序号。")]
    [SerializeField] private int keyNumber = 1;

    public void Toggle()
    {
        ResolvePanelController();

        if (panelController != null)
        {
            panelController.ToggleKey(keyNumber - 1);
        }
    }

    private void ResolvePanelController()
    {
        if (panelController != null)
        {
            return;
        }

        panelController = GetComponentInParent<CipherPanelController>();

        if (panelController == null)
        {
            panelController = FindObjectOfType<CipherPanelController>();
        }
    }
}
