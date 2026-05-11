using UnityEngine;

public class DoorPickup : MonoBehaviour
{
    private bool playerInside;
    private PlayerCC currentPlayer;

    [Header("进入Trigger时显示的物体")]
    public GameObject objectToShow;

    void Update()
    {
        // 只有玩家在Trigger内部并且有PlayerCC才处理按键
        if (!playerInside || currentPlayer == null)
            return;

        // 按E拾取
        if (!CartoonPanelController.IsPlaying && Input.GetKeyDown(KeyCode.E))
        {
            currentPlayer.hasDoorPickup = true;
            Debug.Log("picked door");

            // 拾取后隐藏Pickup
            gameObject.SetActive(false);

            // 拾取后可选择隐藏显示物体
            if (objectToShow != null)
                objectToShow.SetActive(false);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerCC player = other.GetComponent<PlayerCC>();

        if (player == null)
            return;

        playerInside = true;
        currentPlayer = player;

        // 进入Trigger时立即显示物体
        if (objectToShow != null)
            objectToShow.SetActive(true);
    }

    private void OnTriggerExit(Collider other)
    {
        PlayerCC player = other.GetComponent<PlayerCC>();

        if (player == null)
            return;

        playerInside = false;
        currentPlayer = null;

        // 离开Trigger时可隐藏物体
        if (objectToShow != null)
            objectToShow.SetActive(false);
    }
}
