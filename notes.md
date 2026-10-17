# Notes: Unity 项目架构与逻辑审查

## 玩家系统四步重构（2026-09-28）

- `PlayerControl.currentState` 改为从 `StateMachine.CurrentState` 推导，瞄准/换弹移动子状态通过各状态类公开只读属性表达；状态类不再手动写入平行枚举。
- 近战入口使用 `PlayerDefinition.meleeWeapon` 配置引用，保留旧数字 ID API 给背包和远程武器切换使用。
- `PlayerControl` 发布 `PresentationChanged`/`StateChanged`；`GamePlay` 监听并更新生命、近战耐久、弹药、准星、QTE、武器图标和死亡界面。
- `SaveManager` 仅解析存档格式和资产引用；`PlayerControl.RestoreFromSave` 负责运行时数据、装备、位置和表现恢复。
- Roslyn 编译运行时与 Editor 程序集均成功。UnitySkills Python helper 缺少 `requests`，未运行 PlayMode；需要人工回归死亡重开、拾刀近战、QTE、读档恢复 HUD。

## Root Motion 运动重构（2026-09-29）

- PlayerMotionMotor 是单一运动入口：读取 Move/Run 输入、相机相对方向、角度转向、动画混合参数、Root Motion 倍率、垂直速度、CharacterController 碰撞和脚步距离。
- PlayerBaseState 默认 Action 模式；Idle、Move、Aim、Reload、Fall 选择各自模式；TC 与 Hit 选择 Cinematic 以保留原动画纵向演出。
- `PlayerDefinition` 新增走跑/瞄准/动作位移倍率、输入阈值、混合阻尼、跑步急转角、最大落速、脚步间距，并在 Inspector 与现有 Player.asset 中显式配置。
- Unity 6 Roslyn 编译运行时和 Editor 均通过；Unity AssetDatabase.Refresh 已导入新脚本，编辑器日志无 C# 编译错误。UnitySkills REST 服务在 Domain Reload 后未恢复，PlayMode 运动手感与动画配置需进入编辑器回归。
- 转向粘滞修正：Player.asset 的转速从 90 提到 540 度/秒，跑步急转阈值从 120 提到 150 度。普通走跑不叠加动画旋转，急转动画保留 Root Motion 旋转。Unity Level01 PlayMode 启动后无运行时错误，已退出；未模拟玩家输入，手感仍需人工确认。

## 项目基线

- Unity `6000.3.12f1`，URP/Core/ShaderGraph `14.0.11`。
- 自有代码集中在 `Assets/Scripts`，约 100 个 C# 文件，均编入默认 `Assembly-CSharp`；没有自有 asmdef。
- 主要系统：玩家状态机、敌人状态机/Behavior Designer、物品与背包、任务、存档、对象池、关卡流程、UI。
- 自有测试只有手工测试脚本 `ItemTest.cs`，没有 NUnit/Unity Test Framework 测试。
- 单例访问非常密集：`GamePlay.Instance` 111 处、`ObjectPool.Instance` 52 处、`AudioManager.instance` 39 处、`BackPack.Instance` 28 处。
- 用 Unity 6 自带 Roslyn 对当前 `Assembly-CSharp.rsp` 编译成功；自有警告包括旧 `FindObjectOfType`、`AutoRollText` 不可达代码、状态字段未使用。

## 已确认的高风险逻辑

