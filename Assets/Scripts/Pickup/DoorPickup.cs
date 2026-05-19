using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

public class DoorPickup : MonoBehaviour
{
    private bool playerInside;
    private PlayerCC currentPlayer;

    public AudioClip doorCardAchieved;
    [Range (0 ,1)]
    public float volume;

    [Header("进入 Trigger 时显示的提示 Panel")]
    [SerializeField, FormerlySerializedAs("objectToShow")]
    private GameObject promptPanel;
    public GameObject showUIObject;

    void Awake()
    {

    }
    void Update()
    {
        

        
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerCC player = other.GetComponent<PlayerCC>();

        if (player == null)
            return;

        playerInside = true;
        currentPlayer = player;

        SetPromptPanelVisible(true);

        
        
    }

    void OnTriggerStay(Collider other)
    {
        if (Input.GetKeyDown(KeyCode.E) || Keyboard.current.eKey.wasPressedThisFrame)
        {
            
            currentPlayer.hasDoorPickup = true;
            Debug.Log("picked door");
            if(showUIObject != null)
            { 
                Debug.Log("获得开门标识！");
                showUIObject.SetActive(true);
            }

            SetPromptPanelVisible(false);
            AudioSFXManager.Instance.PlaySFX(doorCardAchieved , volume);


            gameObject.SetActive(false);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        PlayerCC player = other.GetComponent<PlayerCC>();

        if (player == null)
            return;

        playerInside = false;
        currentPlayer = null;

        SetPromptPanelVisible(false);
    }

    private void SetPromptPanelVisible(bool visible)
    {
        ResolvePromptPanel();
        if (promptPanel != null)
        {
            promptPanel.SetActive(visible);
        }
    }

    private void ResolvePromptPanel()
    {
        if (promptPanel == null || promptPanel.GetComponent<TMP_Text>() == null)
        {
            return;
        }

        Transform parent = promptPanel.transform.parent;
        if (parent != null)
        {
            promptPanel = parent.gameObject;
        }
    }
}
