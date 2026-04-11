# 3D 暂停技能 UI 挂接说明

本文档说明如何在现有 `Player Bundle`、3D UI 模型与 Cinemachine 虚拟相机基础上，挂接完整的暂停与技能选取 UI。

## 目标效果

- 按 `Esc` 打开/关闭暂停技能 UI。
- 打开时启用现有 `UI Camera` / Cinemachine 虚拟相机，并暂停游戏时间。
- 左侧为 2D 技能栏，技能图标按“从下到上”逐个解锁。
- 右侧为 3D 主 UI 标志物，初始全部为空标志。
- 从左侧选择技能后，可装备到右侧空标志位。
- 装备后左侧对应技能翻到背面/空状态。
- 右键点击已装备的 3D 技能，可拆卸并让左侧图标翻回正面。
- 点击已装备技能一次为选中，再次点击进入技能详情页。
- 选中左侧技能时，可显示 Bolt 点数；超出上限时 UI 飘红并闪烁。

## 涉及脚本

新增脚本：

- `Assets/Scripts/UI/SkillPauseUIController.cs`
- `Assets/Scripts/UI/Skill2DSlot.cs`
- `Assets/Scripts/UI/Skill3DSlot.cs`

修改脚本：

- `Assets/Scripts/Player/PlayerCC.cs`
- `Assets/Scripts/UI/UI manager.cs`

`SkillPauseUIController` 是总控。它负责暂停、相机启用、左侧技能栏、右侧 3D 装备位、音效、详情页和 Bolt 显示。

`Skill2DSlot` 挂在左侧每个 2D 技能图标上。

`Skill3DSlot` 挂在右侧每个 3D 圆形标志物上。

## 一、总控挂接

在现有 `Player Bundle` prefab 的 `Player` 对象上添加：

```text
SkillPauseUIController
```

建议保留旧的 `UImanager`。新脚本存在时，旧 `UImanager` 会自动停止监听 `Esc`，避免两个脚本同时切换 UI。

### SkillPauseUIController 字段

#### Core References

`Ui Camera Root`

- 拖入现有 prefab 中的 `UI Camera` GameObject。
- 这个对象下面应包含现有 Cinemachine 虚拟相机和 3D UI 模型。
- 打开暂停 UI 时会 `SetActive(true)`，关闭时会 `SetActive(false)`。

`Player`

- 拖入同一个 `Player` 对象上的 `PlayerCC`。
- 用于监听 `UnlockNewSkill()` 触发的技能解锁事件。

`Library Slots Bottom To Top`

- 拖入左侧 2D 技能槽。
- 顺序必须是从下到上。
- 例如：

```text
Element 0 = 最下方技能
Element 1 = 倒数第二个技能
Element 2 = 再往上一个技能
```

`Equip Slots`

- 拖入右侧 3D 主 UI 的圆形标志物。
- 每个对象都需要挂 `Skill3DSlot`，并且需要 Collider。
- 顺序可按你的技能架逻辑定义，例如上杆主技能、中杆主技能、下杆主技能、上杆副技能等。

#### Skill Data

`Skill Entries Bottom To Top`

- 与 `Library Slots Bottom To Top` 使用同样顺序。
- 每一项配置：

```text
Skill       = 对应 SkillBase 资源
FrontSprite = 技能正面图
BackSprite  = 技能背面/扣牌图
BoltCost    = 该技能所需 Bolt 点数
```

`Starting Unlocked Count`

- 初始激活的左侧技能数量。
- 当前需求是默认只显示最下方一个技能，所以设为 `1`。

`Bolt Limit`

- Bolt 点数上限。
- 当选中技能的 `BoltCost` 大于这个数值时，Bolt UI 会变红。

#### Bolt UI

`Bolt Panel`

- 右侧或其他位置的 Bolt 点数面板。
- 没有选中技能时自动隐藏。

`Bolt Text`

- 显示当前技能 Bolt 消耗的 `Text`。
- 如果项目后续改用 TextMeshPro，可以再扩展为 TMP 版本。

`Bolt Normal Color`

- Bolt 消耗未超限时的颜色。

`Bolt Overflow Color`

- Bolt 消耗超过上限时的颜色，建议红色。

`Bolt Blink Speed`

- Bolt 数值闪烁速度。
- 使用 `Time.unscaledTime`，所以暂停时仍会闪烁。

#### Detail UI

`Detail Page`

- 技能详细检视页面，也就是需求里的 2D Paddle 页面。
- 默认可关闭。
- 点击已装备技能一次为选中，再次左键点击会打开。

`Detail Title Text`

- 显示技能名。

`Detail Cost Text`

- 显示 Bolt 消耗。

