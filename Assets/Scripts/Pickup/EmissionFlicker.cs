using UnityEngine;

public class EmissionFlicker : MonoBehaviour
{
    [Header("闪烁参数")]
    public Color emissionColor = Color.white;
    public float minIntensity = 0f;
    public float maxIntensity = 5f;
    public float speed = 2f;

    [Header("闪烁起始偏移（关键）")]
    public float startOffset = 0f;

    private Renderer rend;
    private Material mat;

    void Start()
    {
        rend = GetComponent<Renderer>();

        // 避免影响共享材质
        mat = rend.material;

        // 开启 emission
        mat.EnableKeyword("_EMISSION");

        // 如果你想“随机起点”
        if (startOffset == 0f)
        {
            startOffset = Random.Range(0f, 100f);
        }
    }

    void Update()
    {
        float time = Time.time + startOffset;

        float intensity = Mathf.Lerp(
            minIntensity,
            maxIntensity,
            (Mathf.Sin(time * speed) + 1f) * 0.5f
        );

        mat.SetColor("_EmissionColor", emissionColor * intensity);
    }
}