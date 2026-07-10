using System;
using System.Collections.Generic;

// 装备/拾取 UI 状态的存档快照。由 PickupUIController 采集与恢复。
[Serializable]
public class PickupSaveState
{
    public List<PickupItemId> unlockedItems = new List<PickupItemId>();
    public List<PickupItemId> equippedSlotItems = new List<PickupItemId>();
    public List<bool> equippedSlotOccupied = new List<bool>();
    public bool fifthEquippedSlotUnlocked;
    public bool hasMimicTarget;
    public int mimicTargetRightSideIndex;
    public string mimicTargetComboCode;
}

// 单存档位的完整存档数据。JSON 序列化到 persistentDataPath。
[Serializable]
public class SaveData
{
    public int sceneBuildIndex = -1;
    public float checkpointX;
    public float checkpointY;
    public float checkpointZ;
    public PickupSaveState pickup;
    public int boltUnlockedCount;
    public List<string> unlockedSkillIds = new List<string>();
}
