# 3D Adventure 多人协作架构重构方案

## 目标与项目定位

项目已从原型进入需要长期维护的小型独立游戏阶段。重构目标是：

1. 程序员能从目录和接口判断依赖方向，不需要先读完整项目。
2. 策划能从 Inspector 完成关卡事件、刷怪、任务、掉落和提示配置。
3. 场景缺少引用、稳定 ID 重复、配置不完整时，在提交前直接给出错误。
4. 保留当前状态机、ScriptableObject 配置和代码命名风格，渐进迁移。

## 当前最影响协作的问题

- `GamePlay` 同时管理敌人注册、掉落、HUD、暂停、死亡、Loading、物品展示和世界对象，修改一个功能容易影响其他模块。
- Level01 原先通过数组下标、对象激活状态、每帧轮询和委托拼接表达关卡流程；该关卡已迁移为命名事件，但其他关卡仍需按同一规则检查。
- 跨模块大量使用 `*.Instance`、`Find`、对象名和 `GetComponent` 链，依赖没有显示在 Inspector 或构造入口中。
- 当前 `LevelEventTrigger` 已经能数据化配置，但 `UnityEvent` 连接仍较抽象，重命名方法或丢失对象后不容易审查。
- 运行时字段和策划字段混在大型 MonoBehaviour 中，Inspector 很难区分“必须配置”“可调参数”和“调试状态”。
- 项目自有代码尚无 asmdef；第三方源码位于 Assembly-CSharp，现阶段直接拆程序集会产生反向引用问题。

## 目标模块

控制在七个模块内：

| 模块 | 责任 | 不负责 |
| --- | --- | --- |
| App / Session | 启动、场景切换、暂停、存档会话、全局服务生命周期 | 具体玩家、敌人和 UI 表现 |
| Content | 物品、武器、敌人、任务、掉落等只读定义和稳定 ID | 运行时生命、弹药、背包数量 |
| Player & Combat | 玩家状态、输入意图、武器运行时、伤害、交互 | 存档文件和关卡剧情 |
| Enemy & AI | 敌人状态、行为树适配、感知、注册、池化生命周期 | HUD、任务推进 |
| Inventory & Quest | 背包运行时、物品使用、任务进度和前置条件 | 场景表现与加载 |
| LevelFlow | 触发器、条件、动作、波次、出生点和世界状态 | 全局单例和具体 HUD 实现 |
| Presentation | HUD、菜单、提示、相机、音频和动画表现 | 修改领域内部字段 |

依赖方向：`Presentation → Gameplay contracts ← LevelFlow`，`App / Session` 负责装配；`Content` 只提供定义。模块之间不能通过对象名、数组下标或另一个模块的具体 MonoBehaviour 建立隐式依赖。

## 场景契约

每个可玩关卡使用固定根节点：

```text
__Level
  LevelInstaller
  LevelContext
  Flow
    Events
    Waves
    SpawnPoints
  Saveables
__Gameplay
  PlayerSpawn
  Enemies
  Interactables
__Presentation
  LevelHUD
  Cameras
__Environment
__Lighting
```

`LevelInstaller` 是场景唯一装配入口，Inspector 明确引用 `LevelContext`、玩家出生点、HUD、相机组和默认关卡配置。它不实现游戏规则，只在场景加载后完成绑定。

`LevelContext` 提供当前场景的窄接口：敌人注册表、世界可存档对象、关卡 HUD、波次目录和出生点目录。运行时系统不能自行 `Find("GamePlay")`。

启动顺序固定为：

1. Bootstrap 创建或确认 Session 服务。
2. SceneFlow 加载关卡。
3. 找到并校验唯一 `LevelInstaller`。
4. Installer 注册 `LevelContext`。
5. 创建场景玩家并应用新游戏数据或存档快照。
6. 启用输入和关卡流程。
7. 关闭 Loading。

## 策划配置模型

### 关卡事件

把现有 `LevelEventTrigger + UnityEvent` 演进为三个可见部分：

- `LevelEventNode`：事件 ID、策划备注、触发模式、是否仅触发一次。
- `LevelCondition`：门状态、持有物品、任务状态、敌人清空、对象状态等。
- `LevelAction` 组件：`SpawnWaveAction`、`SetObjectActiveAction`、`OpenDoorAction`、`StartQuestAction`、`ShowTipAction`、`PlayAudioAction`、`SaveGameAction`。

