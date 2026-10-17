# 3D Adventure 渐进式架构实施说明

## 项目定位

当前架构按小型独立游戏设计：保留现有 Singleton、MonoBehaviour、ScriptableObject、Behavior Designer 和组件式写法，不增加全局 DI 框架或大型事件总线。

模块边界：

1. Bootstrap / SceneFlow：启动、服务创建和场景加载。
2. GameSession：存档、暂停和跨场景会话状态。
3. Player & Combat：玩家状态机、武器和运行时弹药。
4. Enemy & AI：敌人配置、感知、波次和池化生命周期。
5. Inventory & Items：物品定义、背包和掉落表。
6. Quest & LevelFlow：任务状态、条件触发器和关卡事件。
7. Presentation：UI、提示、光标和输入表现。

## 已完成的基础改造

- StateMachine 的 Update、LateUpdate、FixedUpdate 已绑定到正确通道，并支持 Pause/Resume。
- 玩家和池化敌人在禁用时暂停状态回调，销毁时释放状态机。
- 对象池会自动创建容器、防止同一实例重复入池，并清理实例记录。
- 关卡启动不再自动读档，避免读档后反复加载同一场景。
- 存档槽位、空档、损坏 JSON、临时文件写入和场景 Single 加载已加固。
- 交互物和可破坏物分别读取正确集合；SaveableId 可替代对象名称进行恢复。
- 武器弹药使用 PlayerControl 的运行时字典，不再持续写入 WpConfig 资产。
- 背包使用运行时 Dictionary；`PlayerDefinition.startingItems` 只作为初始配置来源，并直接引用 `ItemConfig`。
- 玩家生命、防御、移动、相机、检测半径和初始装备集中到 `PlayerDefinition`；Player Prefab 只保留状态机运行数据与场景引用。
- 暂停使用原因集合，背包、物品查看、存档菜单和死亡界面不会互相提前释放暂停。
- AI 感知按一次扫描统一提交结果，并修复视野步数为零和检测状态残留。
- 射击表现只保留一个计时协程。
- 任务前置条件、重复 ID、缺失任务和最终任务边界已加固。

## 新增的策划搭建模块

### GameContentCatalog

统一收集 ItemConfig、WpConfig、EnemiesInfo 和 QuestInfo，通过 StableId 查询。用于存档恢复和内容校验。

项目已创建 `Assets/Resources/GameContentCatalog.asset`，并收录现有 10 个物品、3 把武器、6 类敌人和 2 个任务。现有 33 个内容资产均已写入唯一 StableId。

后续维护：

1. 新增或删除配置资产后，选中 `GameContentCatalog.asset`。
2. 在 Inspector 点击 `Collect All Game Content` 重新收集。
3. 目录默认从 Resources 自动加载，也可以将资产直接赋给 SaveManager 的 Content Catalog 字段。

### SaveableId

需要保存激活状态的门、箱子、可破坏物添加 SaveableId。旧场景仍会按名称恢复，新内容应使用稳定 ID。

复制带 SaveableId 的对象后，运行 `Tools > 3D Adventure > Validate Game Content`。发现重复时，在其中一个对象的组件菜单执行“生成新的存档 ID”。

### LootTable

通过 `Item > Loot Table` 创建。每项配置物品、Prefab、权重和数量范围。赋给 GamePlay.defaultLootTable 后，旧 GiveItem 硬编码仅作为回退。

### EnemyWave

在场景空对象上添加 EnemyWave，将多个 EnemySpawn 组成命名波次并配置延迟。Level1Flow 已提供 firstWave、butcherWave、backWave、alphaWave；字段未配置时继续使用旧数组下标。

### LevelEventTrigger

支持 Manual、OnStart、PlayerEnter 和 WhenConditionsMet 四种触发方式。内置条件：

- GameObject 激活状态
- 门是否打开
- 是否拥有物品
- 任务状态
- 场景是否没有活动敌人

