using System;
using UnityEngine;
using UnityEngine.UI;

public class BoltPanelController : MonoBehaviour
{
    [Serializable]
    public class BoltSlot
    {
        public Image image;

        [Header("未解锁")]
        public Sprite lockedSprite;

        [Header("已花费")]
        public Sprite unlockedSprite;

        [Header("已解锁但未使用")]
        public Sprite availableSprite;

        [Header("红色警告")]
        public Sprite redSprite;
    }

    public static BoltPanelController Instance { get; private set; }

    [Header("Bolt UI")]
    [SerializeField] private BoltSlot[] slots = new BoltSlot[6];

    [SerializeField] private int initialUnlockedCount = 2;

    [SerializeField] private int maxUnlockedCount = 6;

    [SerializeField] private float flashSpeed = 6f;

    [SerializeField]
    [Range(0.1f, 1f)]
    private float flashMinAlpha = 0.35f;

    private int unlockedCount;

    // 已经花费掉的数量
    private int spentCount;

    private int previewCost;

    private int redSlotIndex = -1;

    private bool showInsufficientState;

    public event Action<int> BoltCountChanged;

    public int UnlockedCount => unlockedCount;

    public int MaxUnlockedCount =>
        Mathf.Clamp(maxUnlockedCount, 0, slots != null ? slots.Length : 0);

    public int SpentCount => spentCount;

    public int AvailableCount =>
        Mathf.Max(0, unlockedCount - spentCount);

    public bool IsFull =>
        unlockedCount >= MaxUnlockedCount;

    //========================================================
    // 生命周期
    //========================================================

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning(
                "场景中存在多个 BoltPanelController，只会使用最先初始化的一个。",
                this
            );
        }
        else
        {
            Instance = this;
        }

        unlockedCount =
            Mathf.Clamp(initialUnlockedCount, 0, MaxUnlockedCount);

        Refresh();
    }

    private void Update()
    {
        // 预览闪烁需要持续刷新
        if (previewCost > 0 && !showInsufficientState)
        {
            Refresh();
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void OnValidate()
    {
        if (slots == null || slots.Length != 6)
        {
            Array.Resize(ref slots, 6);
        }

        maxUnlockedCount =
            Mathf.Clamp(maxUnlockedCount, 0, slots.Length);

        initialUnlockedCount =
            Mathf.Clamp(initialUnlockedCount, 0, maxUnlockedCount);
    }

    //========================================================
    // 解锁
    //========================================================

    public bool TryUnlockNextBolt()
    {
        if (IsFull)
        {
            return false;
        }

        SetUnlockedCount(unlockedCount + 1);

        return true;
    }

    public void SetUnlockedCount(int count)
    {
        int clampedCount =
            Mathf.Clamp(count, 0, MaxUnlockedCount);

        if (unlockedCount == clampedCount)
        {
            Refresh();
            return;
        }

        unlockedCount = clampedCount;

        if (redSlotIndex >= unlockedCount)
        {
            redSlotIndex = -1;
        }

        spentCount =
            Mathf.Clamp(spentCount, 0, unlockedCount);

        previewCost = 0;

        showInsufficientState = false;

        Refresh();

        BoltCountChanged?.Invoke(unlockedCount);
    }

    //========================================================
    // 消耗
    //========================================================

    public void SetSpentCount(int count)
    {
        spentCount =
            Mathf.Clamp(count, 0, unlockedCount);

        ClearPreview();
    }

    //========================================================
    // 预览
    //========================================================

    public void PreviewCost(int cost)
    {
        previewCost = Mathf.Max(0, cost);

        showInsufficientState = false;

        Refresh();
    }

    public void ShowInsufficient()
    {
        previewCost = 0;

        showInsufficientState = true;

        Refresh();
    }

    public void ClearPreview()
    {
        previewCost = 0;

        showInsufficientState = false;

        Refresh();
    }

    //========================================================
    // 红色标记
    //========================================================

    public void SetRedSlot(int index)
    {
        if (index < 0 || index >= unlockedCount)
        {
            redSlotIndex = -1;
        }
        else
        {
            redSlotIndex = index;
        }

        Refresh();
    }

    public void ClearRedSlot()
    {
        redSlotIndex = -1;

        Refresh();
    }

    //========================================================
    // UI刷新
    //========================================================

    private void Refresh()
    {
        if (slots == null)
        {
            return;
        }

        int maxCount = MaxUnlockedCount;

        unlockedCount =
            Mathf.Clamp(unlockedCount, 0, maxCount);

        spentCount =
            Mathf.Clamp(spentCount, 0, unlockedCount);

        bool shouldFlashAvailable =
            previewCost > 0 &&
            previewCost <= AvailableCount &&
            !showInsufficientState;

        float flashAlpha =
            Mathf.Lerp(
                flashMinAlpha,
                1f,
                (Mathf.Sin(Time.unscaledTime * flashSpeed) + 1f) * 0.5f
            );

        for (int i = 0; i < slots.Length; i++)
        {
            BoltSlot slot = slots[i];

            if (slot == null || slot.image == null)
            {
                continue;
            }

            slot.image.raycastTarget = false;

            // 超出最大上限
            if (i >= maxCount)
            {
                slot.image.enabled = false;
                continue;
            }

            slot.image.enabled = true;

            Color color = slot.image.color;
            color.a = 1f;
            slot.image.color = color;

            //------------------------------------------------
            // 红色状态
            //------------------------------------------------

            if (i == redSlotIndex && i < unlockedCount)
            {
                slot.image.sprite =
                    slot.redSprite != null
                    ? slot.redSprite
                    : slot.unlockedSprite;
            }

            //------------------------------------------------
            // 不足状态
            //------------------------------------------------

            else if (
                showInsufficientState &&
                i >= spentCount &&
                i < unlockedCount
            )
            {
                slot.image.sprite =
                    slot.redSprite != null
                    ? slot.redSprite
                    : slot.unlockedSprite;
            }

            //------------------------------------------------
            // 普通状态
            //------------------------------------------------

            else
            {
                // 未解锁
                if (i >= unlockedCount)
                {
                    slot.image.sprite = slot.lockedSprite;
                }

                // 已花费
                else if (i < spentCount)
                {
                    slot.image.sprite = slot.unlockedSprite;
                }

                // 已解锁但未花费
                else
                {
                    slot.image.sprite =
                        slot.availableSprite != null
                        ? slot.availableSprite
                        : slot.unlockedSprite;
                }
            }

            //------------------------------------------------
            // 闪烁预览
            //------------------------------------------------

            if (
                shouldFlashAvailable &&
                i >= spentCount &&
                i < spentCount + previewCost
            )
            {
                color = slot.image.color;

                color.a = flashAlpha;

                slot.image.color = color;
            }
        }
    }
}