#### Audio

`Hover Clip`

- 鼠标悬停音效，“格”。

`Select Clip`

- 点击/确认选中音效，“格楞”。
- 目标 3D 槽不为空时，也播放这个音效并取消当前 2D 技能选中。

`Equip Success Clip`

- 装备成功音效，“噌——”。

`Detach Clip`

- 右键拆卸音效。

`Sfx Volume`

- UI 音效音量。
- 实际播放走现有 `AudioManager.Instance.PlaySFX()`。

#### Pause

`Pause On Open`

- 建议开启。
- 打开暂停 UI 时设置 `Time.timeScale = 0`。
- 关闭时恢复之前的时间缩放。

`Unlock Cursor On Open`

- 建议开启。
- 打开暂停 UI 时解锁并显示鼠标。
- 关闭时恢复之前鼠标状态。

## 二、左侧 2D 技能槽挂接

左侧每个技能图标 GameObject 添加：

```text
Skill2DSlot
```

### Skill2DSlot 字段

`Front Image`

- 技能正面图片。
- 显示技能图标。

`Back Image`

- 技能背面图片。
- 技能装备到 3D 主 UI 后显示。

`Empty State`

- 可选。
- 如果你想用单独空状态对象，可以拖这里。
- 装备后会随背面一起显示。

`Highlight`

- 悬停/选中时启用的发光边缘对象。
- 可以是普通 UI Image，也可以是使用 Shader Graph 的描边/发光材质。

`Selected Scale`

- 点击选中后的放大倍率。
- 默认 `1.12`。

### 左侧交互规则

- 未解锁的槽位会 `SetActive(false)`。
- 默认只显示底部 `Starting Unlocked Count` 个技能。
- 玩家获得新技能时，`PlayerCC.UnlockNewSkill()` 会触发事件，总控会激活对应技能槽。
- 鼠标悬停可启用 `Highlight` 并播放 “格”。
- 左键点击可放大图标，进入“已选中 2D 技能”状态，并播放 “格楞”。
- 成功装备到 3D 空槽后，该图标会切到背面/空状态。
- 右侧拆卸后，该图标会翻回正面。

## 三、右侧 3D 标志槽挂接

右侧主 UI 的每个圆形标志物添加：

```text
Skill3DSlot
```

该对象必须有 Collider，否则 `OnMouseEnter`、`OnMouseDown`、`OnMouseOver` 不会触发。

### Skill3DSlot 字段

`Empty Visual`

- 空标志物体。
- 初始显示。
- 没有装备技能时启用。

`Equipped Visual`

- 已装备技能后的 3D 视觉对象。
- 装备技能后启用。
- 如果每个槽之后要显示不同技能模型，可以在后续扩展此脚本。

`Highlight`

- 悬停/选中时启用的高亮对象。
- 可以是边缘发光 mesh、外圈、粒子或带 Shader Graph 的对象。

`Target Renderer`

- 可选。
- 如果你的标志物 Shader Graph 有高亮参数，可拖入对应 Renderer。

`Shader Highlight Property`

- 默认是 `_Highlight`。
- 悬停/选中时写入 `1`，取消时写入 `0`。
- Shader Graph 里需要暴露同名 Float 参数。

`Selected Scale`

- 选中 3D 槽后的放大倍率。
- 默认 `1.08`。

### 右侧交互规则

当已选中左侧 2D 技能时：

- 鼠标悬停 3D 槽，槽位高亮。
- 如果目标槽为空：
  - 左键点击后装备成功。
  - 播放 “噌——”。
  - 该槽记录 `EquippedSkill`。
  - 左侧对应技能翻到背面/空状态。
- 如果目标槽不为空：
  - 左键点击播放 “格楞”。
  - 取消当前左侧技能选中。
  - 左侧图标缩回原尺寸。

当没有选中左侧 2D 技能时：

- 左键点击已装备的 3D 槽，进入“已选中 3D 技能”状态。
- 再次左键点击同一个已装备槽，打开详情页。
- 右键点击已装备槽，拆卸技能。
- 拆卸后：
  - 3D 槽回到空标志。
  - 左侧对应技能图标翻回正面。
  - 播放拆卸音效。

## 四、Cinemachine 与暂停状态

现有 `UI Camera` 是通过 `SkillPauseUIController.Ui Camera Root` 控制启用/隐藏。

打开 UI 时：

```text
UI Camera Root.SetActive(true)
Time.timeScale = 0
Cursor.lockState = None
Cursor.visible = true
```

关闭 UI 时：

```text
UI Camera Root.SetActive(false)
恢复原 Time.timeScale
恢复原鼠标锁定与显示状态
```