`LevelEventNode` 序列化一组实现 `ILevelAction` 的组件引用。每个动作独立显示必填字段和中文 Tooltip。UnityEvent 只保留给低风险表现连接，不再作为主流程入口。

策划看到的是：

```text
事件：取得门禁卡后开启机房战斗
触发：持有物品「门禁卡」
条件：机房战斗尚未完成
动作 1：播放 EnemyWave「MachineRoomWave」
动作 2：解除 MachineDoor 刚体锁定
动作 3：播放 DoorDown 音效
动作 4：设置世界标记 machine_room_started
```

这会替代 `Level1Flow` 中的 `enemySpawnPoints[2]`、对象激活反推状态和每帧委托调用。

### 配置资产

- `ScriptableObject` 继续保存只读定义：名称、图标、数值、Prefab、稳定 ID。
- 普通可序列化 DTO 保存运行时状态：生命、弹药、背包数量、任务进度、世界标记。
- 场景引用只放在场景组件或 Prefab，不放入跨场景配置资产。
- 每个策划资产包含：稳定 ID、显示名、说明、分类标签和版本字段。

### Inspector 规则

- 策划字段统一使用 `[SerializeField] private`，按“身份 / 必填引用 / 规则 / 表现 / 调试”分组。
- 必填字段使用自定义 Required 标记和校验器，不靠运行时报空引用。
- 数值使用 `[Min]`、`[Range]` 和安全默认值。
- 运行时状态只读显示，避免策划误认为需要配置。
- 自定义 Inspector 顶部显示配置完成度，并提供“定位缺失对象”“运行此事件”“重置事件”按钮。

## 程序通信规则

按以下优先级选择通信方式：

1. 同一 Prefab 内：`[SerializeField]` 直接引用。
2. 同一模块的运行时对象：明确的普通 C# 引用。
3. 场景级依赖：由 `LevelInstaller` 注入 `LevelContext`。
4. 跨模块请求：窄接口，例如 `ISaveService`、`IInventoryService`、`IQuestService`。
5. 跨模块通知：强类型事件，例如 `HealthChanged`、`InventoryChanged`、`QuestChanged`、`EnemyCountChanged`。

禁止新增：

- `GameObject.Find`、按名称查对象、依赖数组魔法下标。
- UI 直接改玩家、背包或任务内部字段。
- 新功能继续向 `GamePlay` 添加字段和方法。
- 无生命周期说明的全局单例和全局事件订阅。

事件订阅必须在 `OnEnable/OnDisable` 成对；接口按使用方需要设计，不暴露整个管理器。

## 玩家、武器、物品、敌人的策划友好度

| 系统 | 当前评分 | 当前可配置内容 | 主要阻碍 |
| --- | --- | --- | --- |
| 玩家 | 4/5 | `PlayerDefinition` 已集中生命、防御、移动、相机、检测半径、初始物品、默认远程武器与近战武器；PlayerControl Inspector 已按场景连接分组 | 动画状态名仍在状态机和武器定义中，GamePlay HUD 仍通过兼容门面读取玩家状态 |
| 武器 | 2.5/5 | 伤害、射速、弹量、特效、动画名和音效名 | 近战与枪械字段混在同一资产；使用数字弹药 ID、动画/音效字符串、Prefab 层级和 wpIndex/ammoIndex |
| 物品 | 3/5 | 名称、类型、数量、组合和拾取配置 | 顶层字段与 Item 结构重复；Prefab/Icon 使用 Resources 字符串；组合同时保存数字 ID 与资产引用 |
| 敌人 | 2/5 | 血量、韧性、移速、部位耐久、伤害和动画名 | AI、技能、NavMesh、声音、掉落、HitBox 分散在 Enemy Prefab；大量字符串和运行时字段公开；缺少完整性预览 |

整改目标：每类内容只有一个策划入口资产，并通过预览 Prefab 将表现引用集中起来。

