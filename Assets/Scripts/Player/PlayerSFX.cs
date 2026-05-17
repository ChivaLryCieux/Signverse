using System.Collections.Generic;
using UnityEngine;
using static PlayerCC;
public class PlayerSFX : MonoBehaviour
{

    public PlayerCC controller;

    public Posture currentPosture;
    private PlayerCC.Posture lastPosture;
    [Header("落地音效冷却（秒）")]

    public float landingCooldown = 0.15f;

    private float lastPlayTime = -999f;
    [Header("落地音效 - 默认/其他")]
    public List<AudioClip> otherLandClip = new List<AudioClip>();

    [Header("落地音效 - 自然")]
    public List<AudioClip> natureLandClip = new List<AudioClip>();

    [Header("落地音效 - 水面")]
    public List<AudioClip> waterLandClip = new List<AudioClip>();


    [Header("脚步音列表（默认/其他）")]
    public List<AudioClip> otherStepClip = new List<AudioClip>();

    [Header("自然地面")]
    public List<AudioClip> natureStepClip = new List<AudioClip>();

    [Header("水面")]
    public List<AudioClip> waterStepClip = new List<AudioClip>();
    [Header("攀爬触碰音效")]
    public AudioClip climbClip;
    public AudioClip vaultClip;
    
    [Header("AudioSource")]
    public AudioSource audioSource;

    [Header("随机范围控制（可选）")]
    public float minPitch = 0.95f;
    public float maxPitch = 1.05f;

    [Header("地面检测")]
    public float rayDistance = 1.5f;
    public LayerMask groundLayer;

    void Awake()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
        if (controller != null)
        {
            lastPosture = controller.CurrentPosture;
        }
    }
    void Update()
    {
        HandleLandingSFX();
    }



    void HandleLandingSFX()
    {
        if (controller == null || audioSource == null)
        {
            return;
        }

        PlayerCC.Posture current = controller.CurrentPosture;

        // 只在这一帧触发：Airborne -> Grounded
        if (lastPosture == PlayerCC.Posture.Airborne &&
            current == PlayerCC.Posture.Grounded)
        {
            if (Time.time - lastPlayTime >= landingCooldown)
            {
                List<AudioClip> landingList = GetLandingListBySurface();

                if (landingList != null && landingList.Count > 0)
                {
                    int index = Random.Range(0, landingList.Count);
                    AudioClip clip = landingList[index];

                    if (clip != null)
                    {
                        audioSource.pitch = Random.Range(minPitch, maxPitch);
                        audioSource.PlayOneShot(clip);
                    }
                }

                lastPlayTime = Time.time;
            }
        }

        lastPosture = current;
    }
    // =========================
    // 脚步声播放（带地面检测）
    // =========================
    public void PlayFootstep()
    {
        if (audioSource == null)
            return;

        List<AudioClip> targetList = GetFootstepListBySurface();

        if (targetList == null || targetList.Count == 0)
            return;

        int index = Random.Range(0, targetList.Count);
        AudioClip clip = targetList[index];

        if (clip == null)
            return;

        audioSource.pitch = Random.Range(minPitch, maxPitch);
        audioSource.PlayOneShot(clip);
    }

    // =========================
    // 向下射线检测地面类型
    // =========================
    private List<AudioClip> GetFootstepListBySurface()
    {
        RaycastHit hit;

        Vector3 origin = transform.position + Vector3.up * 0.1f;


        if (Physics.Raycast(origin, Vector3.down, out hit, rayDistance, groundLayer))
        {
            string tag = hit.collider.tag;
            Debug.Log(tag);
            if (tag == "Nature")
            {
                return natureStepClip;
            }
            else if (tag == "Water")
            {
                return waterStepClip;
            }
            else
            {
                return otherStepClip;
            }
        }

        // 没打到地面时 fallback
        return otherStepClip;
    }


    private List<AudioClip> GetLandingListBySurface()
    {
        RaycastHit hit;
        Vector3 origin = transform.position + Vector3.up * 0.1f;

        if (Physics.Raycast(origin, Vector3.down, out hit, rayDistance, groundLayer))
        {
            string tag = hit.collider.tag;

            if (tag == "Nature")
            {
                return natureLandClip;
            }
            else if (tag == "Water")
            {
                return waterLandClip;
            }
            else
            {
                return otherLandClip;
            }
        }

        return otherLandClip;
    }


    public void PlayClimbingSFX()
    {
        if (audioSource == null)
            return;
        audioSource.pitch = Random.Range(minPitch, maxPitch);
        audioSource.PlayOneShot(climbClip);
    }
    public void PlayVautingSFX()
    {
        if (audioSource == null)
            return;
        audioSource.pitch = Random.Range(minPitch, maxPitch);
        audioSource.PlayOneShot(vaultClip);
    }

    
    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;

        Vector3 origin = transform.position + Vector3.up * 0.1f;
        Vector3 direction = Vector3.down * rayDistance;

        Gizmos.DrawLine(origin, origin + direction);
    }
}