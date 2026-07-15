using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using Cinemachine;


public class VolumeClickEffect : MonoBehaviour
{
    [Header("控制的 Volume")]
    [SerializeField] private Volume targetVolume;


    [Header("Volume 整体持续时间")]
    [SerializeField] private float volumeDuration = 2f;


    [Header("Volume变化曲线")]
    [Tooltip("X轴 = 时间比例(0~1)，Y轴 = Volume权重(0~1)")]
    [SerializeField] private AnimationCurve volumeCurve =
        new AnimationCurve(
            new Keyframe(0f, 0f),
            new Keyframe(0.5f, 1f),
            new Keyframe(1f, 0f)
        );


    [Header("镜头抖动")]
    [SerializeField] private CinemachineImpulseSource impulseSource;


    [Header("音效")]
    [SerializeField] private AudioSource audioSource;

    [SerializeField] private AudioClip[] clips;


    [Header("射线检测")]
    [SerializeField] private Camera raycastCamera;

    [SerializeField] private float maxRayDistance = 100f;


    [Header("点击后触发按钮")]
    [SerializeField] private ButtonMoveCamera upButton;


    [Header("触发按钮延迟")]
    [SerializeField] private float buttonDelay = 1f;

    private Camera mainCamera;

    private bool isPlaying = false;



    private void Awake()
    {



        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }


        if (impulseSource == null)
        {
            impulseSource = GetComponent<CinemachineImpulseSource>();
        }
        if(targetVolume != null)
        {
            targetVolume.weight = 0f;
        }
    }

    private void Start()
    {
        if (raycastCamera != null)
        {
            mainCamera = raycastCamera;
        }
        else
        {
            mainCamera = Camera.main;
        }
    }



    private void Update()
    {
        bool tapped =
            (Mouse.current != null &&
             Mouse.current.leftButton.wasPressedThisFrame)
             ||
            (Touchscreen.current != null &&
             Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
             ||
            MobileInputManager.InteractPressed;


        if (tapped)
        {
            DetectClick();

            MobileInputManager.ConsumeInteractPressed();
        }
    }



    private void DetectClick()
    {
        if(mainCamera == null)
        {
            Debug.Log("没有找到射线检测相机");
            return;
        }


        Vector2 screenPosition =
            Mouse.current != null
            ?
            Mouse.current.position.ReadValue()
            :
            (
                Touchscreen.current != null
                ?
                (Vector2)Touchscreen.current.primaryTouch.position.ReadValue()
                :
                Vector2.zero
            );



        Ray ray = mainCamera.ScreenPointToRay(screenPosition);



        if (Physics.Raycast(ray, out RaycastHit hit, maxRayDistance))
        {
            Debug.Log("Clicked : " + hit.collider.name);


            if (hit.collider.gameObject == gameObject)
            {
                StartVolumeEffect();
            }
        }
    }



    public void StartVolumeEffect()
    {
        if (isPlaying)
        {
            return;
        }


        StartCoroutine(VolumeEffectCoroutine());
        StartCoroutine(TriggerButtonAfterDelay());
    }


    private IEnumerator TriggerButtonAfterDelay()
    {
        yield return new WaitForSeconds(buttonDelay);

        if (upButton != null)
        {
            upButton.SwitchToCamera2();
        }
    }
    private IEnumerator VolumeEffectCoroutine()
    {
        isPlaying = true;



        //=========================
        // Cinemachine 镜头震动
        //=========================

        if (impulseSource != null)
        {
            impulseSource.GenerateImpulse();
        }



        //=========================
        // 播放音效
        //=========================

        if (audioSource != null)
        {
            foreach (AudioClip clip in clips)
            {
                if (clip != null)
                {
                    audioSource.PlayOneShot(clip);
                }
            }
        }



        //=========================
        // Volume 曲线控制
        //=========================

        float timer = 0f;


        while (timer < volumeDuration)
        {
            timer += Time.deltaTime;


            // 当前时间比例
            float normalizedTime = timer / volumeDuration;


            // 完全由曲线决定权重
            float volumeValue =
                volumeCurve.Evaluate(normalizedTime);



            if (targetVolume != null)
            {
                targetVolume.weight = volumeValue;
            }


            yield return null;
        }



        // 确保最终状态正确
        if (targetVolume != null)
        {
            targetVolume.weight =
                volumeCurve.Evaluate(1f);
        }


        //=========================
        // 延迟触发按钮
        //=========================



        isPlaying = false;
    }
}