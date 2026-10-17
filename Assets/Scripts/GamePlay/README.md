# Scene Gameplay

GamePlay 当前是场景协调兼容门面。新功能应放入 LevelContext、EnemyRegistry、LevelAction 或独立 Presentation 服务，不继续向 GamePlay 增加领域状态。

可消耗资源由同一对象上的 `ResourceDirector` 负责决策，GamePlay 只转发掉落请求。Level01 的策划参数见项目根目录 `dynamic_resource_configuration_guide.md`。