注意：因为游戏暂停后 `Time.timeScale = 0`，所有需要在暂停 UI 里继续播放的动画，建议使用：

```text
Animator Update Mode = Unscaled Time
```

或者脚本里使用：

```csharp
Time.unscaledDeltaTime
Time.unscaledTime
```

当前 Bolt 闪烁已经使用 `Time.unscaledTime`。

## 五、Shader Graph 高亮建议

如果使用 Shader Graph 做边缘发亮，建议暴露一个 Float 参数：

```text
_Highlight
```

`Skill3DSlot` 会在悬停/选中时设置：

```text
_Highlight = 1
```

取消悬停/取消选中时设置：

```text
_Highlight = 0
```

左侧 2D UI 的高亮目前通过 `Highlight` GameObject 控制。如果左侧也需要 Shader Graph 参数，可用一个带发光材质的边框 Image/RawImage 作为 `Highlight`。

## 六、音效挂接

确保场景中存在 `AudioManager`，并且它有可用的 `sfxSource`。

在 `SkillPauseUIController` 上拖入：

```text
Hover Clip        = “格”
Select Clip       = “格楞”
Equip Success Clip = “噌——”
Detach Clip       = 右键拆卸音效
```

播放入口是：

```csharp
AudioManager.Instance.PlaySFX(clip, sfxVolume);
```

如果后续要使用 `SoundDataSO` 的 pitch 或随机音高，可以把 `SkillPauseUIController` 的 `AudioClip` 字段改为 `SoundDataSO` 字段。

## 七、技能解锁流程

现有 `PlayerCC.UnlockNewSkill(string id)` 已经接入事件：

```csharp
SkillUnlocked?.Invoke(newSkill);
```

当玩家获得新技能时：

```csharp
playerCC.UnlockNewSkill("31-dm");
```

如果该技能在 `Skill Entries Bottom To Top` 里已经配置，对应的左侧技能槽会激活。

建议技能解锁顺序与 `Skill Entries Bottom To Top` 顺序保持一致，也就是：

```text
最下方技能
倒数第二个技能
倒数第三个技能
...
最上方技能
```

## 八、后续扩展点

### 1. 真正应用装备技能

`SkillPauseUIController` 提供事件：

```csharp
SkillEquipped(int slotIndex, SkillBase skill)
SkillDetached(int slotIndex, SkillBase skill)
```

后续可以写一个装备应用器监听这两个事件，将技能绑定到玩家的主技能/副技能/杆位逻辑。

### 2. 每个 3D 槽显示不同技能模型

当前 `Skill3DSlot` 只有：

```text
Empty Visual
Equipped Visual
```

如果要按技能显示不同 3D 标志，可以扩展 `SkillPauseUIController.SkillUiEntry`，增加：

```text
GameObject equippedPrefab
Material equippedMaterial
```

然后在 `Skill3DSlot.SetSkill()` 中替换模型或材质。

### 3. 翻牌动画

当前翻牌是直接切换 `Front Image` / `Back Image` / `Empty State`。

如果要做完整翻牌动画，可在 `Skill2DSlot` 中加入 Animator，并把：

```csharp
SetFaceUp(false)
SetFaceUp(true)
```

替换成触发动画参数。

### 4. TextMeshPro

当前 Bolt 与详情页使用 `UnityEngine.UI.Text`。

如果项目 UI 后续统一使用 TextMeshPro，可把字段替换为：

```csharp
TMP_Text boltText;
TMP_Text detailTitleText;
TMP_Text detailCostText;
```

并添加：

```csharp
using TMPro;
```

## 九、快速检查清单

- `Player` 上已挂 `SkillPauseUIController`。
- `Ui Camera Root` 已拖现有 `UI Camera`。
- `Player` 字段已拖 `PlayerCC`。
- 左侧每个图标都挂了 `Skill2DSlot`。
- `Library Slots Bottom To Top` 顺序是从下到上。
- 每个 2D 槽都配置了 `Front Image`、`Back Image`、`Highlight`。
- 右侧每个 3D 标志都挂了 `Skill3DSlot`。
- 每个 3D 标志都有 Collider。
- 每个 3D 标志配置了 `Empty Visual` 和 `Equipped Visual`。
- 如果使用 Shader Graph 高亮，`Shader Highlight Property` 与 Shader 参数名一致。
- `Skill Entries Bottom To Top` 与左侧槽位顺序一致。
- `Starting Unlocked Count` 设为 `1`。
- 音效 Clip 已拖入。
- 场景里有 `AudioManager`。
- 如果使用 2D UI 点击，场景里有 `EventSystem`。
- 打开暂停 UI 后，鼠标可见且游戏停止。

