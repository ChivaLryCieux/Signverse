using UnityEngine;

public class TriggerHideObject : MonoBehaviour
{
    [Header("隐藏设置")]
    [Tooltip("要隐藏的物体（可以拖多个）")]
    public GameObject[] objectsToHide;  // 改成数组，支持隐藏多个物体

    [Header("触发设置")]
    [Tooltip("是否只触发一次")]
    public bool triggerOnce = true;

    [Tooltip("是否在离开时重新显示")]
    public bool showOnExit = false;

    private bool hasTriggered = false;

    private void OnTriggerEnter(Collider other)
    {
        // 检查进入的是不是玩家（通过标签判断）
        if (other.CompareTag("Player"))
        {
            if (triggerOnce && hasTriggered) return;

            HideObjects(true);
            hasTriggered = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // 离开触发器时是否重新显示
        if (showOnExit && other.CompareTag("Player"))
        {
            HideObjects(false);
        }
    }

    private void HideObjects(bool hide)
    {
        foreach (GameObject obj in objectsToHide)
        {
            if (obj != null)
            {
                obj.SetActive(!hide);  // hide=true时设为false，反之亦然
            }
        }

        // 可选：打印调试信息
        Debug.Log($"触发隐藏：{objectsToHide.Length}个物体，隐藏状态：{hide}");
    }

    // 可选：在Scene视图中显示触发器范围
    private void OnDrawGizmos()
    {
        BoxCollider box = GetComponent<BoxCollider>();
        if (box != null)
        {
            Gizmos.color = new Color(1, 0, 0, 0.3f);
            Gizmos.DrawCube(transform.position + box.center, box.size);
        }
        else
        {
            Gizmos.color = new Color(1, 0, 0, 0.3f);
            Gizmos.DrawWireCube(transform.position, Vector3.one);
        }
    }
}