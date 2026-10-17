# PlayerDefinition 配置指南

玩家的策划配置统一位于 `Assets/GameData/Player/Player.asset`。关卡中的 Player Prefab、GamePlay 背包和 `GameContentCatalog` 都引用这一份资产。

## 策划可以修改的内容

- **基础信息**：显示名称和策划说明。
- **生存属性**：最大生命、初始防御、有防御时的生命伤害比例。
- **移动与相机**：重力、转向速度、相机灵敏度和上下视角限制。
- **交互检测**：物品交互、敌人处决、地面检测和战术移动半径。
- **初始物品**：直接引用 `ItemConfig` 并填写数量，不再填写数字 ID 或运行时 `Item` 数据。
- **初始武器**：开局拥有的 `WpConfig` 列表。
- **默认远程武器**：按瞄准键时尝试切换的武器，不会自动授予玩家。
- **近战武器**：近战攻击、格挡和耐久所使用的武器定义。当前配置引用 `Knife`。

## 不应放进 PlayerDefinition 的内容

场景中的相机、UI、骨骼挂点、武器模型和碰撞组件仍由 `Player.prefab` 的 `PlayerControl` 连接。这些对象不能安全地保存在 ScriptableObject 中。`PlayerControl` Inspector 已按相机、武器表现、战斗挂点和场景 UI 分组。

当前生命、防御、刀耐久、持有武器、弹药和状态机状态属于运行状态，由 `PlayerControl` 和存档系统维护。修改 PlayerDefinition 不会覆盖读档后的运行数据。

## 常用流程

1. 修改 `Assets/GameData/Player/Player.asset`。
2. 如果新增开局物品，拖入对应 `ItemConfig` 并填写数量。
3. 如果新增开局武器，拖入对应 `WpConfig`，并确认 Player Prefab 的 `weaponModels` 中有匹配的武器表现对象。
4. 执行 `Tools > 3D Adventure > Validate Game Content` 检查配置。
5. 需要重新绑定 Player Prefab、背包和内容目录时，执行 `Tools > 3D Adventure > Rebind Player Definition`。

## 运行时连接

项目只使用 `PlayerDefinition` 作为玩家配置入口。状态机通过 `PlayerControl` 的稳定接口访问重力、转向、武器和运行状态，场景 UI 读取 `MaxHealth` 等只读接口。
