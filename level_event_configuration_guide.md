# 策划配置关卡事件

关卡事件使用 `LevelEventTrigger`、`LevelCondition` 和 `LevelAction` 搭建。Level01 的 `__Level/Events/MachineRoom_Key1_Event` 是完整样板，模板位于 `Assets/Prefab/Templates`。

## 场景固定结构

每个可玩关卡需要五个根节点：

- `__Level`：唯一的 `LevelInstaller`、`LevelContext`、`EnemyRegistry`；Events 中放事件节点和敌人波次，SpawnPoints 中放出生点。
- `__Gameplay`：玩家、交互物和玩法对象的组织入口。
- `__Presentation`：HUD、相机和纯表现对象。
- `__Environment`：场景模型和碰撞。
- `__Lighting`：灯光、探针和后处理。

`LevelContext` 的玩家、GamePlay 和 EnemyRegistry 都是必填引用。运行 `Tools > 3D Adventure > Validate Level Scene Contract` 可以检查场景结构。

## 新建事件

1. 复制 `LevelEventNode_Template` 到 `__Level/Events` 下。
2. 填写事件 ID、显示名称和策划备注。事件 ID 发布后不要随意修改。
3. 选择触发方式：手动、开始时、玩家进入或条件满足时。只有“玩家进入”需要 Collider；改为其他方式时可移除模板自带的 BoxCollider。
4. 配置条件。多个条件需要同时满足；勾选 Invert 可以反转单个条件。
5. 在同一对象上添加需要的 Action 组件，并按执行顺序拖入 Actions 列表。
6. Inspector 显示“配置完整”后，再运行场景契约校验。

## 可用 Action

- `SpawnWaveAction`：执行一个 EnemyWave。
- `SetActiveAction`：显示或隐藏目标对象。
- `OpenDoorAction`：解锁并按需立即打开 Door。
- `StartQuestAction`：开始指定 QuestInfo。
- `ShowTipAction`：在 HUD 显示提示文字。
- `PlayAudioAction`：按 AudioManager 中的声音名称播放音效或音乐。
- `SaveGameAction`：执行自动存档。
- `RigidbodyImpulseAction`：解除刚体 Kinematic 并施加局部方向冲击力。
- `CameraShakeAction`：播放 SmoothCameraShaker 的 ShakeData。
- `OpenTrapAction`：调用场景 Trap 的 OpenTrap，适用于危险标志等机关事件。

核心玩法连接放入 Actions。UnityEvent 只用于按钮和纯表现回调。

## 敌人波次

复制 `EnemyWave_Template`，在 Entries 中直接引用 EnemySpawn 并填写逐个出生的延迟。禁止留下空出生点。事件通过 `SpawnWaveAction` 引用该波次，不再记住 `enemySpawnPoints[2]` 之类数组下标。

## Level01 机房样板

`MachineRoom_Key1_Event` 使用“条件满足时”触发：

1. 检查背包是否拥有 Key1。
2. 生成原 `enemySpawnPoints[2]` 对应敌人。
3. 解除机房门刚体锁定并施加原有方向和力度。
4. 播放 DoorDown 音效。
5. 播放原 ShakeData。

## Level01 已迁移事件

| 事件 ID | 触发条件 | 主要动作 |
| --- | --- | --- |
| `level01.machine_room.key1` | 拥有 Key1 | 机房敌人波次、门体冲击、音效、镜头震动 |
| `level01.document.first_wave` | 首份文档变为非激活 | 激活 Trigger1，生成第一波敌人 |
| `level01.back_door.wave` | Trigger2 变为非激活 | 生成后门敌人波次 |
| `level01.danger_sign.butcher` | 危险标志变为非激活 | 打开 Trap，生成屠夫 |
| `level01.tube.alpha` | TubeTrigger 变为非激活 | 生成管道敌人 |
| `level01.music.start` | 关卡开始 | 播放关卡背景音乐 |

四个条件事件都只触发一次。任务门引用由 `LevelContext` 提供，`Level1Flow` 已从 Level01 场景和代码中移除。修改事件后运行 `Tools > 3D Adventure > Validate Level Scene Contract`；程序员可再运行 `Run Level01 Event Regression` 做 PlayMode 回归。
