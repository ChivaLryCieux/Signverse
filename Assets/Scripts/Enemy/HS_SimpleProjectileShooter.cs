using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters;
using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class HS_SimpleProjectileShooter : MonoBehaviour
{
    [Header("子弹效果")]
    public GameObject projectilePrefab;
    public AudioClip fireSFX;

    [Header("发射点")]
    public Transform firePoint;

    [Header("调控触发")]
    public bool fire = false;

    [Range(0.01f, 1f)]
    public float fireRate = 0.1f;

    private float fireTimer = 0f;

    [Header("测试按键")]
    public KeyCode testKey = KeyCode.I; // Inspector里配置

    [Header("相机抖动动画")]
    public Animation camAnim;

    [Header("索敌检测引用")]
    public EnemyLookAtPlayer detector; // 引用你的检测脚本

    [Tooltip("蓄力时间")]
    public float chargeTime = 1.5f;

    // 当前蓄力计时
    private float chargeTimer = 0f;

    AudioSource audioSource;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    void Update()
    {
        fireTimer -= Time.deltaTime;

        // 测试输入（Inspector可选键）
        if (Input.GetKeyDown(testKey))
        {
            fire = true;
        }

        HandleAutoFire();

        
        if (fire && fireTimer <= 0f)
        {
            Shoot();

            fireTimer = fireRate;

            // 发射一次后关闭
            fire = false;
        }
    }

    void Shoot()
    {
        if (projectilePrefab == null || firePoint == null)
        {
            Debug.LogWarning("ProjectilePrefab 或 FirePoint 未设置");
            return;
        }

        Instantiate( projectilePrefab, firePoint.position, firePoint.rotation );
        audioSource.PlayOneShot(fireSFX);

        if (camAnim != null)
        {
            camAnim.Play();
        }
    }

    void HandleAutoFire()
{
    if (detector == null)
        return;

    // 是否检测到玩家
    bool playerDetected = detector.playerInRange;

    if (playerDetected)
    {
        // 开始蓄力（进入范围立刻开始）
        chargeTimer += Time.deltaTime;

        // 蓄力完成
        if (chargeTimer >= chargeTime)
        {
            fire = true;

            // 重置蓄力 → 进入下一轮循环
            chargeTimer = 0f;
        }
    }
    else
    {
        // 玩家离开 → 清空蓄力
        chargeTimer = 0f;
    }
}
}