1. `StateMachine` 把 `LateUpdate`、`FixedUpdate` 都注册到 `updateAction`，因此每个状态一帧被调三次；退出也使用错误通道。
2. `GamePlay.Start -> SaveManager.AutoLoading -> LoadSceneAsync` 会在关卡每次创建时再次触发自动读档，存在重复加载同一场景的闭环；加载前还错误地按目标关卡查找并卸载场景。
3. 存档里的 `WpConfig`/`currentWp` 实际只写出 `instanceID`。现有真实档案已验证；该 ID 不能作为跨进程稳定标识。
4. `SaveManager.RefreshObjs` 构造可破坏物状态时读取 `objs[i]`，不是 `dobjs[i]`；保存可破坏物状态时也遍历错数组。现有档案的 `destroyState` 只有一个错误的 `Box (4)`，与代码问题一致。
5. `AutoLoading` 检查 `recordName[lastID]`，随后固定加载 0 号档并把 `lastID` 改回 0；载入对象为空时无防御。
6. `QuestManager` 的前置条件表达式会放行“同类型但未完成”的前置任务；最后一个任务完成后索引持续越界增长，后续事件可抛出异常；重复 ID 先警告后仍 `Add`，会抛异常。
7. `GamePlay` 敌人初始化循环每次都调用 `FindGameObjectWithTag`，列表中加入同一个敌人多次。
8. `ObjectPool.PushObject` 接受任意场景对象但不创建 `parentContainers`；再次 `GetObject` 时会索引不存在的容器。池键只用对象名，也会发生不同 prefab 冲突。
9. `WeaponControl` 在 `isShoot` 为真期间每帧启动一个 1.5 秒协程；会制造大量并行计时器。
10. 敌人死亡后被对象池禁用，但纯 C# 状态仍订阅持久化 `MonoManager`；禁用对象仍每帧收到回调，池越大泄漏越明显。
11. `BackPack.GiveItemPrefab` 未检查 `FindIndex` 的 `-1`；未注册 ID 会直接数组越界。
12. `AIDetect` 在发现玩家后停止唯一的重置协程且不恢复，`isDetected` 可能永久为真；无命中时攻击标记也可能保留旧值；`viewAngleStep=0` 会除零。
13. `ItemBase.cs` 和 `BackPackUI.cs` 在运行时程序集直接 `using UnityEditor`，Player Build 存在编译失败风险。

## 架构发现

- `GamePlay` 同时管理 UI、输入、掉落、玩家引用、敌人列表、死亡界面和加载流程，责任过多。
- `PlayerConfig` 与 `WpConfig` 既是静态配置又承载运行时背包、武器和弹量，导致资产数据、存档数据、会话状态混在一起。
- `Time.timeScale`、光标和输入启停由背包、存档 UI、物品展示分别直接修改；多个界面叠加时，关闭任一界面会错误恢复游戏。
- 玩家、音频、任务、存档、对象池、部分 UI 使用多套不同单例实现，生命周期规则不一致。
- 玩家是 `DontDestroyOnLoad`，关卡 `GamePlay` 是场景对象，存档服务持久化；场景重建期间通过全局静态引用互相查找，初始化顺序脆弱。
- 敌人既存在自研状态机又存在 Behavior Designer Task，形成两套行为表达方式，维护成本高。

## 性能与可维护性

- 约 80 个 Update/FixedUpdate/LateUpdate 声明，其中多个为空；97 次 `GetComponent`，许多在触发器或热路径重复调用。
- `AIDetect` 对每个敌人每个 FixedUpdate 发射 `viewAngleStep+1` 条射线。
- 运行时保留大量 `Debug.Log`，子弹默认开启详细日志。
- 背包 UI 每次开关全量 Instantiate/Destroy；当前体量可用，但应在功能正确后再考虑复用。

## 验证限制与错误

- 系统 `dotnet build` 不可用，因为安装的运行时不含 .NET SDK。改用 Unity 自带 Roslyn 和 Bee 生成的响应文件完成编辑器程序集编译。
- 未执行 Player Build，也未运行完整游戏流程；场景和时序问题来自静态调用链、场景配置与现有存档交叉验证。
# 架构实现补充记录（2026-09-16）

- 当前项目没有 Git 元数据；修改必须依赖文件备份意识和编译验证。
- 保留现有场景序列化字段，新增系统优先采用旁路兼容方式。
- 首批基础问题：StateMachine 更新通道错误、对象池容器缺失/重复入池、自动读档闭环、存档空值与槽位错误。
- 策划层首批模块：稳定内容 ID、LootTable、EnemyWave、LevelEvent、项目内容验证器。
- 运行时与 Editor 程序集均通过 Unity 2022.3 Roslyn 编译；仅剩第三方弃用警告和 AutoRollText 原有不可达代码警告。
- UnitySkills REST 无活动注册实例，未能从工具端执行 AssetDatabase.Refresh 或读取 Unity Console。
