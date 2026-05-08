using System.Collections;
using UnityEngine;

[RequireComponent(typeof(PlayerCC))]
[RequireComponent(typeof(CharacterController))]
public class PlayerDeath : MonoBehaviour
{

    private const string HazardousTag = "Hazardous";

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
    private PlayerCameraRespawnReset cameraRespawnReset;
    private float airStartY;
    private bool wasGrounded;
    private float invincibleUntil;
    private int deathBlockRequestFrame = -1;
    private bool deathBlockedThisFrame;

    public bool IsDead => isDead;
    public bool IsInvincible => Time.time < invincibleUntil ||
                                (deathBlockRequestFrame >= 0 && Time.frameCount - deathBlockRequestFrame <= 1);
    public bool WasDeathBlockedThisFrame => deathBlockedThisFrame;

    private void Awake()
    {
        controller = GetComponent<PlayerCC>();
        characterController = GetComponent<CharacterController>();
        cameraRespawnReset = GetComponent<PlayerCameraRespawnReset>();
        if (cameraRespawnReset == null)
        {
            cameraRespawnReset = gameObject.AddComponent<PlayerCameraRespawnReset>();
        }

        currentCheckpoint = transform.position;
    }

    private void LateUpdate()
    {
        deathBlockedThisFrame = false;

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
        Die(false);
    }

    public void ForceDie()
    {
        Die(true);
    }

    private void Die(bool ignoreInvincibility)
    {
        if (isDead)
        {
            return;
        }

        if (!ignoreInvincibility && IsInvincible)
        {
            deathBlockedThisFrame = true;
            return;
        }

        isDead = true;
        controller.SetVerticalVelocity(0f);
        controller.SetClimbState(false, 0f);
        controller.isGrounded = false;
        controller.ClearMovementLocks();
        controller.SetInputEnabled(false);
        controller.SetDeathPresentationActive(true);
        characterController.enabled = false;

        Debug.Log($"<color=red>角色死亡！{respawnDelay:F1} 秒后将在存档点复活。</color>");
        StartCoroutine(RespawnAfterDelay());
    }

    public void Kill()
    {
        Die();
    }

    public void RequestDeathBlock()
    {
        deathBlockRequestFrame = Time.frameCount;
    }

    public void GrantInvincibility(float duration)
    {
        invincibleUntil = Mathf.Max(invincibleUntil, Time.time + Mathf.Max(0f, duration));
    }

    public bool ConsumeDeathBlockedThisFrame()
    {
        if (!deathBlockedThisFrame)
        {
            return false;
        }

        deathBlockedThisFrame = false;
        return true;
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        TryDieFromHazard(hit.collider);
    }

    private void OnTriggerEnter(Collider other)
    {
        TryDieFromHazard(other);
    }

    private void OnCollisionEnter(Collision collision)
    {
        TryDieFromHazard(collision.collider);
    }

    private void TryDieFromHazard(Collider hazardCollider)
    {
        if (isDead || hazardCollider == null || !hazardCollider.CompareTag(HazardousTag))
        {
            return;
        }

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
                if(controller.isClimbInvincible) return;
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
        Vector3 previousPosition = transform.position;
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
        invincibleUntil = 0f;
        deathBlockRequestFrame = -1;
        deathBlockedThisFrame = false;

        characterController.enabled = true;
        controller.SetDeathPresentationActive(false);
        controller.SetInputEnabled(true);
        ResetRespawnCamera(previousPosition, respawnPosition);

        Debug.Log("<color=cyan>角色已在存档点复活。</color>");
    }

    private void ResetRespawnCamera(Vector3 previousPosition, Vector3 respawnPosition)
    {
        if (cameraRespawnReset == null)
        {
            cameraRespawnReset = GetComponent<PlayerCameraRespawnReset>();
        }

        if (cameraRespawnReset != null)
        {
            cameraRespawnReset.ResetAfterRespawn(previousPosition, respawnPosition);
        }
    }
}