满足条件后通过 UnityEvent 调用 EnemyWave.SpawnWave、QuestManager.RequestStartQuest、GamePlay.ShowTips、SaveManager.AutoSave、GameObject.SetActive 等现有公开方法。

## 推荐迁移顺序

1. 打开 Unity，确认 `GameContentCatalog.asset` 已完成导入；新增内容后点击 `Collect All Game Content`。
2. 给 Level01 中需要存档的交互物和可破坏物添加 SaveableId。
3. 创建默认 LootTable，赋给 GamePlay。
4. 将 Level1Flow 中的出生点按剧情分成四个 EnemyWave 并赋值。
5. 新事件改用 LevelEventTrigger；确认流程通过后再删除对应的 Level1Flow Update 判断。
6. 新任务优先使用通用条件触发器；只有出现新规则时才新增 QuestStep 类型。
7. 每次提交内容前运行 `Tools > 3D Adventure > Validate Game Content`。

## 验证结果与边界

- Unity 6000.3.12f1 自带 Roslyn：当前 Assembly-CSharp 编译通过，0 个 C# 错误。
- Unity 编辑器最近一次完整刷新：Assembly-CSharp 与 Assembly-CSharp-Editor 均编译通过，Shader 导入错误已清零。
- 33 个内容 StableId 均非空且互不重复；GameContentCatalog 的四类引用数量已核对。
- 新增编辑器工具可检查内容 StableId、场景 SaveableId、敌人出生点和空任务步骤。
- 已清理运行时代码中无效的 Visual Scripting、UIElements 等 using，以及普通高频 Debug.Log；保护性 Warning/Error 保留。
- 尚未执行完整 Player Build、PlayMode 流程、同名场景对象恢复和跨进程旧存档迁移实测。

## Loading 与近战补充修复（2026-09-18）

- 读档场景完成后等待新场景的 GamePlay、PlayerControl 和 BackPack 初始化，再恢复存档；超时或异常都会退出 Loading 状态。
- 死亡界面重试按钮改为 OnEnable 单次绑定，并读取 lastID 对应的有效存档，不再每帧重复注册或固定读取 0 号槽。
- GamePlay 优先绑定持久化 PlayerControl；重复玩家会立即禁用，避免场景切换时随机拿到待销毁实例。
- 近战通过 HitPart/EnemyBase 层级解析敌人子碰撞体；每次攻击窗口只结算一次，状态中断时强制关闭碰撞体。
- 刀耐久值统一 Clamp；读档后会重新激活当前武器模型并同步可攻击状态。
- 工作区没有 Git 仓库；当前无法提供 Git diff 或提交级回滚。

## 暂缓项目

- 暂不拆 asmdef。项目代码直接引用 Behavior Designer、DOTween、SmoothCameraShaker 和 Knife Effects 的默认程序集源码；自定义程序集不能反向引用 Assembly-CSharp。应先为这些源码建立或取得官方 asmdef，再按 Core → Runtime → UI → Tests 顺序拆分。
- 暂不开发节点编辑器。条件列表、EnemyWave 和 UnityEvent 足以覆盖当前体量，维护成本更低。
- Behavior Designer 继续只负责敌人决策，不用于任务和主关卡流程。

## 死亡后重新开始修复（2026-09-18）

- 死亡状态进入时只打开一次死亡界面，不再每帧重复创建淡入协程。
- 死亡状态会立即写入 `PlayerState.Dead` 并关闭玩家输入，避免死亡后仍能交互或切换状态。
- 重试按钮增加防重复提交；有有效存档时读最近存档，无有效存档时重载当前关卡。
- `PlayerControl`、`MainUI` 和 `AimPosition` 改为场景对象，重载时重新绑定当前关卡的相机、UI 和输入引用。
- 死亡 UI 的打开与关闭改为幂等操作，并在关闭时停止尚未结束的淡入协程和释放死亡暂停原因。
- Unity 6 Roslyn 编译通过，0 个 C# 错误；Unity Editor 刷新触发 Domain Reload 后 REST 服务未恢复，死亡重试仍需 PlayMode 手动回归。
