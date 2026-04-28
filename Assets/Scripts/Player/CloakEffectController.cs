using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Skills
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource))]
    public class CloakEffectController : MonoBehaviour
    {
        [Header("画面效果")]
        [SerializeField] private Volume cloakVolume;
        [SerializeField] private string fallbackVolumeName = "隐身";
        [SerializeField, Range(0f, 1f)] private float invisibleWeight = 1f;
        [SerializeField, Min(0.01f)] private float weightChangeSpeed = 4f;
        [SerializeField, Min(0.02f)] private float requestTimeout = 0.15f;

        [Header("角色显示")]
        [SerializeField] private bool hideRenderersWhileCloaked = true;

        [Header("音效")]
        [SerializeField] private AudioClip cloakSfx;
        [SerializeField, Range(0f, 1f)] private float cloakSfxVolume = 1f;

        private readonly Dictionary<object, float> activeRequests = new Dictionary<object, float>();
        private Renderer[] cachedRenderers;
        private AudioSource audioSource;
        private bool isCloaked;

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            cachedRenderers = GetComponentsInChildren<Renderer>(true);
            ResolveCloakVolume();

            if (cloakVolume != null)
            {
                cloakVolume.weight = 0f;
            }
        }

        private void OnDisable()
        {
            activeRequests.Clear();
            SetCloaked(false);

            if (cloakVolume != null)
            {
                cloakVolume.weight = 0f;
            }
        }

        private void Update()
        {
            ClearExpiredRequests();
            SetCloaked(activeRequests.Count > 0);
            UpdateVolumeWeight();
        }

        public void RequestCloak(object source, bool active)
        {
            if (source == null)
            {
                source = this;
            }

            if (active)
            {
                activeRequests[source] = Time.time + requestTimeout;
            }
            else
            {
                activeRequests.Remove(source);
            }
        }

        private void SetCloaked(bool cloaked)
        {
            if (isCloaked == cloaked)
            {
                return;
            }

            isCloaked = cloaked;
            ApplyRendererVisibility(!isCloaked);

            if (isCloaked && audioSource != null)
            {
                AudioClip clip = cloakSfx != null ? cloakSfx : audioSource.clip;
                if (clip != null)
                {
                    audioSource.PlayOneShot(clip, cloakSfxVolume);
                }
            }
        }

        private void UpdateVolumeWeight()
        {
            if (cloakVolume == null)
            {
                return;
            }

            float targetWeight = isCloaked ? invisibleWeight : 0f;
            cloakVolume.weight = Mathf.MoveTowards(
                cloakVolume.weight,
                targetWeight,
                weightChangeSpeed * Time.deltaTime
            );
        }

        private void ApplyRendererVisibility(bool visible)
        {
            if (!hideRenderersWhileCloaked)
            {
                return;
            }

            if (cachedRenderers == null || cachedRenderers.Length == 0)
            {
                cachedRenderers = GetComponentsInChildren<Renderer>(true);
            }

            for (int i = 0; i < cachedRenderers.Length; i++)
            {
                if (cachedRenderers[i] != null)
                {
                    cachedRenderers[i].enabled = visible;
                }
            }
        }

        private void ClearExpiredRequests()
        {
            if (activeRequests.Count == 0)
            {
                return;
            }

            List<object> expiredSources = null;
            foreach (KeyValuePair<object, float> request in activeRequests)
            {
                if (request.Value >= Time.time)
                {
                    continue;
                }

                expiredSources ??= new List<object>();
                expiredSources.Add(request.Key);
            }

            if (expiredSources == null)
            {
                return;
            }

            for (int i = 0; i < expiredSources.Count; i++)
            {
                activeRequests.Remove(expiredSources[i]);
            }
        }

        private void ResolveCloakVolume()
        {
            if (cloakVolume != null)
            {
                return;
            }

            Volume[] volumes = FindObjectsOfType<Volume>(true);
            for (int i = 0; i < volumes.Length; i++)
            {
                if (volumes[i] != null && volumes[i].name.Contains(fallbackVolumeName))
                {
                    cloakVolume = volumes[i];
                    return;
                }
            }
        }
    }
}
