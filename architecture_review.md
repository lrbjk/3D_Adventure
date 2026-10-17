# 3D Adventure 架构与逻辑审查

## 结论

项目已经形成完整的小型游戏骨架，玩家、敌人、物品、任务、存档和关卡流程都有明确入口；`IDamage`、`Iinteractable`、配置型 ScriptableObject、纯 C# 状态类和对象池这些方向值得保留。

当前主要风险不是目录命名，而是生命周期与运行时数据所有权混乱。状态机调度、存档/场景加载、对象池和任务边界中存在会改变实际游戏结果的错误。建议先修复 P0/P1，再拆模块；现在直接引入大型框架会扩大修改面。

项目定位：**正在从原型进入可长期维护的小型游戏阶段**。

## 必须优先修复

### P0：状态机每帧错误执行三次

位置：`Assets/Scripts/Rainbow/Scripts/StateMachine/StateMachine.cs:38-47`

`currentState.Update`、`LateUpdate`、`FixedUpdate` 全部被注册到 `MonoManager.updateAction`。结果是一个状态在普通 `Update` 中连续执行三次，真正的 LateUpdate/FixedUpdate 没有状态回调。玩家移动、敌人 AI、QTE 扣血和输入判断都会被放大；旧状态还可能在调用 `ChangeState` 后继续执行本帧剩余回调。

修复：分别使用 `AddUpdateListener`、`AddLateUpdateListener`、`AddFixedUpdateListener`，退出时对应移除；`Stop()` 先判断 `currentState`；`ChangeState` 成功后返回 `true`。

### P0：自动读档与场景加载形成重复加载闭环

位置：`Assets/Scripts/GamePlay/GamePlay.cs:51-65`、`Assets/Scripts/SaveManager/SaveManager.cs:171-207`、`297-305`

每次关卡中的 `GamePlay.Start` 都调用 `AutoLoading()`；读档又加载存档中的关卡。新关卡创建新的 `GamePlay` 后会再次自动读档。当前逻辑还用目标关卡编号查找“旧场景”，手动卸载后再以 Single 模式加载，职责重复且时序脆弱。

修复：自动读档只能由启动场景/Bootstrap 调用一次。建立一个 `SceneLoader`，只执行 `LoadSceneAsync(target, Single)`，完成后再绑定场景对象并应用快照。关卡内 `GamePlay.Start` 不触发读档。

### P0：武器存档不能跨进程可靠恢复

位置：`Assets/Scripts/SaveManager/SaveManager.cs:73-88`、`121-132`、`241-250`

存档直接保存 `List<WpConfig>` 和 `WpConfig currentWp`。真实存档内容只包含 `{"instanceID":...}`；Unity 实例 ID 不是稳定存档 ID，重启游戏或进入正式构建后无法可靠还原这些 ScriptableObject。

修复：存档只保存稳定值，例如 `weaponIds`、`currentWeaponId`、`magazineAmmoByWeaponId`。加载时通过只读武器目录 `WeaponCatalog` 将 ID 映射回配置资产。

### P0：可破坏物存档读取了错误数组

位置：`Assets/Scripts/SaveManager/SaveManager.cs:59-65`、`149-163`

两处可破坏物逻辑都误用交互物数组 `objs`，其中 `RefreshObjs` 甚至可能因数组数量不同而越界。现有存档的 `destroyState` 只有错误的 `Box (4)`，已经体现这个问题。

修复：分别使用 `dobjs[i]` 和 `foreach (var obj in dobjs)`。不要以 GameObject 名称作为唯一键；在可存档对象上配置稳定 GUID/ID。

## 重要逻辑漏洞

### P1：自动档槽位与空档处理错误

位置：`Assets/Scripts/SaveManager/SaveManager.cs:261-265`、`297-305`

代码检查 `recordName[lastID]` 后固定调用 `LoadPlayerData(0)`，随后强制 `lastID=0`。文件不存在或 JSON 损坏时，`ForLoadPlayerData(null)` 会直接空引用。

修复：加载 `lastID` 对应槽位；验证槽位范围、文件存在、反序列化结果和存档版本；失败时返回明确结果，不能继续切场景。

### P1：任务前置条件和末尾边界错误

位置：`Assets/Scripts/Quest/QuestManager.cs:52-83`、`96-101`、`143-164`

- 前置任务未完成时，还额外要求“前置类型不同”才判失败，同类型前置任务会被错误放行。
- 最后一个任务完成后 `currentQuestIndex` 会继续增长；后续状态事件访问 `questInfo[currentQuestIndex]` 时越界。
- 重复任务 ID 只打印警告，随后仍然 `Add`，会抛出异常。
- `questsMap[id]` 会在“不存在”时先抛异常，后面的 null 检查无效。

