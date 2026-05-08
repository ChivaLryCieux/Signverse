using UnityEngine;

[RequireComponent(typeof(PlayerCC))]
public class ClimbGroundInvincibility : MonoBehaviour
{
    [Header("引用")]
    public PlayerCC controller;

    [Header("检测参数")]
    public float rayDistance = 1.5f;
    public LayerMask groundLayer;

    [Header("无敌时长")]
    public float invincibleTime = 0.2f;

    private bool wasGroundedHit;

    void Awake()
    {
        if (controller == null)
        {
            controller = GetComponent<PlayerCC>();
        }
    }

    void Update()
    {
        if (controller == null || controller.IsDead)
        {
            return;
        }

        // 只在攀爬状态执行
        if (!controller.IsPosture(PlayerCC.Posture.Climbing))
        {
            
            return;
        }

        Transform t = controller.GetControlTransform();

        // 从角色位置向下射线
        Ray ray = new Ray(t.position, Vector3.down);

        bool hitGround = Physics.Raycast(ray, rayDistance, groundLayer);

        Debug.DrawRay(t.position, Vector3.down * rayDistance, hitGround ? Color.green : Color.red);

        // ⭐关键：只在“第一次接触到地面”时触发
        if (hitGround)
        {
            controller.isClimbInvincible = true;
        }
        else   controller.isClimbInvincible = false;

        
    }
}