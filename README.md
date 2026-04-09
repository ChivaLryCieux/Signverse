# 标示界 Signverse

`Signverse` 是一个基于 Unity 2022 开发的 2.5D 横版技能解谜闯关游戏原型。  
项目当前围绕“技能组合 + 场景机关 + 平台跳跃”的核心体验展开，玩家通过不同能力的切换、解锁与组合，完成移动、穿越、攀爬、跳跃和机关互动。

## 项目概览

- 引擎版本：Unity `2022.3.62f3`
- 渲染管线：URP
- 输入系统：Unity Input System
- 游戏类型：2.5D 横版 / 技能解谜 / 平台闯关

## 当前核心玩法

- 基础横向移动
- 跳跃与蓄力远跳
- 攀爬能力
- 标准冲刺与变体冲刺
- 技能解锁与组合扩展
- 简单 UI 与音效管理

## 项目结构

```text
Assets/
|- Scenes/                  关卡与测试场景
|- Scripts/
|  |- Player/               玩家控制、动画、输入封装
|  |- Camera/               2.5D 跟随相机逻辑
|  |- SkillSystem/          技能基类、数据库、技能实现
|  |- UI/                   UI 开关与技能按钮交互
|  |- SFX/                  音频管理与声音数据
|- Prefabs/                 预制体资源
|- UI/                      UI 美术与界面资源
|- SkillAssets/             技能相关资源
|- Animation/               动画资源
|- Shader/                  Shader 与体积雾等效果
Packages/
ProjectSettings/
```

## 架构说明

### 1. 玩家控制主线

玩家核心逻辑位于 `Assets/Scripts/Player/PlayerCC.cs`。

它负责：

- 读取输入
- 管理 `CharacterController`
- 维护朝向、落地、攀爬、重力和垂直速度
- 每帧调度当前装备技能与已解锁技能

项目当前采用“玩家本体负责调度，技能对象负责行为”的设计。

### 2. 技能系统

技能系统位于 `Assets/Scripts/SkillSystem/`。

- `SkillBase.cs`：所有技能的抽象基类
- `SkillDatabase.cs`：技能数据库，用 `skillID` 管理可解锁技能
- `SkillSO/`：具体技能实现

当前技能实现包括：

- `MoveSkill`：基础移动与攀爬支持
- `JumpSkill`：普通跳跃
- `LongJumpSkill`：蓄力跳跃 / 远跳
- `StdDash`：标准冲刺
- `31-dm`、`32-dj` 等：技能强化或方向扩展变体

技能主要使用 `ScriptableObject` 实现，便于后续做：

- 技能解锁
- 技能组合
- 技能升级
- 数值独立调参

### 3. 输入系统

输入资源位于 `Assets/PlayerControls.inputactions`。

当前已配置的基础动作：

- `Move`
- `Jump`

项目已经接入 Unity 新输入系统，但部分技能脚本仍保留 `Input.GetKey` 风格写法，后续可以逐步统一为 Input System。

### 4. 其他子系统

- `Camera/`：2.5D 跟随相机与跟随点控制
- `UI/`：界面开关、技能按钮交互
- `SFX/`：单例音频管理器与音效数据对象

## 当前场景

当前项目内可见场景包括：

- `Assets/Scenes/SceneLiang.unity`
- `Assets/Scenes/TestScene.unity`

说明：`ProjectSettings/EditorBuildSettings.asset` 中仍保留了旧的 `MainScene.unity` 路径引用，但其 GUID 指向的是当前的 `TestScene.unity`。如果后续需要正式打包，建议在 Unity Editor 中重新确认 Build Settings。

## 运行方式

1. 使用 Unity Hub 打开本项目目录
2. 选择 Unity `2022.3.62f3`
3. 等待资源导入完成
4. 打开 `Assets/Scenes/` 下的场景
5. 点击 Play 运行

## 开发建议

- 优先统一所有技能输入到 Input System
- 补充技能 UI 与技能切换反馈
- 完善关卡机关、死亡与重生流程
- 为技能组合建立更清晰的命名和规则
- 增加 README 中的操作说明、关卡说明和截图

## 项目状态

当前项目处于原型开发阶段，已经具备基础移动、技能和关卡实验能力，适合继续向以下方向推进：

- 技能系统扩展
- 解谜关卡设计
- 美术和演出完善
- 游戏流程整合

---

如果你正在继续开发 `Signverse`，建议优先从 `PlayerCC`、`SkillBase` 和 `SkillSO/` 目录开始阅读，这三部分是目前整个项目的核心。
