# 策划配置动态资源

Level01 的资源配置入口是 `Assets/GameData/Resources/Level01ResourceBalance.asset`，候选物品在同目录的 `Level01ResourceDrops.asset`。`GamePlay` Prefab 上的 `ResourceDirector` 引用该配置。系统只控制消耗品；钥匙、任务物品和关卡必需奖励保持固定放置。

## 当前默认投放

| 来源 | 基础掉落率 | 说明 |
| --- | ---: | --- |
| 敌人 | 18% | 未掉落时没有拾取物 |
| 随机箱子 | 55% | 破碎效果照常播放，箱子可为空 |
| 随机门 | 35% | 一次开门只结算一次，空结果不能重复刷 |

候选池：手枪弹药权重 6，每次 3–6 发；绿色药粉权重 3，每次 1 个；急救喷雾权重 1，每次 1 个。弹药只会在玩家拥有对应枪械时进入候选池。枪内子弹和背包弹药一起计算：总数不超过 8 时，弹药权重乘 2.5；达到 24 时乘 0.2。低于 40% 血量且没有可直接使用的治疗品时，治疗权重乘 2.5；拥有至少两件治疗品时乘 0.2。所有候选资源都充裕时，掉落率也乘 0.2。

真正缺资源时，连续四次空结果后下一次有效结算会给一件资源；资源充裕时不启用保底。保底计数随存档保存和恢复。以上数值是当前 Level01 的起始调参值，体验仍需按完整关卡通关记录调整。

## 修改配置

1. 在 `Level01ResourceBalance` 调来源概率、弹药阈值、低血量阈值、权重倍率和最大连续空结果。
2. 在 `Level01ResourceDrops` 增删候选项。每项直接引用 ItemConfig 和对应的世界拾取 Prefab；`minAmount`、`maxAmount` 是最终拾取数量，不是生成多个 Prefab。
3. 敌人需要专属消耗品池时，在 EnemyArchetype 的 LootTable 引用另一张表。未配置时使用默认池。
4. 固定奖励继续使用 `Box.isRandom = false`、`Door.isRandomSpwan = false`，或敌人的 `isRandom = false` 加固定 `prefab`。随机门只需勾选 `isRandomSpwan`，不需要填写固定掉落 `prefab`；`spwanPoint` 留空时使用门自身位置。剧情必需物品不要放进动态池。
5. 运行 `Tools > 3D Adventure > Validate Game Content` 检查引用，再用 `Run Dynamic Resource Regression` 做 PlayMode 回归。

配置目标是让玩家在弹药耗尽时考虑切枪或近战，在受伤时权衡何时使用治疗品；不是模仿任何作品未公开的内部概率算法。设计参考了 [Capcom《Resident Evil 4》官方手册](https://static.capcom.com/manuals/re4/RE4_GC_DMNL_EN.pdf)对弹药耗尽、切换武器、治疗和物品整理的玩家决策说明。
