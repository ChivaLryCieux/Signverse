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
    Material defaultMat;


    [Header("可选音效")]

    AudioSource audioSource;

    public AudioClip electrifiedSFX;
    public AudioClip chargeElectricitySFX;


    void Start()
    {
        targetRenderer = GetComponent<Renderer>();
        audioSource = GetComponent<AudioSource>();

        defaultMat = targetRenderer.material;

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

        // 视觉反馈
        if (targetRenderer != null)
        {
            if (isElectrified)
                targetRenderer.material = electrifiedMat;
            else
                targetRenderer.material = defaultMat;
        }

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