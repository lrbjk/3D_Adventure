# 3D Adventure 代码整改清单

> 来源：2026-09-16 全项目架构与逻辑审查  
> 状态：待处理  
> 当前验证：Unity 编辑器程序集编译通过；尚未执行 Player Build 和完整 PlayMode 回归。

## P0｜阻断性问题

- [ ] **修正状态机每帧执行三次**
  - 位置：`StateMachine.cs`
  - 动作：将 Update、LateUpdate、FixedUpdate 分别注册到对应通道；退出时成对解绑；补齐空值和返回值处理。
  - 验收：三个回调每帧/物理帧各执行一次，切换状态后旧状态不再收到回调。

- [ ] **阻断自动读档与场景重复加载闭环**
  - 位置：`GamePlay.cs`、`SaveManager.cs`
  - 动作：自动读档仅由 Bootstrap 调用一次；建立统一 `SceneLoader`；关卡内 `GamePlay.Start` 不再触发读档。
  - 验收：读档只加载一次目标场景，加载完成后按固定顺序恢复玩家和世界状态。

- [ ] **重做武器存档格式**
  - 位置：`SaveManager.cs`
  - 动作：使用 `weaponIds`、`currentWeaponId` 和弹药数等稳定值；通过 `WeaponCatalog` 恢复配置。
  - 验收：关闭并重启游戏后，武器、当前装备和弹药均可正确恢复。

- [ ] **修复可破坏物存档读取错误数组**
  - 位置：`SaveManager.cs`
  - 动作：可破坏物统一读取 `dobjs`；用稳定 GUID/ID 代替 GameObject 名称。
  - 验收：交互物与可破坏物数量不同、存在同名对象时仍能准确恢复。

## P1｜高优先级逻辑漏洞

- [ ] **修复自动档槽位和空档处理**
  - 使用 `lastID` 对应槽位；检查范围、文件、JSON、版本和空结果；失败时禁止继续切场景。
  - 验收：空档、损坏档和非 0 槽位均有确定结果且不抛异常。

- [ ] **修复任务前置条件和末尾越界**
  - 前置条件只按完成状态判断；最终任务进入 `AllCompleted`；字典访问使用 `TryGetValue`；重复 ID 阻止初始化。
  - 验收：缺失前置、同类型前置、重复 ID、最后任务完成均不会错误放行或越界。

- [ ] **修复敌人列表重复引用**
  - 一次获取全部敌人并逐个注册，或改用 `EnemyRegistry` 的 OnEnable/OnDisable 注册机制。
  - 验收：列表中每个存活敌人只出现一次，销毁或禁用后正确注销。

- [ ] **加固对象池归属与容器管理**
  - 按 prefab/显式 `PoolId` 注册；Push 时校验归属、创建容器、防止重复入池；不复用的场景物直接禁用。
  - 验收：首次入池、重复入池、同名不同 prefab、清池后再取均正常。

- [ ] **修复池化敌人的状态机生命周期**
  - OnDisable 暂停并解绑；OnEnable 重置并恢复；区分 Pause/Resume 与永久 Stop。
  - 验收：禁用敌人不再收到全局更新，重新取出后只注册一次。

- [ ] **消除射击时每帧创建协程**
  - 仅在射击事件启动一次计时，或使用时间戳控制表现持续时间。
  - 验收：持续观察射击后协程数量不会按帧增长。

- [ ] **重写 AI 感知状态提交**
  - 每轮扫描先计算局部结果再统一更新；使用最后看见时间遗忘；`viewAngleStep >= 1`。
  - 验收：离开视野后检测和攻击状态按时清除，步数为 0 不会除零。

- [ ] **移除运行时代码的 UnityEditor 依赖**
  - 位置：`ItemBase.cs`、`BackPackUI.cs`
  - 删除未使用引用；编辑器逻辑放入 `#if UNITY_EDITOR` 或 Editor 程序集。
  - 验收：Player Build 不再因 `UnityEditor` 命名空间失败。

- [ ] **修复背包掉落索引 -1**
  - 未注册物品时返回失败结果并记录 itemId，由调用方处理。
  - 验收：未知物品 ID 不会访问 `itemPrefabs[-1]`。

## P2｜结构与维护性

- [ ] **分离 ScriptableObject 配置与运行时状态**
  - ScriptableObject 仅保存设计数据；新增 `PlayerRuntimeState`、`InventoryState`、`WeaponRuntimeState`。

- [ ] **统一暂停、输入和光标所有权**
  - 建立 `GamePauseController`/`UIStateCoordinator`，按原因或引用计数申请和释放暂停。

- [ ] **修复局部逻辑错误**
  - `GiveItem` 随机分支返回同一物品。
  - 弹药物品 ID 被当成数组下标。
  - 死亡 UI 淡入约 60 秒且最终透明度仅 0.2。
  - 新提示会被旧协程提前隐藏。
  - 武器 ID 被直接当作模型数组下标。
  - 重复玩家实例的 InputAction 未释放。
  - `AutoRollText` 存在不可达代码且速度依赖帧率。

- [ ] **收敛模块边界**
  - Bootstrap / SceneFlow
  - GameSession
  - Player & Combat
  - Enemy & AI
  - Inventory & Items
  - Quest & Progression
  - Presentation

- [ ] **拆分基础程序集**
  - 建议起步：`Game.Core`、`Game.Runtime`、`Game.UI`、`Game.Tests`。

- [ ] **最后处理性能热路径**
  - 降低 AI 射线频率；清理高频日志、空 Update；减少 UI 的 Instantiate/Destroy。

## 回归验证

- [ ] 状态的 Update/LateUpdate/FixedUpdate 各执行一次。
- [ ] 空档、损坏档、非 0 槽位、跨进程武器恢复通过。
- [ ] 同名世界对象和不同类型可破坏物恢复正确。
- [ ] 最终任务、缺失前置任务、重复任务 ID 均正确处理。
- [ ] 对象池首次入池、重复入池、清池后再取通过。
- [ ] 背包与存档 UI 叠加时，暂停不会提前释放。
- [ ] 完成 Player Build。
- [ ] 完成核心流程 PlayMode 回归。

## 建议执行顺序

1. 状态机更新通道。
2. SceneLoader 与自动读档闭环。
3. 存档 DTO、稳定 ID、版本和异常处理。
4. 可破坏物、任务、敌人列表、对象池、背包越界。
5. 运行时状态迁移。
6. 单例、池化和 InputAction 生命周期。
7. asmdef 拆分。
8. 性能清理与完整回归。
