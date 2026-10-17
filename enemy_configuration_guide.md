# EnemyArchetype 敌人配置指南

敌人的数值和玩法配置统一放在 `Assets/GameData/Enemies`。场景与 Prefab 中的 `EnemyBase` 只保留组件引用，并通过 `Archetype` 指向一份敌人原型资产。

## 新建敌人

1. 在 Project 窗口选择 `Create > Enemy > Enemy Archetype`。
2. 配置身份、基础属性、硬直与恢复、部位耐久、移动与寻路、感知距离、技能、动画状态名、音效和掉落。
3. 创建敌人 Prefab，并把资产拖到 `EnemyBase > Archetype`。
4. 把敌人 Prefab 回填到原型资产的 `Enemy Prefab` 字段。
5. 打开 `GameContentCatalog`，点击 `Collect All Game Content`。
6. 执行 `Tools > 3D Adventure > Validate Game Content`。

## 配置归属

- `EnemyArchetype`：策划可调整的共享配置。
- `EnemyBase`：运行时状态、组件引用和状态机。
- `EnemyModel`：Animator、受击盒、特效和模型节点引用。
- `AIDetect`：运行时检测状态；检测半径和攻击距离由原型注入。
- `SkillConfig`：单个攻击技能的动画事件、倍率和命中盒索引。
- `LootTable`：数据化掉落；未配置时兼容旧的固定或动态掉落规则。

## 现有资产

项目现有 7 个敌人 Prefab 已迁移为独立原型。旧 `EnemiesInfo` 类型暂时保留用于兼容历史资产，新内容统一使用 `EnemyArchetype`。

如需重新同步旧 Prefab，可执行 `Tools > 3D Adventure > Migrate Enemy Archetypes`。该命令会更新 `Assets/GameData/Enemies` 中同名资产并重建内容目录引用。
