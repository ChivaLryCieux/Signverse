using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class FloatingObject : MonoBehaviour
{
    [Header("浮动参数")]
    public float amplitude = 0.3f;
    public float speed = 1.0f;

    [Header("随机化")]
    public bool randomizeSpeed = true;
    public Vector2 speedRange = new Vector2(0.7f, 1.3f);

    private Material runtimeMaterial;
    private Material originalMaterial;
    private static Shader floatingShader;

    void Awake()
    {
        // 随机化速度
        if (randomizeSpeed)
        {
            speed = Random.Range(speedRange.x, speedRange.y);
        }

        // 获取飘动 Shader - 使用新的名称
        if (floatingShader == null)
        {
            floatingShader = Shader.Find("Custom/FloatingSimple");

            if (floatingShader == null)
            {
                Debug.LogError("找不到 Custom/FloatingSimple Shader！请检查 Shader 文件名和编译状态。");
                return;
            }
        }

        Renderer renderer = GetComponent<Renderer>();
        if (renderer == null)
        {
            Debug.LogError($"物体 {gameObject.name} 没有 Renderer 组件！");
            return;
        }

        originalMaterial = renderer.sharedMaterial;

        // 创建新材质
        runtimeMaterial = new Material(floatingShader);

        // 复制原材质的关键属性
        if (originalMaterial != null)
        {
            // 复制颜色
            if (originalMaterial.HasProperty("_Color"))
                runtimeMaterial.SetColor("_Color", originalMaterial.GetColor("_Color"));

            // 复制主贴图
            if (originalMaterial.HasProperty("_MainTex"))
                runtimeMaterial.SetTexture("_MainTex", originalMaterial.GetTexture("_MainTex"));
        }
        else
        {
            // 如果没有原材质，设置默认颜色为白色
            runtimeMaterial.SetColor("_Color", Color.white);
        }

        // 设置飘动参数
        runtimeMaterial.SetFloat("_Amplitude", amplitude);
        runtimeMaterial.SetFloat("_Speed", speed);

        // 应用材质
        renderer.material = runtimeMaterial;

        Debug.Log($"{gameObject.name} 已添加浮动效果 | 幅度: {amplitude:F2} | 速度: {speed:F2}");
    }

    void OnDestroy()
    {
        if (runtimeMaterial != null)
        {
            Destroy(runtimeMaterial);
        }
    }
}