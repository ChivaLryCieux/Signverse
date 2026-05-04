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
        [SerializeField, Min(0.02f)] private float requestTimeout = 0.15f;

        [Header("角色显示")]
        [SerializeField] private bool hideRenderersWhileCloaked = true;

        [Header("音效")]
        [SerializeField] private AudioClip cloakSfx;
        [SerializeField, Range(0f, 1f)] private float cloakSfxVolume = 1f;

        private const float InstantVolumeSpeed = 100000f;

        private class CloakRequest
        {
            public float expiresAt;
            public float volumeWeight;
            public float enterVolumeSpeed;
            public float exitVolumeSpeed;
        }

        private readonly Dictionary<object, CloakRequest> activeRequests = new Dictionary<object, CloakRequest>();
        private readonly Dictionary<Renderer, bool> rendererVisibilityBeforeCloak = new Dictionary<Renderer, bool>();
        private Renderer[] cachedRenderers;
        private AudioSource audioSource;
        private PlayerDeath playerDeath;
        private bool isCloaked;
        private float currentVolumeTargetWeight;
        private float currentEnterVolumeSpeed = InstantVolumeSpeed;
        private float currentExitVolumeSpeed = InstantVolumeSpeed;
        private float lastVolumeUpdateTime = -1f;
        public bool IsCloaked => isCloaked;

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            playerDeath = GetComponent<PlayerDeath>();
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
            RefreshCloakStateFromRequests();
            UpdateVolumeWeight();
            UpdateDeathImmunity();
        }

        public void RequestCloak(object source, bool active)
        {
            RequestCloak(source, active, invisibleWeight, InstantVolumeSpeed, InstantVolumeSpeed);
        }

        public void RequestCloak(
            object source,
            bool active,
            float volumeWeight,
            float enterVolumeSpeed,
            float exitVolumeSpeed)
        {
            if (source == null)
            {
                source = this;
            }

            if (active)
            {
                if (!activeRequests.TryGetValue(source, out CloakRequest request))
                {
                    request = new CloakRequest();
                    activeRequests[source] = request;
                }

                request.expiresAt = Time.time + requestTimeout;
                request.volumeWeight = Mathf.Clamp01(volumeWeight);
                request.enterVolumeSpeed = Mathf.Max(0f, enterVolumeSpeed);
                request.exitVolumeSpeed = Mathf.Max(0f, exitVolumeSpeed);
            }
            else
            {
                if (activeRequests.TryGetValue(source, out CloakRequest request))
                {
                    currentExitVolumeSpeed = Mathf.Max(0f, request.exitVolumeSpeed);
                    activeRequests.Remove(source);
                }
            }

            RefreshCloakStateFromRequests();
            UpdateVolumeWeight();
            UpdateDeathImmunity();
        }

        private void RefreshCloakStateFromRequests()
        {
            bool hasActiveRequest = activeRequests.Count > 0;

            if (hasActiveRequest)
            {
                currentVolumeTargetWeight = 0f;
                currentEnterVolumeSpeed = InstantVolumeSpeed;

                foreach (CloakRequest request in activeRequests.Values)
                {
                    if (request.volumeWeight >= currentVolumeTargetWeight)
                    {
                        currentVolumeTargetWeight = request.volumeWeight;
                        currentEnterVolumeSpeed = request.enterVolumeSpeed;
                    }
                }
            }
            else
            {
                currentVolumeTargetWeight = 0f;
            }

            SetCloaked(hasActiveRequest);
        }

        private void SetCloaked(bool cloaked)
        {
            if (isCloaked == cloaked)
            {
                return;
            }

            isCloaked = cloaked;
            if (isCloaked)
            {
                HideCloakedRenderers();
            }
            else
            {
                RestoreRendererVisibility();
            }

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

            float speed = isCloaked ? currentEnterVolumeSpeed : currentExitVolumeSpeed;
            float targetWeight = isCloaked ? currentVolumeTargetWeight : 0f;
            float deltaTime = lastVolumeUpdateTime < 0f ? Time.deltaTime : Mathf.Max(0f, Time.time - lastVolumeUpdateTime);
            lastVolumeUpdateTime = Time.time;
            cloakVolume.weight = Mathf.MoveTowards(cloakVolume.weight, targetWeight, speed * deltaTime);
        }

        private void UpdateDeathImmunity()
        {
            if (!isCloaked)
            {
                return;
            }

            if (playerDeath == null)
            {
                playerDeath = GetComponent<PlayerDeath>();
            }

            if (playerDeath != null)
            {
                playerDeath.RequestDeathBlock();
            }
        }

        private void HideCloakedRenderers()
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
                Renderer targetRenderer = cachedRenderers[i];
                if (targetRenderer == null)
                {
                    continue;
                }

                if (!rendererVisibilityBeforeCloak.ContainsKey(targetRenderer))
                {
                    rendererVisibilityBeforeCloak.Add(targetRenderer, targetRenderer.enabled);
                }

                targetRenderer.enabled = false;
            }
        }

        private void RestoreRendererVisibility()
        {
            if (!hideRenderersWhileCloaked)
            {
                return;
            }

            foreach (KeyValuePair<Renderer, bool> entry in rendererVisibilityBeforeCloak)
            {
                if (entry.Key != null)
                {
                    entry.Key.enabled = entry.Value;
                }
            }

            rendererVisibilityBeforeCloak.Clear();
        }

        private void ClearExpiredRequests()
        {
            if (activeRequests.Count == 0)
            {
                return;
            }

            List<object> expiredSources = null;
            foreach (KeyValuePair<object, CloakRequest> request in activeRequests)
            {
                if (request.Value.expiresAt >= Time.time)
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
                object source = expiredSources[i];
                if (activeRequests.TryGetValue(source, out CloakRequest request))
                {
                    currentExitVolumeSpeed = Mathf.Max(0f, request.exitVolumeSpeed);
                    activeRequests.Remove(source);
                }
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
