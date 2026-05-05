using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class CipherPanelController : MonoBehaviour
{
    [Serializable]
    public class CipherKey
    {
        [Tooltip("密码贴图按钮。")]
        public Button button;

        [Tooltip("按钮上显示贴图的 Image。不填时会自动从 Button 自身查找。")]
        public Image image;

        [Tooltip("未点击时显示的贴图。")]
        public Sprite normalSprite;

        [Tooltip("点击选中后显示的贴图。")]
        public Sprite selectedSprite;

        [Tooltip("进入游戏时是否默认选中。")]
        public bool startsSelected;

        [NonSerialized] public bool selected;
    }

    [Header("Panel")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Button confirmButton;
    [SerializeField] private bool closeOnSuccess = true;
    [SerializeField] private bool keepOpenOnFailure = true;

    [Header("Trigger 提示")]
    [SerializeField] private GameObject promptRoot;
    [SerializeField] private TMP_Text promptText;

    [Header("密码键")]
    [Tooltip("密码贴图键。数量可变，暂定 9 个；正确密码用 1 开始的序号填写。")]
    [SerializeField] private CipherKey[] keys = new CipherKey[9];
    [SerializeField] private int[] correctKeyNumbers = { 1, 2, 3 };
    [SerializeField] private bool requireExactSelection = true;
    [SerializeField] private bool resetKeysWhenOpened = true;

    [Header("音效")]
    [SerializeField] private AudioClip successSfx;
    [SerializeField] private AudioClip failureSfx;
    [SerializeField, Range(0f, 1f)] private float sfxVolume = 1f;
    [SerializeField] private AudioSource fallbackAudioSource;

    [Header("解锁事件")]
    [SerializeField] private UnityEvent onUnlocked;

    private class KeyClickBinding
    {
        public Button button;
        public UnityAction listener;
    }

    private readonly List<KeyClickBinding> keyClickBindings = new List<KeyClickBinding>();
    private UnityAction confirmListener;
    private bool unlocked;

    public bool IsOpen => panelRoot != null && panelRoot.activeSelf;
    public bool IsUnlocked => unlocked;
    public event Action Unlocked;

    private void Reset()
    {
        panelRoot = gameObject;
        confirmButton = GetComponentInChildren<Button>(true);
        promptText = GetComponentInChildren<TMP_Text>(true);
        promptRoot = promptText != null ? promptText.gameObject : null;
    }

    private void Awake()
    {
        ResolveReferences();
        InitializeKeyStates();
        RegisterButtonListeners();
        RefreshAllKeys();
        Close();
        ShowPrompt(false);
    }

    private void OnDestroy()
    {
        UnregisterButtonListeners();
    }

    public void Open()
    {
        if (unlocked)
        {
            return;
        }

        if (resetKeysWhenOpened)
        {
            InitializeKeyStates();
            RefreshAllKeys();
        }

        if (panelRoot != null)
        {
            panelRoot.SetActive(true);
        }

        ShowPrompt(false);
    }

    public void Close()
    {
        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }
    }

    public void Toggle()
    {
        if (IsOpen)
        {
            Close();
            return;
        }

        Open();
    }

    public void ShowPrompt(bool visible)
    {
        if (promptRoot != null)
        {
            promptRoot.SetActive(visible && !unlocked && !IsOpen);
        }
    }

    public void ToggleKey(int keyIndex)
    {
        if (!IsValidKeyIndex(keyIndex) || unlocked)
        {
            return;
        }

        keys[keyIndex].selected = !keys[keyIndex].selected;
        RefreshKey(keyIndex);
    }

    public void Confirm()
    {
        if (unlocked)
        {
            return;
        }

        if (IsCorrectSelection())
        {
            unlocked = true;
            PlaySfx(successSfx);
            onUnlocked?.Invoke();
            Unlocked?.Invoke();

            if (closeOnSuccess)
            {
                Close();
            }

            ShowPrompt(false);
            return;
        }

        PlaySfx(failureSfx);

        if (!keepOpenOnFailure)
        {
            Close();
        }
    }

    public void ResetCipher()
    {
        unlocked = false;
        InitializeKeyStates();
        RefreshAllKeys();
    }

    private void ResolveReferences()
    {
        if (panelRoot == null)
        {
            panelRoot = gameObject;
        }

        if (keys == null)
        {
            keys = Array.Empty<CipherKey>();
        }

        for (int i = 0; i < keys.Length; i++)
        {
            if (keys[i] == null)
            {
                keys[i] = new CipherKey();
            }

            if (keys[i].image == null && keys[i].button != null)
            {
                keys[i].image = keys[i].button.GetComponent<Image>();
            }
        }
    }

    private void RegisterButtonListeners()
    {
        UnregisterButtonListeners();

        for (int i = 0; i < keys.Length; i++)
        {
            if (keys[i] == null || keys[i].button == null)
            {
                continue;
            }

            int capturedIndex = i;
            UnityAction listener = () => ToggleKey(capturedIndex);
            keys[i].button.onClick.AddListener(listener);
            keyClickBindings.Add(new KeyClickBinding
            {
                button = keys[i].button,
                listener = listener
            });
        }

        if (confirmButton != null)
        {
            confirmListener = Confirm;
            confirmButton.onClick.AddListener(confirmListener);
        }
    }

    private void UnregisterButtonListeners()
    {
        for (int i = 0; i < keyClickBindings.Count; i++)
        {
            KeyClickBinding binding = keyClickBindings[i];
            if (binding != null && binding.button != null && binding.listener != null)
            {
                binding.button.onClick.RemoveListener(binding.listener);
            }
        }

        keyClickBindings.Clear();

        if (confirmButton != null && confirmListener != null)
        {
            confirmButton.onClick.RemoveListener(confirmListener);
            confirmListener = null;
        }
    }

    private void InitializeKeyStates()
    {
        if (keys == null)
        {
            return;
        }

        for (int i = 0; i < keys.Length; i++)
        {
            if (keys[i] != null)
            {
                keys[i].selected = keys[i].startsSelected;
            }
        }
    }

    private void RefreshAllKeys()
    {
        if (keys == null)
        {
            return;
        }

        for (int i = 0; i < keys.Length; i++)
        {
            RefreshKey(i);
        }
    }

    private void RefreshKey(int keyIndex)
    {
        if (!IsValidKeyIndex(keyIndex))
        {
            return;
        }

        CipherKey key = keys[keyIndex];
        if (key.image == null)
        {
            return;
        }

        Sprite nextSprite = key.selected ? key.selectedSprite : key.normalSprite;
        if (nextSprite != null)
        {
            key.image.sprite = nextSprite;
        }
    }

    private bool IsCorrectSelection()
    {
        HashSet<int> correctIndexes = BuildCorrectIndexSet();

        for (int i = 0; i < keys.Length; i++)
        {
            bool shouldBeSelected = correctIndexes.Contains(i);
            bool isSelected = keys[i] != null && keys[i].selected;

            if (shouldBeSelected && !isSelected)
            {
                return false;
            }

            if (requireExactSelection && !shouldBeSelected && isSelected)
            {
                return false;
            }
        }

        return correctIndexes.Count > 0;
    }

    private HashSet<int> BuildCorrectIndexSet()
    {
        HashSet<int> correctIndexes = new HashSet<int>();

        if (correctKeyNumbers == null)
        {
            return correctIndexes;
        }

        for (int i = 0; i < correctKeyNumbers.Length; i++)
        {
            int keyIndex = correctKeyNumbers[i] - 1;
            if (IsValidKeyIndex(keyIndex))
            {
                correctIndexes.Add(keyIndex);
            }
        }

        return correctIndexes;
    }

    private bool IsValidKeyIndex(int keyIndex)
    {
        return keys != null && keyIndex >= 0 && keyIndex < keys.Length && keys[keyIndex] != null;
    }

    private void PlaySfx(AudioClip clip)
    {
        if (clip == null)
        {
            return;
        }

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(clip, sfxVolume);
            return;
        }

        if (fallbackAudioSource == null)
        {
            fallbackAudioSource = GetComponent<AudioSource>();
        }

        if (fallbackAudioSource != null)
        {
            fallbackAudioSource.PlayOneShot(clip, sfxVolume);
        }
    }
}
