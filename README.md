# 标示界 Signverse

`Signverse` 是一个基于 Unity 2022 开发的 2.5D 横版技能解谜闯关游戏原型。  
玩家通过不同能力的解锁、组合与切换，完成移动、穿越、攀爬、跳跃和机关互动。

## 项目概览

| 项目 | 说明 |
|------|------|
| 引擎 | Unity `2022.3.62f3` |
| 渲染管线 | URP (Universal Render Pipeline) |
| 输入系统 | Unity Input System (New)，支持 PC 键鼠 + 安卓触屏 |
| 游戏类型 | 2.5D 横版 / 技能解谜 / 平台闯关 |

## 核心玩法

### 技能系统

技能分为 4 大类，每类 5 个变体（1 个标准 + 4 个组合），共 20 个技能：

| 类别 | 标准 | +移动 | +跳跃 | +冲刺 | +隐身 |
|------|------|-------|-------|-------|-------|
| **移动** (1x) | 10-StdMove (6m/s) | 11-mm (2D自由移动) | 12-mj (攀爬) | 13-md (高速移动) | 14-mc (静止隐身) |
| **跳跃** (2x) | 20-StdJump (单段跳) | 21-jm (蓄力跳) | 22-jj (三段跳) | 23-jd (喷气背包) | 24-jc (占位) |
| **冲刺** (3x) | 30-StdDash (标准冲刺) | 31-dm (加长距离) | 32-dj (八方向冲刺) | 33-dd (持续冲刺) | 34-dc (无敌冲刺) |
| **隐身** (4x) | 40-StdCloak (标准隐身) | 41-cm (替身分身) | 42-cj (自动循环隐身) | 43-cd (死亡博弈) | 44-cc (切换隐身) |

### 技能组合机制

装备栏分为左右两侧：
- **右侧**：已解锁的技能图标（最多 5 个）
- **左侧**：装备槽（最多 5 个，第 5 个需解锁）

将两个技能分别装备到左侧槽位的主槽和副槽，系统自动拼接 ID（如 `1` + `2` → `12-mj`）查找组合技能。

### 装备经济

装备技能消耗「螺栓」资源，通过关卡中拾取获得：

| 槽位 | 螺栓消耗 |
|------|----------|
| 槽 1 | 1 |
| 槽 2 | 1 |
| 槽 3 | 2 |
| 槽 4 | 3 |

装备修改只能在 "Nature" 或 "Water" 标签的地面上进行。

## 项目结构

```text
Assets/
├── Scripts/
│   ├── Player/               玩家控制、动画、死亡重生、攀爬、隐身效果
│   ├── SkillSystem/          技能基类、数据库、技能实现 (SkillSO/)
│   ├── Mobile/               移动端虚拟输入（摇杆、按钮、管理器）
│   ├── UI/                   技能装卸 HUD、暂停菜单、解密面板、叙事面板
│   ├── Camera/               2.5D 跟随相机与 Cinemachine 切换
│   ├── Audio/                单例音频管理器 (BGM + SFX)
│   ├── Enemy/                敌人与危险物（电地板、激光、活塞）
│   ├── Pickup/               拾取物（技能、螺栓、第五槽、钥匙）
│   └── 切换Scene/            场景传送门与主菜单
├── SkillAssets/              技能 ScriptableObject 资源 (21 个技能 + 1 数据库)
├── Scenes/                   游戏场景
├── Prefabs/                  预制体（玩家、UI、拾取物、敌人、关卡模型）
├── Animation/                动画资源
├── Audio/                    音频资源
├── Shader/                   自定义 Shader 与体积雾
├── Mat/                      材质
├── Images/                   图片资源
└── Settings/                 URP 渲染设置
```

## 架构说明

### 1. 玩家控制 — `PlayerCC.cs`

中央控制器（~1100 行，12 个功能分区），持有 `CharacterController`，负责：

- 读取输入（键盘 + 移动端虚拟输入融合）
- 管理姿态状态：`Grounded` / `Airborne` / `Climbing`
- 为技能提供输入查询接口（`GetMoveInput`、`WasJumpPressed`、`WasDashPressed` 等）
- 为技能提供物理钩子（`MoveCharacter`、`SetVerticalVelocity`、`RequestGravitySuppressed` 等）
- 控制代理系统（替身技能 41-cm 使用）
- 攀爬过渡触发器管理

### 2. 技能系统 — ScriptableObject 架构

| 文件 | 职责 |
|------|------|
| `SkillBase.cs` | 抽象基类，定义 `skillID`、`OnActivate()`、`OnUpdate()` |
| `SkillDatabase.cs` | 技能注册表，`GetSkillByID()` 查找 |
| `SkillController.cs` | MonoBehaviour，管理 starting/unlocked/equipped 列表，每帧调度技能更新 |
| `SkillSO/` | 20 个具体技能实现 |

### 3. UI 系统

