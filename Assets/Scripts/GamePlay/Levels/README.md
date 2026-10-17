# LevelFlow 模块

本目录负责关卡装配、条件、动作、事件和敌人波次。

- `LevelInstaller`：场景唯一装配入口。
- `LevelContext`：提供当前场景的玩家、GamePlay、EnemyRegistry 和出生点引用。
- `LevelEventTrigger`：判断触发方式和条件，并顺序执行强类型 LevelAction。
- `EnemyWave`：组织 EnemySpawn，不处理剧情条件。
- Level01 的剧情条件和刷怪已由命名事件节点与 EnemyWave 配置，任务门由 LevelContext 提供引用。

Level01 场景层级：

- `__Level/Events`：六个关卡事件；`__Level/SpawnPoints`：玩家与敌人出生点。
- `__Gameplay/Systems`：GamePlay；`Actors`：玩家与敌人；`Interactables`：门、拾取物和可破坏物；`Triggers`：场景触发器；`Markers`：瞄准点。
- `__Presentation/UI`：主界面与 EventSystem。
- `__Environment/Level01`：Terrain、Locations、StreetProps；另有 Blockout 与 Navigation。
- `__Lighting`：主灯、反射探针和全局 Volume。

MonoManager、AudioManager、PoolManager、SaveManager 在场景根级，因为它们会在运行时调用 `DontDestroyOnLoad`。不要把它们放到上述分组下。

新代码通过 LevelContext 取得场景依赖，不新增 GameObject.Find、对象名查找或 GamePlay.Instance。
