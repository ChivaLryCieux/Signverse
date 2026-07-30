using System.Collections;
using UnityEngine;


[RequireComponent(typeof(InteractionPanelTrigger))]
public class AutoOpenInteractionPanel : MonoBehaviour
{
    private InteractionPanelTrigger trigger;


    private bool opened;



    private void Awake()
    {
        trigger =
            GetComponent<InteractionPanelTrigger>();
    }



    private void OnTriggerEnter(Collider other)
    {
        if(opened)
            return;


        if(other.GetComponentInParent<PlayerCC>() == null)
        {
            return;
        }


        opened = true;


        StartCoroutine(
            AutoOpen()
        );
    }



    private IEnumerator AutoOpen()
    {
        // 等原 InteractionPanelTrigger 的 ShowFixed 执行结束
        yield return null;



        InteractionPanelController controller =
            InteractionPanelController.Instance;



        if(controller == null)
        {
            controller =
                FindObjectOfType<InteractionPanelController>();
        }



        if(controller == null)
        {
            Debug.LogWarning(
                "AutoOpenInteractionPanel: 找不到 InteractionPanelController"
            );

            yield break;
        }



        controller.ShowDetail(
            trigger,
            trigger.GetBackground(),
            trigger.GetBodyText()
        );

    }

}