修复：前置条件只判断完成状态；完成最终任务后进入明确的 `AllCompleted` 状态；使用 `TryGetValue`；重复 ID 应拒绝初始化并给出资产位置。

### P1：敌人列表加入同一个实例多次

位置：`Assets/Scripts/GamePlay/GamePlay.cs:55-61`

先取得敌人数量，循环中却反复调用 `FindGameObjectWithTag`。有 N 个敌人时，列表很可能包含同一个敌人 N 次。

修复：一次取得 `FindGameObjectsWithTag` 的结果并逐个读取组件。更好的做法是敌人在 OnEnable/OnDisable 向 `EnemyRegistry` 注册和注销。

### P1：对象池对场景对象不安全

位置：`Assets/Scripts/ObjectPool/ObjectPool.cs:20-59`、`84-104`

`ItemBase`、`Box` 等场景对象会直接调用 `PushObject(gameObject)`。`PushObject` 创建队列，却没有创建 `parentContainers[poolKey]`；下一次 `GetObject` 从队列取出对象后，访问容器字典会抛 `KeyNotFoundException`。仅用对象名作池键还会把同名不同 prefab 混在一起。

修复：池应按 prefab/显式 PoolId 注册；Push 时验证对象归属并确保容器存在；防止同一实例重复入队；场景摆放物如果不计划复用，应直接禁用或由专门的 Spawnable 组件接管。

### P1：敌人入池后状态仍由全局 MonoManager 驱动

位置：`Assets/Scripts/Enemy/EnemyModel.cs:96-115`、`Assets/Scripts/Enemy/EnemyBase.cs:103-121`、`641-644`

死亡敌人最终被禁用并放回对象池，但状态机只在 OnDestroy 时停止。禁用对象不会被销毁，因此其纯 C# 状态仍留在持久化 MonoManager 的委托列表，每帧继续调用并持有整个敌人对象图。

修复：OnDisable 暂停并解绑状态机，OnEnable 重置后重新进入初始状态。将 `Stop` 与永久销毁、`Pause/Resume` 与池化生命周期分开。

### P1：射击后每帧创建协程

位置：`Assets/Scripts/Weapons/WeaponControl.cs:115-140`、`213-217`

`isShoot` 设为 true 后，Update 每帧调用 `StartCoroutine(Delay(1.5f))`，直到第一个协程结束。一次射击可创建约 90 个并行协程。

修复：只在 `Shooting()` 中启动一个协程，启动前停止旧协程，或使用 `shootVisualUntil` 时间戳。

### P1：AI 感知状态可能永久残留

位置：`Assets/Scripts/Enemy/AIDetect.cs:25-33`、`39-100`、`119-123`

发现玩家时停止唯一的十秒重置协程，之后没有恢复。未命中玩家的射线也没有统一清理 `isDetected/canAttack/canFarAttack`。`viewAngleStep` 为 0 时会除零。

修复：一次扫描先计算局部结果，扫描结束后统一提交状态；用“最后看见时间”控制遗忘；在 OnValidate/Awake 强制步数至少为 1。

### P1：Player Build 存在 Editor 命名空间依赖

位置：`Assets/Scripts/Item/ItemBase.cs:3`、`Assets/Scripts/UI/BackPackUI.cs:4`

运行时程序集顶层直接 `using UnityEditor`。编辑器程序集目前可编译，但 Player Build 不应依赖 UnityEditor。

修复：删除未使用的 using；编辑器调用放到 `#if UNITY_EDITOR` 内，或移动到 `Editor` 目录和 Editor asmdef。

### P1：背包掉落未检查查找失败

位置：`Assets/Scripts/GamePlay/backpack/BackPack.cs:138-143`

`FindIndex` 返回 -1 时仍访问 `itemPrefabs[-1]`。

修复：未注册时返回 false/Result，并打印包含 itemId 的一次性错误。调用方决定是否使用默认掉落。

## 次要但值得改正

### P2：ScriptableObject 同时承担配置和运行时状态

`PlayerConfig.items/wps` 与 `WpConfig.bulletTotalNum` 在运行时持续修改。配置资产被多个对象共享，容易造成不同角色/存档互相污染，也让 Play Mode 重启行为难以预测。

修复：ScriptableObject 只保存只读设计数据；运行时使用普通 C# 状态对象：`PlayerRuntimeState`、`InventoryState`、`WeaponRuntimeState`。

### P2：暂停、输入和光标没有统一所有者

位置：`GamePlay`、`SaveUI`、物品展示逻辑。

多个界面分别把 `Time.timeScale` 直接设为 0 或 1。两个界面叠加时，关闭其中一个会提前恢复游戏和输入。

修复：建立 `GamePauseController`/`UIStateCoordinator`，使用原因集合或引用计数，例如 `AcquirePause(PauseReason.Inventory)` / `ReleasePause(...)`。

### P2：若干明显的局部逻辑问题