- `PlayerDefinition`：基础属性、移动参数、初始装备；Player Prefab 只保留结构引用。
- `WeaponDefinition`：公共字段 + 按武器类型显示的近战/远程设置；直接引用弹药资产、AudioCue 和表现 Prefab。
- `ItemDefinition`：名称、图标、世界 Prefab、查看 Prefab、使用行为和组合规则；取消 Resources 路径字符串。
- `EnemyArchetype`：属性、感知、技能组、掉落表、声音组和表现 Prefab；Enemy Prefab 只负责骨骼、碰撞体和组件连接。

每个入口资产的 Inspector 都提供“验证配置”和“打开关联 Prefab”，并隐藏不适用于当前类型的字段。

## 现有代码处置

| 现有部分 | 处理方式 |
| --- | --- |
| StateMachine / PlayerStates / EnemyStates | 保留，逐步缩小状态类访问的全局对象 |
| GameContentCatalog / StableId | 保留并扩展校验 |
| LootTable | 保留，增加预览概率和配置完整性检查 |
| EnemyWave | 保留，作为 `SpawnWaveAction` 的目标 |
| LevelEventTrigger | 兼容保留，逐步升级为 `LevelEventNode` |
| Level1Flow | Level01 事件迁移完成后已删除 |
| GamePlay | 先作为兼容门面，再拆为 EnemyRegistry、HUDPresenter、ItemInspectController、LootService |
| SaveManager | 拆为 SaveRepository、SaveSnapshotBuilder、SaveRestoreService；旧 API 先转发 |
| SingletonMono | 只允许 App / Session 级服务使用；玩家、HUD、关卡流程必须场景化 |
| UnityEvent | 用于按钮和纯表现；核心关卡逻辑改用类型明确的动作组件 |

## 文件与协作边界

建议目录：

```text
Assets/Game/
  App/
  Content/
  Player/
  Enemy/
  InventoryQuest/
  LevelFlow/
  Presentation/
  Editor/
  Tests/
```

每个模块放一份短 README，固定写四项：模块责任、公开入口、允许依赖、配置步骤。复杂决策放 `Docs/ADR`，一项决策一页，不写长篇框架文档。

多人同时制作关卡时优先使用 Prefab 和独立事件节点，减少直接编辑同一个 `.unity`。三人以上需要同时编辑同一关卡时，再拆成 `Level_Environment`、`Level_Gameplay`、`Level_Lighting` 三个 Additive Scene。

项目设置统一：Visible Meta Files、Force Text、UnityYAMLMerge；二进制美术资源使用 Git LFS。Prefab、Scene 和 ScriptableObject 必须连同 `.meta` 提交，移动资源只在 Unity 内完成。

## 自动校验与策划交付

把现有 `Tools > 3D Adventure > Validate Game Content` 扩展为提交前检查：

- 场景恰好有一个 `LevelInstaller`，所有必填引用完整。
- Event ID、StableId、SaveableId 唯一。
- 每个 LevelEventNode 至少有一个动作，引用的条件和动作有效。
- EnemyWave 不含空出生点，出生点有敌人 Prefab。
- Quest、LootTable、武器和物品配置范围合法。
- 禁止场景玩家、HUD 和 LevelFlow 使用 `DontDestroyOnLoad`。
- 构建场景列表与存档引用的关卡一致。

提供三个策划模板 Prefab：`LevelEventNode_Template`、`EnemyWave_Template`、`SaveableObject_Template`。策划新建内容时复制模板，不从空 GameObject 开始。

## 分阶段迁移计划

### 阶段 1：建立规则，不改玩法

- 创建新目录、模块 README、命名和依赖规则。
- 新增 `LevelInstaller`、`LevelContext`、场景验证器。
- 新代码禁止增加 `GamePlay.Instance` 和 `Find`。
- 验收：现有关卡行为不变，验证工具能指出缺失引用。

### 阶段 2：让策划看懂关卡流程

- 实现常用的七个 LevelAction 组件和自定义 Inspector。
- 把 Level01 的四段刷怪与开门流程从 `Level1Flow` 迁移到命名事件节点。
- 为每个事件填写策划备注，并在 Scene 视图绘制触发范围和目标连线。
- 验收：策划不打开代码即可修改触发条件、波次和提示。

### 阶段 3：拆分 GamePlay

- 先抽 EnemyRegistry、HUDPresenter、ItemInspectController、LootService。
- `GamePlay` 暂时保留同名方法并转发，避免一次修改所有调用方。
- 每迁走一项责任就删除对应公开字段。
- 验收：`GamePlay` 只剩场景协调兼容层，UI 不再直接访问玩家内部状态。

