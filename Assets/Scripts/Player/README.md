# Player & Combat

负责玩家输入、状态机、伤害、交互和武器运行状态。静态策划数据进入 PlayerDefinition/WpConfig，生命、弹药和近战耐久只保存在 PlayerControl 的运行时数据中。

- `currentState` 从 `StateMachine.CurrentState` 推导；新增状态时更新 PlayerControl 的映射，不在状态类中另存一份枚举。瞄准、换弹的移动子状态由各自状态类提供。
- 近战行为使用 `PlayerDefinition.meleeWeapon`（运行时通过 `MeleeWeapon` 访问）；旧数字 ID 接口仅供现有背包和武器切换调用。
- 状态类调用 `SetAimVisible`、`SetQteDisplay` 等方法发布显示数据。GamePlay 监听 `PresentationChanged` 和 `StateChanged`，负责实际更新 HUD 与死亡界面。
- SaveManager 解析旧档或新档后，只调用 `RestoreFromSave` 恢复玩家。该方法集中恢复状态、装备、位置、Idle 状态与显示；强制回到 Idle 时绕过策划转移覆盖，避免读档被普通战斗规则阻断。

## 扩展 Root Motion 动作

- Animator 动画仍提供水平位移。`PlayerMotionMotor` 统一读取移动输入、计算相机相对方向、按角度转向，并通过 `CharacterController.Move` 处理位移和碰撞。走跑、瞄准、普通动作的 Root Motion 倍率、输入阈值、转身角度、落速和脚步间距在 `PlayerDefinition` 中配置。
- 新状态继承 `PlayerBaseState` 后，在 `Enter` 中调用 `player.model.SetRootMotionAction(OnRootMotion)`，在 `Exit` 中调用 `ClearRootMotionAction()`。默认 `Action` 模式使用动画水平位移和统一重力；需要走跑、瞄准或下落规则时覆盖 `MotionMode`。仅抓取演出等依赖动画纵向位移的状态使用 `Cinematic`。
- 用 `player.Motion.HasMoveInput`、`CameraRelativeDirection`、`FaceCameraForward` 或 `FaceMoveDirection` 处理方向；不要在状态内再次调用 `controller.Move`。新动作仍需在 `PlayerControl.ChangeState` 和 `currentState` 映射中注册。
- 跳跃等需要初始上升速度的动作可在进入状态时调用 `player.Motion.LaunchVertical(speed)`，并选择 `Airborne` 模式；读档传送时由玩家恢复入口重置垂直速度。
- `turnSpeed` 单位是度/秒。走路和跑步的朝向可分别选择相机前方或输入方向；跑步急转、微调动画和原地转身的角度都从 PlayerDefinition 读取。所有现有运动模式的 Root Motion 倍率、接地速度、瞄准近距阈值、动画状态名与相关 Animator 参数名也在同一资产中配置。
- 策划可从 `Tools/3D Adventure/Player Movement & State Table` 打开独立的运动表，按分类筛选并修改现有走、跑、瞄准、换弹、下落与落地的行为和动画；表格直接写入 PlayerDefinition 资产。动画状态名、参数名须与 Animator Controller 一致，修改后运行“校验内容”并在 PlayMode 试动作。`Numeric Config Tables` 的“玩家”页继续管理基础、生存、镜头、检测和初始装备，并提供跳转按钮。
- 独立窗口的“状态转移”页从 PlayerStates.cs 和 PlayerControl.ChangeState 读取状态、子状态、运动模式及直接跳转，可切换状态列表和转移矩阵；PlayMode 高亮当前运行类。选中状态后可启用、停用或改写已有跳转的实际目标，“外部事件跳转”折叠区可按当前状态覆盖受伤、武器等外部入口。修改作为稀疏覆盖规则保存在 PlayerDefinition 中，运行时由 PlayerControl.ChangeState 执行；若改写目标缺少武器、敌人或交互对象等前置条件，会回退到代码原目标并在 Console 警告。点源码行可查看触发条件。新增触发条件仍需改代码；改写目标不能凭空产生触发。
- 增加全新的输入动作或状态转场仍要在 Input Actions、Animator 和 PlayerControl/PlayerStates 中接线；现有数值表用于调整已有运动模式，不会自动生成新的状态机逻辑。
