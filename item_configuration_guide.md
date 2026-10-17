# 物品配置说明

## 新建物品

1. 在 Project 窗口选择 Create > Item > Item Config。
2. 填写“显示名称、交互类型、默认数量”。
3. 配置背包图标、世界拾取 Prefab 和查看模型 Prefab。
4. 可使用物品选择效果类型并填写效果数值。
5. 可组合物品勾选“允许组合”，配置组合材料与组合结果。
6. 运行 Tools > 3D Adventure > Validate Game Content。
7. 新增资产后选中 GameContentCatalog，点击 Collect All Game Content。

## 字段含义

- **旧版数字 ID**：兼容当前背包、武器弹药和旧存档。新增物品仍需保持唯一。
- **世界拾取 Prefab**：物品丢弃或生成在场景中时使用。
- **拾取数量**：在世界拾取 Prefab 的 `ItemBase` 中填写；0 使用物品配置的默认数量。手枪弹药的 10 发与 1 发共用一份配置。
- **查看模型 Prefab**：拾取时的物品查看界面使用。
- **效果类型**：当前支持无效果和恢复生命。
- **组合材料**：背包中需要同时消耗的另一件物品。
- **组合结果**：组合后加入背包的物品。

## 旧资产迁移

现有物品已迁移到第二版配置，并保留旧 DTO 字段用于读取旧存档。也可以在单个物品 Inspector 点击“自动迁移旧物品引用”，或运行 Tools > 3D Adventure > Migrate Item Configs。

## 提交前检查

- 显示名称不能为空。
- Pickup 类型应配置背包图标。
- 可组合物品必须同时配置组合材料和组合结果。
- 数字 ID 必须唯一。Box 使用 30，手枪弹药使用 0；旧存档会通过稳定 ID 或物品名称映射到当前配置。
- Prefab 上的 ItemBase.itemConfig 必须指向对应的 Item Config。

## PlayMode 回归

打开 Level01 后运行 `Tools > 3D Adventure > Run Item PlayMode Regression`。工具会在 PlayMode 中检查弹药拾取与叠加、背包图标、治疗、蓝绿药粉组合、丢弃后重新拾取和旧存档 ID 恢复，并自动退出 PlayMode。最后运行 `Validate Game Content` 检查资源配置。
