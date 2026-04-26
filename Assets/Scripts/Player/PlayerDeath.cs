using System.Collections;
using UnityEngine;

[RequireComponent(typeof(PlayerCC))]
[RequireComponent(typeof(CharacterController))]
public class PlayerDeath : MonoBehaviour
{
    [Header("摔死检测")]
    public float deathDistance = 8.0f;
    public float respawnDelay = 3.0f;

    [Header("地面检测")]
    public float groundCheckDistance = 0.3f;
    public LayerMask groundLayer;

    [Header("重生状态")]
    [SerializeField] private Vector3 currentCheckpoint;
    [SerializeField] private bool isDead;

    private PlayerCC controller;
    private CharacterController characterController;
    private float airStartY;
    private bool wasGrounded;

    public bool IsDead => isDead;

    private void Awake()
    {
        controller = GetComponent<PlayerCC>();
        characterController = GetComponent<CharacterController>();
        currentCheckpoint = transform.position;
    }

    private void LateUpdate()
    {
        if (isDead)
        {
            return;
        }

        CheckElectricFloor();
        HandleFallDeath();
    }

    public void SetCheckpoint(Vector3 checkpointPosition)
    {
        currentCheckpoint = checkpointPosition;
        Debug.Log($"<color=green>已更新存档点：</color>{currentCheckpoint}");
    }

    public void Die()
    {
        if (isDead)
        {
            return;
        }

        isDead = true;
        controller.SetVerticalVelocity(0f);
        controller.SetClimbState(false, 0f);
        controller.isGrounded = false;
        controller.ClearMovementLocks();
        controller.SetInputEnabled(false);
        characterController.enabled = false;

        Debug.Log($"<color=red>角色死亡！{respawnDelay:F1} 秒后将在存档点复活。</color>");
        StartCoroutine(RespawnAfterDelay());
    }

    public void Kill()
    {
        Die();
    }

    private void CheckElectricFloor()
    {
        if (!Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, groundCheckDistance, groundLayer))
        {
            return;
        }

        ElectricFloor floor = hit.collider.GetComponent<ElectricFloor>();
        if (floor != null && floor.isElectrified)
        {
            Die();
        }
    }

    private void HandleFallDeath()
    {
        bool isGrounded = characterController != null && characterController.isGrounded;

        if (wasGrounded && !isGrounded)
        {
            airStartY = transform.position.y;
        }

        if (!wasGrounded && isGrounded)
        {
            float fallHeight = airStartY - transform.position.y;
            if (fallHeight > deathDistance)
            {
                Die();
            }
        }

        wasGrounded = isGrounded;
    }

    private IEnumerator RespawnAfterDelay()
    {
        yield return new WaitForSeconds(respawnDelay);
        RespawnAtCheckpoint();
    }

    private void RespawnAtCheckpoint()
    {
        Vector3 respawnPosition = new Vector3(currentCheckpoint.x, currentCheckpoint.y, 0f);

        transform.position = respawnPosition;
        transform.forward = Vector3.right;

        controller.SetFacing(Vector3.right);
        controller.SetVerticalVelocity(-2f);
        controller.SetClimbState(false, 0f);
        controller.ClearMovementLocks();

        airStartY = respawnPosition.y;
        wasGrounded = false;
        isDead = false;

        characterController.enabled = true;
        controller.SetInputEnabled(true);

        Debug.Log("<color=cyan>角色已在存档点复活。</color>");
    }
}
