using UnityEngine;

public class FloatingObjectTransform : MonoBehaviour
{
    [Header("浮动参数")]
    public float amplitude = 0.3f;
    public float speed = 1.0f;

    [Header("随机化")]
    public bool randomizeSpeed = true;
    public float minSpeed = 0.7f;
    public float maxSpeed = 1.3f;
    public Vector2 speedRange;
    public bool randomizeAmplitude = true;
    public float minAmplitude = 0.2f;
    public float maxAmplitude = 0.5f;
    public Vector2 amplitudeRange;

    [Header("相位偏移")]
    public float phaseOffset = 0f;
    public bool randomizePhase = true;

    private Vector3 startPosition;
    private float currentSpeed;
    private float currentAmplitude;
    private float currentPhase;

    void Start()
    {
        startPosition = transform.position;
    
        SetValue();

    }

    void SetValue()
    {

        speedRange = new Vector2(minSpeed, maxSpeed);
        amplitudeRange = new Vector2(minAmplitude, maxAmplitude);
        currentSpeed = randomizeSpeed ? Random.Range(speedRange.x, speedRange.y) : speed;
        currentAmplitude = randomizeAmplitude ? Random.Range(amplitudeRange.x, amplitudeRange.y) : amplitude;
        currentPhase = randomizePhase ? Random.Range(0f, Mathf.PI * 2) : phaseOffset;
    }
    void Update()
    {
  
        // 纯正弦波：最自然的浮动
        float t = Time.time * currentSpeed + currentPhase;
        float sinValue = Mathf.Sin(t);

        float offsetY = sinValue * currentAmplitude;

        transform.position = new Vector3(
            startPosition.x,
            startPosition.y + offsetY,
            startPosition.z
        );
    }

    void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying)
        {
            Gizmos.color = Color.green;
            Vector3 topPos = transform.position + Vector3.up * amplitude;
            Vector3 bottomPos = transform.position + Vector3.down * amplitude;
            Gizmos.DrawWireSphere(topPos, 0.1f);
            Gizmos.DrawWireSphere(bottomPos, 0.1f);
            Gizmos.DrawLine(topPos, bottomPos);
        }
    }
}