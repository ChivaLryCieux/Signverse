using System.Collections;
using UnityEngine;

public class ElectricFloor : MonoBehaviour
{
    [Header("时间设置")]

    [Tooltip("冷却时间（安全时间）")]
    public float cooldownTime = 2f;

    [Tooltip("通电持续时间（危险时间）")]
    public float electrifiedTime = 0.3f;


    [Header("状态（调试用）")]

    [Tooltip("当前是否通电")]
    public bool isElectrified = false;


    [Header("可选视觉效果")]

    Renderer targetRenderer;

    public Material electrifiedMat;
    Material[] defaultMats;  // 改为数组存储所有默认材质


    [Header("可选音效")]

    AudioSource audioSource;

    public AudioClip electrifiedSFX;
    public AudioClip chargeElectricitySFX;


    // 保存原始tag
    private string originalTag;


    void Start()
    {
        targetRenderer = GetComponent<Renderer>();
        audioSource = GetComponentInChildren<AudioSource>();

        // 保存所有原始材质
        defaultMats = targetRenderer.materials;

        // 保存原始tag
        originalTag = gameObject.tag;

        StartCoroutine(ElectricLoop());
    }


    IEnumerator ElectricLoop()
    {
        while (true)
        {
            // 安全阶段
            SetElectrified(false);

            yield return new WaitForSeconds(cooldownTime);
            audioSource.Stop();

            // 通电阶段
            SetElectrified(true);

            yield return new WaitForSeconds(electrifiedTime);
            audioSource.Stop();
        }
    }


    void SetElectrified(bool state)
    {
        isElectrified = state;

        // 视觉反馈 - 修改所有材质
        if (targetRenderer != null)
        {
            if (isElectrified)
            {
                // 将所有材质设置为闪光材质
                Material[] newMats = new Material[defaultMats.Length];
                for (int i = 0; i < defaultMats.Length; i++)
                {
                    newMats[i] = electrifiedMat;
                }
                targetRenderer.materials = newMats;
            }
            else
            {
                // 恢复所有原始材质
                targetRenderer.materials = defaultMats;
            }
        }

        // ===== 新增：Tag切换逻辑 =====
        if (isElectrified)
        {
            // 通电时设置为 Hazardous
            gameObject.tag = "Hazardous";
        }
        else
        {
            // 安全时恢复原始tag
            gameObject.tag = originalTag;
        }
        // ============================

        // 音效反馈
        if (isElectrified)
        {
            audioSource.PlayOneShot(electrifiedSFX);
        }
        else
        {
            audioSource.PlayOneShot(chargeElectricitySFX);
        }
    }
}