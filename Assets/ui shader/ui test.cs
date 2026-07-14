using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class MaterialTransitionController : MonoBehaviour
{
    [Header("材质设置")]
    [Tooltip("要控制的材质")]
    public Material targetMaterial;

    [Header("过渡时间设置")]
    [Tooltip("按O后，开始过渡的延迟时间（秒）")]
    public float transitionToMagicStartDelay = 2f;
    [Tooltip("按O后，完成过渡的总时间（秒）")]
    public float transitionToMagicEndTime = 4f;

    [Tooltip("按P后，开始过渡的延迟时间（秒）")]
    public float transitionToNormalStartDelay = 0f;
    [Tooltip("按P后，完成过渡的总时间（秒）")]
    public float transitionToNormalEndTime = 2f;

    [Header("调试信息")]
    [SerializeField] private float currentProgress = 0f;
    [SerializeField] private bool isTransitioning = false;
    [SerializeField] private bool isGoingToMagic = true;
    [SerializeField] private float elapsedTime = 0f;
    [SerializeField] private float startDelay = 0f;
    [SerializeField] private float endTime = 0f;
    [SerializeField] private float startProgress = 0f;
    [SerializeField] private float endProgress = 0f;

    private void Start()
    {
        // 初始化进度为0（起始态）
        if (targetMaterial != null)
        {
            targetMaterial.SetFloat("_Progress", 0f);
            currentProgress = 0f;
        }
        else
        {
            Debug.LogError("目标材质未设置！");
        }
    }

    private void Update()
    {
        // ===== 检查按键输入（同时兼容新旧输入系统） =====
        bool oKeyPressed = false;
        bool pKeyPressed = false;

#if ENABLE_INPUT_SYSTEM
        // 新版 Input System
        oKeyPressed = Keyboard.current != null && Keyboard.current.oKey.wasPressedThisFrame;
        pKeyPressed = Keyboard.current != null && Keyboard.current.pKey.wasPressedThisFrame;
#else
        // 旧版 Input Manager
        oKeyPressed = Input.GetKeyDown(KeyCode.O);
        pKeyPressed = Input.GetKeyDown(KeyCode.P);
#endif

        if (oKeyPressed)
        {
            StartTransitionToMagic();
        }
        else if (pKeyPressed)
        {
            StartTransitionToNormal();
        }

        // 更新过渡动画
        if (isTransitioning && targetMaterial != null)
        {
            elapsedTime += Time.deltaTime;

            float progress = 0f;

            if (elapsedTime < startDelay)
            {
                // 延迟阶段：保持当前进度
                progress = startProgress;
            }
            else if (elapsedTime >= endTime)
            {
                // 结束阶段：到达目标
                progress = endProgress;
            }
            else
            {
                // 过渡阶段：从 startDelay 到 endTime 之间线性插值
                float t = (elapsedTime - startDelay) / (endTime - startDelay);
                // 使用平滑插值让过渡更自然
                float smoothT = Mathf.SmoothStep(0f, 1f, t);
                progress = Mathf.Lerp(startProgress, endProgress, smoothT);
            }

            // 应用到材质
            currentProgress = progress;
            targetMaterial.SetFloat("_Progress", progress);

            // 检查是否完成
            if (elapsedTime >= endTime)
            {
                isTransitioning = false;
                currentProgress = endProgress;
                targetMaterial.SetFloat("_Progress", currentProgress);

                Debug.Log($"过渡完成！当前进度: {currentProgress:F2}，总耗时: {elapsedTime:F2}秒");
            }
        }
    }

    /// <summary>
    /// 开始过渡到魔幻态（按O触发）
    /// </summary>
    public void StartTransitionToMagic()
    {
        if (targetMaterial == null) return;

        // 如果已经在过渡中，先完成当前过渡
        if (isTransitioning)
        {
            // 立即设置到当前目标
            targetMaterial.SetFloat("_Progress", endProgress);
            currentProgress = endProgress;
        }

        // 设置过渡参数
        isGoingToMagic = true;
        startProgress = currentProgress;
        endProgress = 1f;
        elapsedTime = 0f;
        startDelay = transitionToMagicStartDelay;
        endTime = transitionToMagicEndTime;

        isTransitioning = true;

        Debug.Log($"按O：延迟 {startDelay:F2}秒后开始过渡，在 {endTime:F2}秒完成，从 {startProgress:F2} 到 {endProgress:F2}");
    }

    /// <summary>
    /// 开始过渡到起始态（按P触发）
    /// </summary>
    public void StartTransitionToNormal()
    {
        if (targetMaterial == null) return;

        // 如果已经在过渡中，先完成当前过渡
        if (isTransitioning)
        {
            // 立即设置到当前目标
            targetMaterial.SetFloat("_Progress", endProgress);
            currentProgress = endProgress;
        }

        // 设置过渡参数
        isGoingToMagic = false;
        startProgress = currentProgress;
        endProgress = 0f;
        elapsedTime = 0f;
        startDelay = transitionToNormalStartDelay;
        endTime = transitionToNormalEndTime;

        isTransitioning = true;

        Debug.Log($"按P：延迟 {startDelay:F2}秒后开始过渡，在 {endTime:F2}秒完成，从 {startProgress:F2} 到 {endProgress:F2}");
    }

    /// <summary>
    /// 立即设置到指定进度（0-1）
    /// </summary>
    public void SetProgress(float progress)
    {
        if (targetMaterial == null) return;

        progress = Mathf.Clamp01(progress);
        currentProgress = progress;
        targetMaterial.SetFloat("_Progress", progress);

        // 停止过渡
        isTransitioning = false;

        Debug.Log($"手动设置进度到: {progress:F2}");
    }

    /// <summary>
    /// 获取当前进度（0-1）
    /// </summary>
    public float GetCurrentProgress()
    {
        return currentProgress;
    }

    /// <summary>
    /// 是否正在过渡中
    /// </summary>
    public bool IsTransitioning()
    {
        return isTransitioning;
    }

    // 在Inspector中显示当前状态
    private void OnValidate()
    {
        // 如果材质存在，更新进度显示
        if (targetMaterial != null && !isTransitioning)
        {
            currentProgress = targetMaterial.GetFloat("_Progress");
        }
    }
}