### 阶段 4：拆分存档与会话

- SaveRepository 负责文件；SnapshotBuilder 负责采集；RestoreService 负责场景恢复。
- 关卡流程使用稳定事件 ID 和世界标记进入存档。
- 验收：新游戏、死亡重试、跨场景读档、损坏档都有明确结果。

### 阶段 5：程序集与所有权

- 先给直接引用的第三方源码建立或取得官方 asmdef，或通过 Adapter 隔离。
- 再按 `Game.Core`、`Game.Runtime`、`Game.Presentation`、`Game.Editor`、`Game.Tests` 拆分。
- 验收：程序集依赖单向，Editor 与 Runtime 完全分离。

## 首批任务清单

### 已完成：物品配置第一阶段

- ItemConfig 已改为统一策划入口：基础信息、直接资源引用、使用效果和组合规则。
- 背包、拾取、使用、组合与丢弃优先通过 ItemConfig 工作，并保留旧存档字段回退。
- 新增物品自定义 Inspector、自动迁移菜单和内容校验。
- 现有 10 个物品资产已写入第二版配置；详细操作见 item_configuration_guide.md。

- [x] 新建 `LevelInstaller` 和 `LevelContext`，替代场景中的名称查找。
- [x] 给 LevelEventTrigger 增加事件 ID、显示名、策划备注和验证状态。
- [x] 实现 SpawnWave、SetActive、OpenDoor、StartQuest、ShowTip、PlayAudio、SaveGame 动作组件。
- [x] 将 Level01 机房和剩余关卡事件迁移为命名事件节点，并删除 Level1Flow。
- [x] 新增场景契约和事件节点校验器。
- [x] 把 GamePlay 的敌人列表抽到 EnemyRegistry，以兼容属性转发。
- [x] 写一页《策划配置关卡事件》操作说明；样板位于 `__Level/Events/MachineRoom_Key1_Event`。

### 已完成：LevelFlow 第一批架构任务

- Level01 已建立五个标准根节点，并由唯一 LevelInstaller 装配 LevelContext。
- 机房 Key1 与剩余四段关卡流程已改为命名事件、EnemyWave 和强类型 Action；开场音乐也已改为开始事件。旧 Level1Flow 已移除。

- EnemyRegistry 已接管敌人注册，GamePlay 保留兼容转发。
- 已生成 LevelEventNode、EnemyWave、SaveableObject 三个模板 Prefab。
- Unity 编译 0 错误；场景契约校验 0 错误、0 警告；PlayMode 初始化无错误。

### 已完成：Level01 剩余关卡事件迁移

- [x] 文档、后门、危险标志、管道四段事件改为独立命名节点与敌人波次。
- [x] 危险标志事件使用 `OpenTrapAction`；开场音乐使用独立开始事件。
- [x] Q1/Q2 任务门改从 `LevelContext` 获取；`Level1Flow` 和数组下标入口已删除。
- [x] Level01 PlayMode 事件回归通过：六个事件、四波敌人、机关动作与单次触发。

### 已完成：玩家旧配置直接替换

- `Assets/GameData/Player/Player.asset` 是唯一玩家策划配置入口。
- Player Prefab、背包和 GameContentCatalog 已直接引用 `PlayerDefinition`。
- 已删除无引用的旧 `PlayerConfig` 类型、脚本与资产。
- 编辑器工具改为 `Rebind Player Definition`，只负责重新绑定统一配置。

### 已完成：物品 ID 合并与 PlayMode 回归

- [x] Box 改用 ID 30；两份手枪弹药配置合并为一个，拾取 Prefab 决定 10 发或 1 发。
- [x] 旧弹药稳定 ID 和旧存档 ID 0 可恢复到正确物品；掉落固定只生成 1 件。
- [x] Level01 PlayMode 回归拾取、背包图标、治疗、蓝绿药粉组合及丢弃重拾通过。
- [x] `Validate Game Content` 通过，Unity 控制台 0 错误。

不建议现在引入大型 DI、通用事件总线、可视化节点编辑器或全面 ECS。这些会增加学习成本，当前用明确装配、窄接口、强类型事件和动作组件已经足够支撑独立游戏团队协作。
