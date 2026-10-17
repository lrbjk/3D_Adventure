# 玩家自定义状态

在 Unity 中打开 `Tools/3D Adventure/Player Movement & State Table`，切到“状态转移”，点击“＋ 新增状态”。表格会在当前 `PlayerDefinition` 中创建带有固定调用 ID 的一行。填写名称、Animator 状态名、运动模式和 Root Motion 参数；只需播放动画并结束后回到 Idle 时，可勾选“动画结束回 Idle”。

选中内置状态后，可以把其已有跳转的“实际目标”改成新状态。新的触发条件和状态中的机制由脚本提供。代码中可以通过 `player.ChangeState("表格中的调用 ID")` 主动进入；回到内置状态使用 `player.ChangeState(PlayerState.Idle)` 等。

机制脚本继承 `PlayerCustomStateLogic`，例如：

```csharp
using Astraparasitus;
using UnityEngine;

[CreateAssetMenu(menuName = "Player/State Logic/Dash")]
public class PlayerDashLogic : PlayerCustomStateLogic
{
    public override void OnEnter(PlayerControl player)
    {
        // 进入状态时初始化本次动作。
    }

    public override void OnUpdate(PlayerControl player)
    {
        // 检测退出条件，再调用 player.ChangeState(PlayerState.Idle)。
    }

    public override void OnExit(PlayerControl player)
    {
        // 清理本状态添加的效果。
    }
}
```

保存脚本后通过 `Assets > Create > Player > State Logic > Dash` 创建资产，再拖进该状态的“机制逻辑资产”栏。系统会为每个玩家状态克隆一份逻辑资产，脚本里可以保存本次动作的临时变量。状态 ID 不随显示名称修改而改变；删除状态前须先重置指向它的转移规则。