- `GamePlay.GiveItem` 多组随机分支都返回同一个 `items[2]`，概率判断没有实际效果；`ammos[minAmmoId]` 把物品 ID 当数组下标。
- `GamePlay.DieUITime` 每 3 秒增加 0.01 alpha，只循环 20 次，约 60 秒后透明度也只有 0.2。
- `ShowTips` 未取消旧协程，旧提示可能把新提示提前隐藏。
- `PlayerControl.ChangeWeapon` 使用武器 ID 直接索引 `weaponModels`，依赖 ID 必须是连续且从 0 开始。
- `PlayerControl` 的重复实例在确认单例身份前就创建并启用输入资产，销毁时又没有 Dispose；反复加载场景会泄漏输入资源。
- `AutoRollText` 有两处编译器确认的不可达 `break`，速度按帧而非按时间计算。

## 建议的目标架构

控制在七个模块以内：

1. **Bootstrap / SceneFlow**：唯一启动入口、服务创建、场景加载和加载后绑定。
2. **GameSession**：当前存档、暂停状态、会话生命周期；不直接管理具体 UI。
3. **Player & Combat**：玩家状态机、武器运行时状态、伤害与交互接口。
4. **Enemy & AI**：敌人状态/行为树二选一为主，统一注册表和池化生命周期。
5. **Inventory & Items**：背包状态、物品目录、使用/组合/掉落规则。
6. **Quest & Progression**：任务定义、运行时进度、前置条件与事件。
7. **Presentation**：HUD、背包、任务和提示 UI，只观察状态并发送命令。

存档可以作为 GameSession 下的服务，不必再增加大型框架层。

### 场景与初始化

- 建一个很小的 Bootstrap 场景，持久化对象只保留 `GameSession`、`SaveService`、`AudioService`、`SceneLoader`、`ObjectPool`。
- 明确玩家归属：要么作为 SessionRoot 的持久对象，要么每关创建并从快照恢复；不要同时依赖 `DontDestroyOnLoad` 和关卡 prefab。
- `LevelContext` 持有关卡 UI、出生点、敌人注册表和关卡流程。场景加载完成后由 SceneLoader 将它绑定到 GameSession。
- 所有加载均走一个入口，加载完成后按固定顺序：绑定场景 → 生成/定位玩家 → 应用存档 → 启用输入 → 关闭加载 UI。

### 数据所有权

- ScriptableObject：武器、物品、敌人、任务的只读定义。
- 普通可序列化类：生命、弹量、背包数量、任务进度、世界对象状态。
- MonoBehaviour：场景引用、Unity 生命周期和表现桥接，尽量保持薄。
- 存档引用：只保存稳定 ID 与值，不保存 GameObject、Component、ScriptableObject 或 instanceID。

### 通信规则

- 同一模块内使用序列化直接引用。
- 伤害、交互继续使用接口。
- 跨模块状态变化使用少量强类型事件，例如 `HealthChanged`、`InventoryChanged`、`QuestUpdated`；订阅必须在 OnEnable/OnDisable 成对。
- UI 不直接修改玩家内部字段，只调用命令或服务方法。
- 停止继续扩大 `GamePlay.Instance`；先把存档/加载、暂停、掉落规则三个责任移出。

## 推荐整改顺序

1. 修正 StateMachine 三个更新通道，并给状态切换写最小 EditMode 测试。
2. 移除 `GamePlay.Start` 自动读档；统一 SceneLoader，阻断重复场景加载。
3. 重做存档 DTO：稳定 ID、版本号、空档/损坏处理、原子写入；提供旧档迁移或明确废弃旧档。
4. 修正可破坏物数组、任务边界、敌人列表、对象池容器和背包 -1 越界。
5. 把 ScriptableObject 中的弹量、背包和武器列表迁移到运行时状态。
6. 统一单例与场景生命周期，清理禁用对象订阅和 InputAction 资源。
7. 拆 asmdef：`Game.Core`、`Game.Runtime`、`Game.UI`、`Game.Tests` 即可起步。
8. 最后处理热路径：AI 射线频率、日志、空 Update、UI Instantiate/Destroy。

## 验证建议

至少补以下自动测试：

- 状态切换后每帧 Update/LateUpdate/FixedUpdate 各执行一次。
- 空档、损坏档、非 0 槽位、跨进程武器恢复。
- 两个同名世界对象和不同类型可破坏物的存档恢复。
- 最后一个任务完成、缺失前置任务、重复任务 ID。
- 场景对象首次入池、重复入池、清池后再取。
- 背包/存档 UI 叠加时暂停状态不会提前释放。

当前验证结果：Unity 编辑器程序集编译通过；未执行 Player Build 和完整 PlayMode 回归。现有存档与静态调用链已经足以确认上述 P0/P1 中的大部分问题。
