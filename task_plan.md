# Task Plan: 3D Adventure 渐进式架构完善

## Goal
在保留现有代码风格和场景资产兼容性的前提下，把项目整理为适合小型独立游戏持续开发、支持策划配置并且可验证的架构。

## Phases
- [x] Phase 1: 读取现有审查结论、核心代码和适用规范
- [x] Phase 2: 修复状态机、对象池、自动读档和关键生命周期错误
- [x] Phase 3: 建立稳定 ID、配置目录与运行时数据边界
- [x] Phase 4: 加入 LootTable、EnemyWave、LevelEvent 等策划搭建模块
- [x] Phase 5: 迁移现有硬编码入口并保持旧场景兼容
- [x] Phase 6: 添加编辑器校验、编译验证和交付文档
- [x] Phase 7: 按 Notion 整改清单补齐存档、场景加载、对象池和局部逻辑修复

## Architecture Boundaries
1. Bootstrap / SceneFlow：服务初始化和唯一场景加载入口。
2. GameSession：当前会话、读档和暂停等跨场景状态。
3. Player & Combat：玩家、武器、伤害和运行时战斗状态。
4. Enemy & AI：敌人配置、生成、感知和池化生命周期。
5. Inventory & Items：物品定义、背包、掉落表和交互物。
6. Quest & LevelFlow：数据化任务、条件与动作。
7. Presentation：UI 只显示状态并发出命令。

## Decisions Made
- 保留现有 Singleton、ScriptableObject、MonoBehaviour 组件和命名风格，不引入新的第三方框架。
- 新架构先作为兼容层落地，再逐步迁移 Level01，避免一次性破坏场景序列化引用。
- 先使用 Inspector 列表型条件/动作，不开发复杂节点编辑器。
- 配置资产只保存设计数据；运行时弹药、背包、生命和任务进度逐步移出 ScriptableObject。

## Errors Encountered
- 工作区没有 Git 仓库，无法使用 Git diff/回滚；所有修改保持小步并通过编译验证。
- UnitySkills 注册表为空，当前 Unity 进程未暴露 REST 实例；Python helper 同时缺少 requests。改用当前 Unity 6000.3.12f1 自带 Roslyn 分别编译运行时和 Editor 程序集，均通过。
- vFolders 2.1.5 的 Project Browser 绘制代码依赖旧版非泛型 TreeViewItem；Unity 6000.3 改为 TreeViewItem<T> 后会持续抛 InvalidCastException。Unity 6000.3 下已关闭该不兼容绘制回调，保留插件其他功能。

## Status
**Implementation complete / runtime verification pending** - Notion 清单中的代码整改已完成并通过 Unity 6 Roslyn 编译。完整 Player Build、跨进程旧存档恢复和核心 PlayMode 流程仍需在可交互 Unity 编辑器中执行。asmdef 拆分受未程序集化的 Behavior Designer、DOTween、SmoothCameraShaker 与特效包源码依赖阻塞。

## 当前任务：玩家系统四步重构（2026-09-28）
- [x] 核对玩家状态、近战配置、UI 调用和读档调用链
- [x] 状态机成为当前状态唯一来源
- [x] 近战逻辑改用 PlayerDefinition.meleeWeapon 引用
- [x] 玩家 HUD 通过事件监听更新
- [x] 集中初始化与存档恢复并验证编译

本轮验证：运行时和 Editor 程序集均通过 Roslyn 编译；当前环境无法通过 UnitySkills helper 连接 Unity（缺少 requests），尚未进行 PlayMode 回归。临时响应文件最初包含已删除脚本且遗漏现有脚本，已用当前文件清单修正后通过。

## 当前任务：Root Motion 运动扩展入口（2026-09-29）
- [x] 核对移动、瞄准、换弹、踢击、下落等状态的 Root Motion 行为
- [x] 建立统一运动入口并迁移输入方向、转向、重力和碰撞
- [x] 将走跑等运动参数加入 PlayerDefinition 与 Inspector
- [x] 更新扩展说明并验证编译与静态调用链

验证：Unity 6 Roslyn 运行时与 Editor 编译通过；Unity 编辑器资产刷新已导入新脚本，Editor.log 未出现编译错误。UnitySkills 服务在 Domain Reload 后未恢复，未执行 PlayMode 手感回归；转向速度从错误的弧度解释修正为度/秒，需要在场景中调校。
