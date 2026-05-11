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

    private GameObject extraObject;

    private bool playerInRange;

    private GameObject GetRecordBG()
    {
        if (RecordBG.Instance != null)
        {
            return RecordBG.Instance.gameObject;
        }

        RecordBG record = FindObjectOfType<RecordBG>();
        return record != null ? record.gameObject : null;
    }


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



    

    // 默认关闭
    if (extraObject != null)
    {
        extraObject.SetActive(false);
    }
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

        // ⭐E键：关闭详情
        if (panelController.IsDetailOpenFor(this))
        {
            panelController.HideFromTrigger(this);

            // ⭐同步关闭
            if (extraObject != null)
            {
                extraObject.SetActive(false);
            }

            return;
        }


        // ⭐E键：打开详情
        panelController.ShowDetail(this, background, bodyText);

        // ⭐同步关闭（关键：交互成功瞬间）
        if (extraObject != null)
        {
            extraObject.SetActive(false);
        }
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

        extraObject = GetRecordBG();

        // ⭐进入时显示
        if (extraObject != null)
        {
            extraObject.SetActive(true);
        }
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

        extraObject = GetRecordBG();
        // ⭐离开时关闭
        if (extraObject != null)
        {
            extraObject.SetActive(false);
        }
    }

    private bool WasInteractPressed()
    {
        return !CartoonPanelController.IsPlaying &&
               Keyboard.current != null &&
               Keyboard.current.eKey.wasPressedThisFrame;
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