**技能装卸 HUD** — `PickupUIController.cs`（~1700 行）：
- 左侧装备槽 + 右侧解锁槽，**拖拽操作**装备/卸下/交换技能
- 螺栓经济系统（`BoltPanelController`）
- 技能组合自动同步到 `PlayerCC.equippedSkills`
- 右键查看详情面板，Tab 切换 HUD 可见性

**其他 UI：**
- `PausePanelController` — 暂停菜单（重生/继续/返回主菜单）
- `CartoonPanelController` — 开场漫画序列
- `CipherPanelController` — 密码锁解密面板
- `InteractionPanelController` — 叙事/交互面板
- `TMPTypewriter` / `UIImageFadeIn` / `TMPFadeOut` — 文字与图片动画效果

### 4. 输入系统

完全使用 Unity 新输入系统（`Active Input Handling = New`）。

**PC 端输入：**

| 动作 | 按键 | 类型 |
|------|------|------|
| 移动 | WASD | 持续轴 |
| 跳跃 | Space | 按住/释放（支持蓄力） |
| 冲刺 | L | 点击 |
| 隐身 | K / H | 点击 |
| 交互 | E | 点击 |
| 暂停 | Escape | 点击 |
| HUD 切换 | Tab | 点击 |

**移动端输入：**

通过 `MobileInputManager` 单例桥接触屏操作到现有输入系统：

| 控件 | 功能 | 交互方式 |
|------|------|----------|
| 虚拟摇杆 | 移动 | 持续拖拽（4 方向） |
| 跳跃按钮 | 跳跃 | 点击 / 长按蓄力 / 松开释放 |
| 冲刺按钮 | 冲刺 | 点击 |
| 隐身按钮 | 隐身 | 点击 |
| 交互按钮 | 交互 | 点击（自动显隐） |
| 暂停按钮 | 暂停 | 点击 |

移动端输入脚本位于 `Assets/Scripts/Mobile/`：
- `MobileInputManager.cs` — 输入管理器单例，暴露静态输入状态
- `MobileJoystick.cs` — 虚拟摇杆组件
- `MobileButton.cs` — 虚拟按钮组件

输入读取采用**消费者重置模式**：`PlayerCC` 读取标志后立即重置，避免 MonoBehaviour 执行顺序问题。

### 5. 其他子系统

| 子系统 | 关键文件 | 说明 |
|--------|----------|------|
| 死亡/重生 | `PlayerDeath.cs` | 摔落、触电、Hazard 死亡，检查点重生，无敌帧 |
| 动画 | `AnimatorStateDebugger.cs` | 奔跑/攀爬/蓄力跳/冲刺动画驱动，根运动 |
| 隐身效果 | `CloakEffectController.cs` | 多源请求制隐身，URP Volume 混合，渲染器隐藏，穿水 |
| 相机 | `CameraFollow.cs` | 2.5D 跟随，死区，边界约束，Cinemachine 切换 |
| 音频 | `AudioManager.cs` | 单例 BGM/SFX 管理，DontDestroyOnLoad |
| 攀爬 | `ClimbTransitionTrigger.cs` | 攀爬区域触发器，自动翻越顶部 |
| 场景切换 | `MainMenuScenePortal.cs` / `TriggerScenePortal.cs` | 主菜单渐变过渡，游戏内传送门 |

## 当前场景

| 场景 | 用途 |
|------|------|
| `开始界面.unity` | 主菜单 |
| `标识界.unity` | 主关卡 |
| `标识界外部.unity` | 外部区域 |
| `副标识界.unity` | 副关卡 |
| `外部【修改】.unity` | 修改版外部区域 |
| `TestLiang/` `TestLu/` `TestLuo/` | 开发者测试场景 |

## 运行方式

1. 使用 Unity Hub 打开本项目目录
2. 选择 Unity `2022.3.62f3`
3. 等待资源导入完成
4. 打开 `Assets/Scenes/开始界面.unity` 或 `Assets/Scenes/标识界.unity`
5. 点击 Play 运行

### 安卓构建

1. `File > Build Settings` 切换平台到 Android
2. 确认 `Player Settings > Active Input Handling` 为 `New`
3. 在场景中创建 `MobilePanel` UI（摇杆 + 按钮），挂载 `MobileJoystick` 和 `MobileButton` 组件
4. Build And Run

## 开发建议

- 关卡机关与解谜深度扩展
- 技能 UI 冷却/充能可视化
- 存档系统
- 音效与演出完善
- 新手引导优化

## 项目状态

当前处于原型开发阶段，已具备：
- 完整的技能解锁、组合、装备系统（20 个技能）
- 拖拽式技能装卸 UI
- 螺栓经济系统
- 死亡/重生/检查点流程
- 攀爬、隐身、冲刺等核心机制
- PC + 安卓双端输入支持
- 多关卡场景与场景切换
- 密码锁解密与叙事交互面板

---

核心阅读入口：`PlayerCC.cs` → `SkillBase.cs` → `SkillSO/` → `PickupUIController.cs`
